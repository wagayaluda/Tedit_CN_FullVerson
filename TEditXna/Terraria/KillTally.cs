using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace TEditXNA.Terraria
{

    /* SBLogic
     * Rudimentary tally output, based on WorldAnalysis object.
     * Tally indices map to banners, not specific NPCs, mapping is provided by settings.xml
     */

    public static class KillTally
    {

        private const string tallyFormat = "{0}: {1}";

        public static string LoadTally(World world)
        {
            if (world == null) return string.Empty;

            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            using (var reader = new StreamReader(ms))
            {
                WriteTallyCount(writer, world, true);
                writer.Flush();
                ms.Position = 0;

                var text = reader.ReadToEnd();
                return text;
            }
        }

        public static void SaveTally(World world, string file)
        {
            if (world == null) return;

            using (var writer = new StreamWriter(file, false))
            {
                WriteTallyCount(writer, world, true);
            }
        }

        private static void WriteProperty(this StreamWriter sb, string prop, object value)
        {
            sb.WriteLine(tallyFormat, prop, value);
        }

        private static void WriteTallyCount(StreamWriter sb, World world, bool fullAnalysis = false)
        {
            WriteHeader(sb, world);
            WriteTally(sb, world);
        }

        private static void WriteHeader(StreamWriter sb, World world)
        {
            sb.WriteProperty("兼容版本", world.Version);
            sb.Write(Environment.NewLine);
        }

        private static void WriteTally(StreamWriter sb, World world)
        {

            int index = 0;
            int killcount = 0;
            int bannercount = 0;
            int uniquecount = 0;

            StringBuilder bufferBanner = new StringBuilder(); // 很难想象之前这里用string
			StringBuilder bufferNoBanner = new StringBuilder();
			StringBuilder bufferNoKill = new StringBuilder();

			// Let's explore each monster
			foreach (int count in world.KilledMobs)
            {

                if (count == 0)
                {
                    // Monster never killed
                    if (index > 0 && index <= World.TallyNames.Count)
                    {
                        World.TallyNames[index] = Regex.Replace(World.TallyNames[index], @" Banner", "");
                        bufferNoKill.AppendFormat("[{0}] {1}\n", index, World.TallyNames[index]);
                    }
                        
                }
                else if (count < 50)
                {
                    // Monster killed, but banner never obtained (less than 50 kills)
                    World.TallyNames[index] = Regex.Replace(World.TallyNames[index], @" Banner", "");
                    bufferNoBanner.AppendFormat("[{0}] {1} - {2}\n", index, World.TallyNames[index], count);
                    killcount = killcount + count;
                }
                else
                {
                    // Banners ! 50+ kills for this monster
                    int banners = (int)Math.Floor((double)count / 50f);

                    World.TallyNames[index] = Regex.Replace(World.TallyNames[index], @" Banner", "");
                    bufferBanner.AppendFormat("[{0}] {1} - {2} (共获取 {3} 个旗帜)\n", index, World.TallyNames[index], count, banners);
                    killcount = killcount + count;
                    uniquecount = uniquecount + 1;
                    bannercount = bannercount + banners;
                }
                index++;
            }

            // Print lines ...
            sb.WriteLine("=== 击杀 ===");
            sb.WriteLine(bufferBanner.ToString());
            sb.Write(Environment.NewLine);

            sb.WriteLine("=== 未获得旗帜的击杀 (50次以下) ===");
            sb.WriteLine(bufferNoBanner.ToString());
            sb.Write(Environment.NewLine);

            sb.WriteLine("=== 没有击杀过 ===");
            sb.WriteLine(bufferNoKill.ToString());
            sb.Write(Environment.NewLine);

            sb.WriteLine("总共杀怪数: {0}", killcount);
            sb.WriteLine("获取旗帜数: {0}", bannercount);
            sb.WriteLine("旗帜种类数: {0}", uniquecount);
        }

    }
}
