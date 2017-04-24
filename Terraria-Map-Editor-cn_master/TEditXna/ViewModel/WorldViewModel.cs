﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using TEdit.MvvmLight.Threading;
using TEdit.Geometry.Primitives;
using TEdit.Utility;
using Microsoft.Win32;
using TEditXna.Editor;
using TEditXna.Editor.Clipboard;
using TEditXna.Editor.Plugins;
using TEditXna.Editor.Tools;
using TEditXna.Editor.Undo;
using TEditXna.Properties;
using TEditXna.Render;
using TEditXNA.Terraria;
using TEditXNA.Terraria.Objects;
using TEditXna.View.Popups;
using DispatcherHelper = GalaSoft.MvvmLight.Threading.DispatcherHelper;

namespace TEditXna.ViewModel
{
    public partial class WorldViewModel : ViewModelBase
    {
        private readonly BrushSettings _brush = new BrushSettings();
        private readonly ClipboardManager _clipboard;
        private readonly Stopwatch _loadTimer = new Stopwatch();
        private readonly MouseTile _mouseOverTile = new MouseTile();
        private readonly ObservableCollection<IPlugin> _plugins = new ObservableCollection<IPlugin>();
        private readonly ObservableCollection<string> _points = new ObservableCollection<string>();
        private readonly Timer _saveTimer = new Timer();
        private readonly Selection _selection = new Selection();
        private readonly TilePicker _tilePicker = new TilePicker();
        private readonly ObservableCollection<ITool> _tools = new ObservableCollection<ITool>();
        private readonly UndoManager _undoManager;
        public bool[] CheckTiles;
        private ITool _activeTool;
        private bool _checkUpdates;
        private string _currentFile;
        private World _currentWorld;
        private bool _isAutoSaveEnabled = true;
        private ICommand _launchWikiCommand;
        private WriteableBitmap _minimapImage;
        private MorphBiome _morphBiomeTarget;
        private PixelMapManager _pixelMap;
        private ProgressChangedEventArgs _progress;
        private Chest _selectedChest;
        private Item _selectedChestItem;
        private string _selectedPoint;
        private Sign _selectedSign;
        private Sprite _selectedSprite;
        private TileEntity _selectedItemFrame;
        private Vector2Int32 _selectedMannequin;
        private Vector2Int32 _selectedRack;
        private byte _selectedRackPrefix;
        private int _selectedRackNetId;
        private int _selectedMannHead;
        private int _selectedMannBody;
        private int _selectedMannLegs;
        private int _selectedTabIndex;
        private int _selectedSpecialTile = 0;
        private bool _showGrid = true;
        private bool _showLiquid = true;
        private bool _showPoints = true;
        private bool _showTextures = true;
        private bool _showTiles = true;
        private bool _showWalls = true;
        private bool _showActuators = true;
        private bool _showRedWires = true;
        private bool _showBlueWires = true;
        private bool _showGreenWires = true;
        private bool _showYellowWires = true;
        private string _spriteFilter;
        private ListCollectionView _spritesView;
        private ICommand _viewLogCommand;
        private string _windowTitle;
        private ICommand _checkUpdatesCommand;
		private ICommand _reportIssueCommand;


		public ICommand CheckUpdatesCommand
        {
            get { return _checkUpdatesCommand ?? (_checkUpdatesCommand = new RelayCommand<bool>(CheckVersion)); }
        }

        static WorldViewModel()
        {
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
        }

        public WorldViewModel()
        {
            if (IsInDesignModeStatic)
                return;

            CheckUpdates = Properties.Settings.Default.CheckUpdates;

            if (CheckUpdates)
                CheckVersion();


            IsAutoSaveEnabled = Settings.Default.Autosave;

            _undoManager = new UndoManager(this);
            _clipboard = new ClipboardManager(this);
            World.ProgressChanged += OnProgressChanged;
            Brush.BrushChanged += OnPreviewChanged;
            UpdateTitle();

            _spriteFilter = string.Empty;
            _spritesView = (ListCollectionView)CollectionViewSource.GetDefaultView(World.Sprites);
            _spritesView.Filter = o =>
            {
                if (string.IsNullOrWhiteSpace(_spriteFilter)) return true;

                var sprite = (Sprite)o;

                string [] _spriteFilterSplit = _spriteFilter.Split('/');
                foreach (string _spriteWord in _spriteFilterSplit)
                {
                    if (sprite.TileName == _spriteWord) return true;
                    if (sprite.Name == _spriteWord) return true;
                    if (sprite.TileName != null && sprite.TileName.IndexOf(_spriteWord, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (sprite.Name != null && sprite.Name.IndexOf(_spriteWord, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }

                if (sprite.TileName == _spriteFilter) return true;
                if (sprite.Name == _spriteFilter) return true;
                if (sprite.TileName != null && sprite.TileName.IndexOf(_spriteFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (sprite.Name != null && sprite.Name.IndexOf(_spriteFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;

                return false;
            };

            _saveTimer.AutoReset = true;
            _saveTimer.Elapsed += SaveTimerTick;
            // 3 minute save timer
            _saveTimer.Interval = 3 * 60 * 1000;

            _undoManager.Redid += UpdateMinimap;
            _undoManager.Undid += UpdateMinimap;
            _undoManager.UndoSaved += UpdateMinimap;

            // Test File Association and command line
            if (Application.Current.Properties["OpenFile"] != null)
            {
                string filename = Application.Current.Properties["OpenFile"].ToString();
                LoadWorld(filename);
            }
        }


        public WriteableBitmap MinimapImage
        {
            get { return _minimapImage; }
            set { Set("MinimapImage", ref _minimapImage, value); }
        }

        public ListCollectionView SpritesView
        {
            get { return _spritesView; }
            set { Set("SpritesView", ref _spritesView, value); }
        }

        public string SpriteFilter
        {
            get { return _spriteFilter; }
            set
            {
                Set("SpriteFilter", ref _spriteFilter, value);

                SpritesView.Refresh();
            }
        }


        public ICommand LaunchWikiCommand
        {
            get { return _launchWikiCommand ?? (_launchWikiCommand = new RelayCommand(() => LaunchUrl("http://github.com/BinaryConstruct/Terraria-Map-Editor/wiki"))); }
        }

	    public ICommand ReportIssueCommand
	    {
			get { return _reportIssueCommand ?? (_reportIssueCommand = new RelayCommand(() => LaunchUrl("https://github.com/mistzzt/Terraria-Map-Editor/issues"))); }
	    }

        /* SBLogic - catch exception if browser can't be launched */
        private void LaunchUrl(string url)
        {
            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
            try
            {
                Process.Start(url);
            }
            catch
            {
                result = System.Windows.Forms.MessageBox.Show("无法打开外部浏览器. 要复制链接到粘贴板么?", "链接无法打开", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Exclamation);
            }

            // Just in case
            try
            {
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    System.Windows.Clipboard.SetText(url);
                }
            }
            catch { }
        }


        public static string TempPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TEdit"); }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set { Set("SelectedTabIndex", ref _selectedTabIndex, value); }
        }

        public int SelectedSpecialTile
        {
            get { return _selectedSpecialTile; }
            set { Set("SelectedSpecialTile", ref _selectedSpecialTile, value); }
        }

        public int SelectedMannHead
        {
            get { return _selectedMannHead; }
            set { Set("SelectedMannHead", ref _selectedMannHead, value); }
        }

        public int SelectedMannBody
        {
            get { return _selectedMannBody; }
            set { Set("SelectedMannBody", ref _selectedMannBody, value); }
        }

        public int SelectedMannLegs
        {
            get { return _selectedMannLegs; }
            set { Set("SelectedMannLegs", ref _selectedMannLegs, value); }
        }

        public byte SelectedRackPrefix
        {
            get { return _selectedRackPrefix; }
            set { Set("SelectedRackPrefix", ref _selectedRackPrefix, value); }
        }

        public int SelectedRackNetId
        {
            get { return _selectedRackNetId; }
            set { Set("SelectedRackNetId", ref _selectedRackNetId, value); }
        }

        public Sign SelectedSign
        {
            get { return _selectedSign; }
            set
            {
                Set("SelectedSign", ref _selectedSign, value);
                SelectedTabIndex = 1;
                SelectedSpecialTile = 1;
            }
        }

        public Chest SelectedChest
        {
            get { return _selectedChest; }
            set
            {
                Set("SelectedChest", ref _selectedChest, value);
                SelectedTabIndex = 1;
                SelectedSpecialTile = 2;
            }
        }

        public TileEntity SelectedItemFrame
        {
            get { return _selectedItemFrame; }
            set
            {
                Set("SelectedItemFrame", ref _selectedItemFrame, value);
                SelectedTabIndex = 1;
                SelectedSpecialTile = 3;
            }
        }

        public Vector2Int32 SelectedMannequin
        {
            get { return _selectedMannequin; }
            set
            {
                Set("SelectedMannequin", ref _selectedMannequin, value);
                SelectedTabIndex = 1;
                SelectedSpecialTile = 4;
            }
        }

        public Vector2Int32 SelectedRack
        {
            get { return _selectedRack; }
            set
            {
                Set("SelectedRack", ref _selectedRack, value);
                SelectedTabIndex = 1;
                SelectedSpecialTile = 5;
            }
        }

        public ObservableCollection<IPlugin> Plugins
        {
            get { return _plugins; }
        }

        public MorphBiome MorphBiomeTarget
        {
            get { return _morphBiomeTarget; }
            set { Set("MorphBiomeTarget", ref _morphBiomeTarget, value); }
        }

        public bool IsAutoSaveEnabled
        {
            get { return _isAutoSaveEnabled; }
            set
            {
                Set("IsAutoSaveEnabled", ref _isAutoSaveEnabled, value);
                Settings.Default.Autosave = _isAutoSaveEnabled;
                Settings.Default.Save();
            }
        }

        public bool ShowGrid
        {
            get { return _showGrid; }
            set { Set("ShowGrid", ref _showGrid, value); }
        }

        public bool ShowTextures
        {
            get { return _showTextures; }
            set { Set("ShowTextures", ref _showTextures, value); }
        }


        public Sprite SelectedSprite
        {
            get { return _selectedSprite; }
            set
            {
                Set("SelectedSprite", ref _selectedSprite, value);
                PreviewChange();
            }
        }

        public ObservableCollection<string> Points
        {
            get { return _points; }
        }

        public string SelectedPoint
        {
            get { return _selectedPoint; }
            set { Set("SelectedPoint", ref _selectedPoint, value); }
        }


        public Item SelectedChestItem
        {
            get { return _selectedChestItem; }
            set { Set("SelectedChestItem", ref _selectedChestItem, value); }
        }

        public UndoManager UndoManager
        {
            get { return _undoManager; }
        }

        public ClipboardManager Clipboard
        {
            get { return _clipboard; }
        }

        public Selection Selection
        {
            get { return _selection; }
        }

        public MouseTile MouseOverTile
        {
            get { return _mouseOverTile; }
        }

        public string CurrentFile
        {
            get { return _currentFile; }
            set { Set("CurrentFile", ref _currentFile, value); }
        }

        public World CurrentWorld
        {
            get { return _currentWorld; }
            set { Set("CurrentWorld", ref _currentWorld, value); }
        }

        public ProgressChangedEventArgs Progress
        {
            get { return _progress; }
            set { Set("Progress", ref _progress, value); }
        }

        public string WindowTitle
        {
            get { return _windowTitle; }
            set { Set("WindowTitle", ref _windowTitle, value); }
        }

        public BrushSettings Brush
        {
            get { return _brush; }
        }

        public TilePicker TilePicker
        {
            get { return _tilePicker; }
        }

        public ObservableCollection<ITool> Tools
        {
            get { return _tools; }
        }

        public ITool ActiveTool
        {
            get { return _activeTool; }
            set { Set("ActiveTool", ref _activeTool, value); }
        }

        public PixelMapManager PixelMap
        {
            get { return _pixelMap; }
            set { Set("PixelMap", ref _pixelMap, value); }
        }

        public bool ShowRedWires
        {
            get { return _showRedWires; }
            set
            {
                Set("ShowRedWires", ref _showRedWires, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowBlueWires
        {
            get { return _showBlueWires; }
            set
            {
                Set("ShowBlueWires", ref _showBlueWires, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowGreenWires
        {
            get { return _showGreenWires; }
            set
            {
                Set("ShowGreenWires", ref _showGreenWires, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowYellowWires
        {
            get { return _showYellowWires; }
            set
            {
                Set("ShowYellowWires", ref _showYellowWires, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowActuators
        {
            get { return _showActuators; }
            set
            {
                Set("ShowActuators", ref _showActuators, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowPoints
        {
            get { return _showPoints; }
            set { Set("ShowPoints", ref _showPoints, value); }
        }

        public bool ShowLiquid
        {
            get { return _showLiquid; }
            set
            {
                Set("ShowLiquid", ref _showLiquid, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowTiles
        {
            get { return _showTiles; }
            set
            {
                Set("ShowTiles", ref _showTiles, value);
                UpdateRenderWorld();
            }
        }

        public bool ShowWalls
        {
            get { return _showWalls; }
            set
            {
                Set("ShowWalls", ref _showWalls, value);
                UpdateRenderWorld();
            }
        }


        public ICommand ViewLogCommand
        {
            get { return _viewLogCommand ?? (_viewLogCommand = new RelayCommand(ViewLog)); }
        }

        public bool CheckUpdates
        {
            get { return _checkUpdates; }
            set
            {
                Set("CheckUpdates", ref _checkUpdates, value);
                Settings.Default.CheckUpdates = value;
                Settings.Default.Save();
            }
        }

        private void UpdateMinimap(object sender, EventArgs eventArgs)
        {
            if (CurrentWorld != null)
            {
                if (MinimapImage != null)
                    RenderMiniMap.UpdateMinimap(CurrentWorld, ref _minimapImage);
            }
        }

        private void SaveTimerTick(object sender, ElapsedEventArgs e)
        {
            if (IsAutoSaveEnabled)
            {
                if (!string.IsNullOrWhiteSpace(CurrentFile))
                    SaveWorldThreaded(Path.Combine(TempPath, Path.GetFileNameWithoutExtension(CurrentFile) + ".autosave"));
                else
                    SaveWorldThreaded(Path.Combine(TempPath, "newworld.autosave"));
            }
        }

        private void ViewLog()
        {
            ErrorLogging.ViewLog();
        }

        private void UpdateTitle()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
	        Version cnv = App.CnVersion;

            WindowTitle = string.Format("TEdit v{0}.{1}.{2}.{3} (CNv{4}.{5}.{6}.{7}) {8}",
                fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart,
				cnv.Major, cnv.Minor, cnv.Build, cnv.Revision,
                Path.GetFileName(_currentFile));
        }

        public void CheckVersion(bool auto = true)
        {
            Task.Factory.StartNew<bool?>(() =>
            {
                try
                {
                    using (var Client = new WebClient())
                    {
                        byte[] dl = Client.DownloadData("https://raw.githubusercontent.com/mistzzt/Terraria-Map-Editor/cn_master/cnversion.txt");

                        string vers = Encoding.UTF8.GetString(dl);
                        string[] verstrimmed = vers.Split('v');

                        if (verstrimmed.Length != 2) return null;

                        string[] split = verstrimmed[1].Split('.');

                        if (split.Length < 3) return null; // SBLogic -- accept revision part if present

                        int major;
                        int minor;
                        int build;
                        int revis = -1;

                        if (!int.TryParse(split[0], out major)) return null;
                        if (!int.TryParse(split[1], out minor)) return null;
                        if (!int.TryParse(split[2], out build)) return null;
                        if ((split.Length == 4) && (split[3].Length > 0) && (!int.TryParse(split[3], out revis))) return null;

                        if (major > App.CnVersion.Major) return true;
                        if (minor > App.CnVersion.Minor) return true;
                        if (build > App.CnVersion.Build) return true;
                        // if ((revis != -1) && (revis > App.CnVersion.Revision)) return true;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                return false;
            }).ContinueWith(t =>
            {
                bool? isoutofdate = t.Result;

                if (isoutofdate == null)
                {
                    MessageBox.Show("无法检测版本.", "更新检测失败");
                }
                else if (isoutofdate == true)
                {
                    if (MessageBox.Show("现在有TEdit汉化版的可用更新. 要下载么?", "更新提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start("https://github.com/mistzzt/Terraria-Map-Editor/releases");
                        }
                        catch { }

                    }
                }
                else
                {
                    if (!auto)
                        MessageBox.Show("当前版本为最新版本.", "更新提示");
                }

            }, TaskFactoryHelper.UiTaskScheduler);
        }

        private ICommand _analyzeWorldCommand;
        private ICommand _analyzeWorldSaveCommand;
        private ICommand _tallyCountCommand;
        private ICommand _tallyCountSaveCommand;


        /// <summary>
        /// Relay command to execute AnalyzeWorldSave.
        /// </summary>
        public ICommand AnalyzeWorldSaveCommand
        {
            get { return _analyzeWorldSaveCommand ?? (_analyzeWorldSaveCommand = new RelayCommand(AnalyzeWorldSave)); }
        }

        private void AnalyzeWorldSave()
        {
            if (this.CurrentWorld == null) return;
            var sfd = new SaveFileDialog();
            sfd.DefaultExt = "文本文件|*.txt";
            sfd.Filter = "文本文件|*.txt";
            sfd.FileName = this.CurrentWorld.Title + " 分析.txt";
            sfd.Title = "保存世界分析日志.";
            sfd.OverwritePrompt = true;
            if (sfd.ShowDialog() == true)
            {
                TEditXNA.Terraria.WorldAnalysis.AnalyzeWorld(this.CurrentWorld, sfd.FileName);

            }
        }

        public ICommand TallyCountSaveCommand
        {
            get { return _tallyCountSaveCommand ?? (_tallyCountSaveCommand = new RelayCommand(TallyCountSave)); }
        }

        private void TallyCountSave()
        {
            if (this.CurrentWorld == null) return;
            var sfd = new SaveFileDialog();
            sfd.DefaultExt = "文本文件|*.txt";
            sfd.Filter = "文本文件|*.txt";
            sfd.FileName = this.CurrentWorld.Title + " 战况.txt";
            sfd.Title = "保存世界分析日志.";
            sfd.OverwritePrompt = true;
            if (sfd.ShowDialog() == true)
            {
                TEditXNA.Terraria.KillTally.SaveTally(this.CurrentWorld, sfd.FileName);

            }
        }

        /// <summary>
        /// Relay command to execute AnalizeWorld.
        /// </summary>
        public ICommand AnalyzeWorldCommand
        {
            get { return _analyzeWorldCommand ?? (_analyzeWorldCommand = new RelayCommand(AnalyzeWorld)); }
        }

        private void AnalyzeWorld()
        {
            WorldAnalysis = TEditXNA.Terraria.WorldAnalysis.AnalyzeWorld(this.CurrentWorld);
        }

        private string _worldAnalysis;


        public string WorldAnalysis
        {
            get { return _worldAnalysis; }
            set { Set("WorldAnalysis", ref _worldAnalysis, value); }
        }

        /* SBLogic - Relay command to execute KillTally */

        public ICommand LoadTallyCommand
        {
            get { return _tallyCountCommand ?? (_tallyCountCommand = new RelayCommand(GetTallyCount)); }
        }

        private void GetTallyCount()
        {
            TallyCount = TEditXNA.Terraria.KillTally.LoadTally(this.CurrentWorld);
        }

        private string _tallyCount;

        public string TallyCount
        {
            get { return _tallyCount; }
            set { Set("TallyCount", ref _tallyCount, value); }
        }

        public event EventHandler PreviewChanged;

        public void PreviewChange()
        {
            OnPreviewChanged(this, new EventArgs());
        }

        protected virtual void OnPreviewChanged(object sender, EventArgs e)
        {
            if (PreviewChanged != null) PreviewChanged(sender, e);
        }

        private void SetActiveTool(ITool tool)
        {
            if (ActiveTool != tool)
            {
                if (tool.Name == "粘贴" && !CanPaste()) // 工具名
                    return;

                if (ActiveTool != null)
                    ActiveTool.IsActive = false;

                ActiveTool = tool;
                tool.IsActive = true;

                PreviewChange();
            }
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => Progress = e);
        }

        public void MouseDownTile(TileMouseState e)
        {
            if (e.Location != MouseOverTile.MouseState.Location)
                MouseOverTile.Tile = CurrentWorld.Tiles[e.Location.X, e.Location.Y];

            MouseOverTile.MouseState = e;
            ActiveTool.MouseDown(e);

            CommandManager.InvalidateRequerySuggested();
        }

        public void MouseUpTile(TileMouseState e)
        {
            if (e.Location != MouseOverTile.MouseState.Location)
                MouseOverTile.Tile = CurrentWorld.Tiles[e.Location.X, e.Location.Y];

            MouseOverTile.MouseState = e;
            ActiveTool.MouseUp(e);
            CommandManager.InvalidateRequerySuggested();
        }

        public void MouseMoveTile(TileMouseState e)
        {
            if (e.Location.X >= 0 && e.Location.Y >= 0 && e.Location.X < CurrentWorld.TilesWide && e.Location.Y < CurrentWorld.TilesHigh)
            {
                if (e.Location != MouseOverTile.MouseState.Location)
                    MouseOverTile.Tile = CurrentWorld.Tiles[e.Location.X, e.Location.Y];

                MouseOverTile.MouseState = e;

                ActiveTool.MouseMove(e);
            }
        }

        private void OpenWorld()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Terraria 世界文件|*.wld|Terraria 世界备份|*.bak|TEdit 备份文件|*.TEdit";
            ofd.DefaultExt = "Terraria 世界文件|*.wld";
            ofd.Title = "加载 Terraria 世界存档文件";
            ofd.InitialDirectory = DependencyChecker.PathToWorlds;
            ofd.Multiselect = false;
            if ((bool)ofd.ShowDialog())
            {
                LoadWorld(ofd.FileName);
            }
        }

        private void NewWorld()
        {
            var nwDialog = new NewWorldView();
            if ((bool)nwDialog.ShowDialog())
            {
                _loadTimer.Reset();
                _loadTimer.Start();
                _saveTimer.Stop();
                Task.Factory.StartNew(() =>
                {
                    World w = nwDialog.NewWorld;
                    w.SpawnX = w.TilesWide / 2;
                    w.SpawnY = (int)Math.Max(0, w.GroundLevel - 10);
                    w.GroundLevel = (int)w.GroundLevel;
                    w.RockLevel = (int)w.RockLevel;
                    w.BottomWorld = w.TilesHigh * 16;
                    w.RightWorld = w.TilesWide * 16;
                    w.Tiles = new Tile[w.TilesWide, w.TilesHigh];
                    var cloneTile = new Tile();
                    for (int y = 0; y < w.TilesHigh; y++)
                    {
                        OnProgressChanged(w, new ProgressChangedEventArgs(Calc.ProgressPercentage(y, w.TilesHigh), "生成世界中..."));

                        if (y == (int)w.GroundLevel - 10)
                            cloneTile = new Tile { WireRed = false, IsActive = true, LiquidType = LiquidType.None, LiquidAmount = 0, Type = 2, U = -1, V = -1, Wall = 2 };
                        if (y == (int)w.GroundLevel - 9)
                            cloneTile = new Tile { WireRed = false, IsActive = true, LiquidType = LiquidType.None, LiquidAmount = 0, Type = 0, U = -1, V = -1, Wall = 2 };
                        else if (y == (int)w.GroundLevel + 1)
                            cloneTile = new Tile { WireRed = false, IsActive = true, LiquidType = LiquidType.None, LiquidAmount = 0, Type = 0, U = -1, V = -1, Wall = 0 };
                        else if (y == (int)w.RockLevel)
                            cloneTile = new Tile { WireRed = false, IsActive = true, LiquidType = LiquidType.None, LiquidAmount = 0, Type = 1, U = -1, V = -1, Wall = 0 };
                        else if (y == w.TilesHigh - 182)
                            cloneTile = new Tile();
                        for (int x = 0; x < w.TilesWide; x++)
                        {
                            w.Tiles[x, y] = (Tile)cloneTile.Clone();
                        }
                    }
                    return w;
                })
                    .ContinueWith(t => CurrentWorld = t.Result, TaskFactoryHelper.UiTaskScheduler)
                    .ContinueWith(t => RenderEntireWorld())
                    .ContinueWith(t =>
                    {
                        CurrentFile = null;
                        PixelMap = t.Result;
                        UpdateTitle();
                        Points.Clear();
                        Points.Add("出生点"); // PointTool.MouseDown 判定
						Points.Add("地牢");
                        foreach (NPC npc in CurrentWorld.NPCs)
                        {
                            Points.Add(npc.Name);
                        }
                        MinimapImage = RenderMiniMap.Render(CurrentWorld);
                        _loadTimer.Stop();
                        OnProgressChanged(this, new ProgressChangedEventArgs(0, string.Format("加载世界耗时 {0} 秒.", _loadTimer.Elapsed.TotalSeconds)));
                        _saveTimer.Start();
                    }, TaskFactoryHelper.UiTaskScheduler);
            }
        }

        private void SaveWorld()
        {
            if (CurrentWorld == null)
                return;

            if (string.IsNullOrWhiteSpace(CurrentFile))
                SaveWorldAs();
            else
                SaveWorldFile();
        }

        private void SaveWorldAs()
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Terraria 世界文件|*.wld|TEdit 备份文件|*.TEdit";
            sfd.Title = "世界文件另存为";
            sfd.InitialDirectory = DependencyChecker.PathToWorlds;
            if ((bool)sfd.ShowDialog())
            {
                CurrentFile = sfd.FileName;
                SaveWorldFile();
            }
        }

        private void SaveWorldFile()
        {
            if (CurrentWorld.LastSave < File.GetLastWriteTimeUtc(CurrentFile))
            {
                MessageBoxResult overwrite = MessageBox.Show(_currentWorld.Title + " 在上次保存后在外部被修改.\r\n你确定要覆盖保存么?", "世界文件被外部修改", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (overwrite.Equals(MessageBoxResult.Cancel))
                    return;
            }
            SaveWorldThreaded(CurrentFile);
        }

        private void SaveWorldThreaded(string filename)
        {
            Task.Factory.StartNew(() => World.Save(CurrentWorld, filename))
                .ContinueWith(t => CommandManager.InvalidateRequerySuggested(), TaskFactoryHelper.UiTaskScheduler);
        }

        public void LoadWorld(string filename)
        {
            _loadTimer.Reset();
            _loadTimer.Start();
            _saveTimer.Stop();
            CurrentFile = filename;
            CurrentWorld = null;
            GC.WaitForFullGCComplete();

            Task.Factory.StartNew(() => World.LoadWorld(filename))
                .ContinueWith(t => CurrentWorld = t.Result, TaskFactoryHelper.UiTaskScheduler)
                .ContinueWith(t => RenderEntireWorld())
                .ContinueWith(t =>
                {
                    if (CurrentWorld != null)
                    {
                        PixelMap = t.Result;
                        UpdateTitle();
                        Points.Clear();
                        Points.Add("出生点"); // PointTool.MouseDown 判定
						Points.Add("地牢");
                        foreach (NPC npc in CurrentWorld.NPCs)
                        {
                            Points.Add(npc.Name);
                        }
                        MinimapImage = RenderMiniMap.Render(CurrentWorld);
                        _loadTimer.Stop();
                        OnProgressChanged(this, new ProgressChangedEventArgs(0, string.Format("加载世界耗费 {0} 秒.", _loadTimer.Elapsed.TotalSeconds)));
                        _saveTimer.Start();
                    }
                    _loadTimer.Stop();
                }, TaskFactoryHelper.UiTaskScheduler);
        }
    }
}
