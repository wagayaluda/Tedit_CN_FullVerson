using System.ComponentModel;

namespace TEditXna.Editor
{
    public enum MorphBiome
    {
		[Description("净化")]
		Purify = 0,
		[Description("腐化")]
		Corruption = 1,
		[Description("血腥化")]
		Crimson = 2,
		[Description("神圣化")]
		Hallow = 3,
    }
}