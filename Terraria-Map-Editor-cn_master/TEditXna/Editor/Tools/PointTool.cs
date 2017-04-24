using System;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TEdit.Geometry.Primitives;
using TEditXNA.Terraria;
using TEditXna.ViewModel;

namespace TEditXna.Editor.Tools
{
    public sealed class PointTool : BaseTool
    {
        public PointTool(WorldViewModel worldViewModel)
            : base(worldViewModel)
        {
            Icon = new BitmapImage(new Uri(@"pack://application:,,,/TEditXna;component/Images/Tools/point.png"));
            Name = "��"; // ע��MainWindow�����ݰ�
            IsActive = false;
            ToolType = ToolType.Npc;
        }

        public override void MouseDown(TileMouseState e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                NPC npc = _wvm.CurrentWorld.NPCs.FirstOrDefault(n => n.Name == _wvm.SelectedPoint);

                if (npc != null)
                {
                    npc.Home = e.Location;
                    npc.Position = new Vector2(e.Location.X * 16, e.Location.Y * 16);
                }
                else
                {
                    if (string.Equals(_wvm.SelectedPoint, "������", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _wvm.CurrentWorld.SpawnX = e.Location.X;
                        _wvm.CurrentWorld.SpawnY = e.Location.Y;
                    }
                    else if (string.Equals(_wvm.SelectedPoint, "����", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _wvm.CurrentWorld.DungeonX = e.Location.X;
                        _wvm.CurrentWorld.DungeonY = e.Location.Y;
                    }
                }
            }
        }
    }
}