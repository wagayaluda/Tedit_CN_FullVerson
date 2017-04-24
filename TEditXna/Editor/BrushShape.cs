using System.ComponentModel;

namespace TEditXna.Editor
{
    public enum BrushShape
    {
        [Description("矩形")]
        Square,
        [Description("椭圆")]
        Round,
        [Description("右倾斜线")]
        Right,
        [Description("左倾斜线")]
        Left,
    }
}
