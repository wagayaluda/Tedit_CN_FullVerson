using System.ComponentModel;

namespace TEditXna.Editor
{
    public enum MaskMode
    {
        [Description("�ر�ɸѡ�༭")] // Disable Mask ��û�и��õķ���? ��� ReplaceAllPlugin.cs
        Off,
        [Description("�༭�հײ���")]
        Empty,
        [Description("�༭������������")]
        Match,
        [Description("�༭������������")]
        NotMatching,
    }
}