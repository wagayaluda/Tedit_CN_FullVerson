using TEdit.Geometry.Primitives;
using GalaSoft.MvvmLight;
using TEditXna.Helper;
using TEditXNA.Terraria;

namespace TEditXna.Editor
{
    public class MouseTile : ObservableObject
    {
        private TileMouseState _mouseState = new TileMouseState();
        private Tile _tile;
        private Vector2Short _uV;
        private string _tileExtras;
        private string _tileName;
        private string _wallName;
        private string _paint;

        public TileMouseState MouseState
        {
            get { return _mouseState; }
            set { Set("MouseState", ref _mouseState, value); }
        }

        public string WallName
        {
            get { return _wallName; }
            set { Set("WallName", ref _wallName, value); }
        }

        public string TileName
        {
            get { return _tileName; }
            set { Set("TileName", ref _tileName, value); }
        }

        public string Paint
        {
            get { return _paint; }
            set { Set("Paint", ref _paint, value); }
        }

        public string TileExtras
        {
            get { return _tileExtras; }
            set { Set("TileExtras", ref _tileExtras, value); }
        }

        public Vector2Short UV
        {
            get { return _uV; }
            set { Set("UV", ref _uV, value); }
        }

        public Tile Tile
        {
            get { return _tile; }
            set
            {
                Set("Tile", ref _tile, value);

                if (World.TileProperties.Count > _tile.Type)
                {
                    TEditXNA.Terraria.Objects.TileProperty tileProperty = World.TileProperties[_tile.Type];
                    if (!tileProperty.HasFrameName)
                    {
                        TileName = tileProperty.Name;
                    }
                    else
                    {
                        string frameNameKey = World.GetFrameNameKey(_tile.Type, _tile.U, _tile.V);
                        TileName = World.FrameNames.ContainsKey(frameNameKey) ? World.FrameNames[frameNameKey] : tileProperty.Name + "*";
                    }
                    TileName = _tile.IsActive ? string.Format("{0} ({1})", TileName, _tile.Type) : "[无]";
                }
                else
                    TileName = string.Format("无效物块 ({0})", _tile.Type);

                if (World.WallProperties.Count > _tile.Wall)
                    WallName = string.Format("{0} ({1})", World.WallProperties[_tile.Wall].Name, _tile.Wall);
                else
                    WallName = string.Format("无效墙壁 ({0})", _tile.Wall);

                UV = new Vector2Short(_tile.U, _tile.V);
                if (_tile.LiquidAmount > 0)
                {
                    TileExtras = string.Format("{0}: {1}", _tile.LiquidType.GetDisplayName(), _tile.LiquidAmount);
                }
                else
                    TileExtras = string.Empty;
                
                if (_tile.TileColor > 0)
                {
                    if (_tile.WallColor > 0)
                        Paint = string.Format("物块: {0}, 墙壁: {1}", World.PaintProperties[_tile.TileColor].Name, World.PaintProperties[_tile.WallColor].Name);
                    else
                        Paint = string.Format("物块: {0}", World.PaintProperties[_tile.TileColor].Name);
                }
                else if (_tile.WallColor > 0)
                {
                    Paint = string.Format("墙壁: {0}", World.PaintProperties[_tile.WallColor].Name);
                }
                else
                {
                    Paint = "无";
                }

                if (_tile.InActive)
                {
                    TileExtras += (string.IsNullOrWhiteSpace(TileExtras) ? string.Empty : " ") + "虚化";
                }

                if (_tile.Actuator)
                {
                    TileExtras += (string.IsNullOrWhiteSpace(TileExtras) ? string.Empty : " ") + "促动器";
                }

                if (_tile.WireRed || _tile.WireBlue || _tile.WireGreen || _tile.WireYellow)
                {
	                var clr = string.Empty;

					if (_tile.WireRed)
                        clr += "红";
                    if (_tile.WireGreen)
						clr += "绿";
                    if (_tile.WireBlue)
						clr += "蓝";
                    if (_tile.WireYellow)
						clr += "黄";

					TileExtras += (string.IsNullOrWhiteSpace(TileExtras) ? string.Empty : " ") + clr + "线";
				}
            }
        }
    }
}
