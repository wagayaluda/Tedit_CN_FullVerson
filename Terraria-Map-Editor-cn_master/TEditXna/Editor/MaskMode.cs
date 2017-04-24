using System.ComponentModel;

namespace TEditXna.Editor
{
    public enum MaskMode
    {
        [Description("关闭筛选编辑")] // Disable Mask 有没有更好的翻译? 另见 ReplaceAllPlugin.cs
        Off,
        [Description("编辑空白部分")]
        Empty,
        [Description("编辑符合条件部分")]
        Match,
        [Description("编辑不符条件部分")]
        NotMatching,
    }
}