using System.ComponentModel;

namespace TEditXna.Editor
{
    public enum PaintMode
    {
        [Description("物块/墙壁")]
        TileAndWall,
        [Description("电线")]
        Wire,
        [Description("液体")]
        Liquid,
        [Description("轨道")]
        Track,
    }
    public enum TrackMode
    {
        [Description("轨道")]
        Track,
        [Description("加速器")]
        Booster,
        [Description("压力板")]
        Pressure,
        [Description("锤子")]
        Hammer,
    }
}