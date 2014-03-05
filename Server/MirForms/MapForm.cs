﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.MirForms
{
    public static class ConvertMapInfo
    {
        public static List<MapInfo> MapInfo = new List<MapInfo>();
        public static List<MapMovements> MapMovements = new List<MapMovements>();
        private static int _endIndex = 0;
        public static string Path = string.Empty;

        public static void Start(int lastIndex = 0)
        {
            if (Path == string.Empty) return;

            var lines = File.ReadAllLines(Path);

            _endIndex = lastIndex; // Last map index number

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[")) // Read map info
                {
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"\s+", " "); // Clear white-space

                    lines[i] = lines[i].Replace(" ;", ";"); // Remove space before semi-colon

                    // Trim comment at the end of the line
                    if (lines[i].Contains(';'))
                        lines[i] = lines[i].Substring(0, lines[i].IndexOf(";", System.StringComparison.Ordinal));

                    MapInfo newMapInfo = new MapInfo {Index = ++_endIndex};

                    var a = lines[i].Split(']'); // Split map info into [0] = MapFile MapName 0 || [1] = Attributes

                    string[] b = a[0].Split(' ');

                    newMapInfo.MapFile = b[0].TrimStart('['); // Assign MapFile from variable and trim leading '[' char
                    newMapInfo.MapName = b[1]; // Assign MapName from variable

                    List<string> mapAttributes = new List<string>(); // List of all attributes associated with that map
                    mapAttributes.AddRange(a[1].Split(' '));

                    int nri = mapAttributes.FindIndex(x => x.StartsWith("NORECONNECT(".ToUpper())); // NORECONNECT() placement in list of parameters
                    int fi = mapAttributes.FindIndex(x => x.StartsWith("FIRE(".ToUpper())); // FIRE() placement in list of parameters
                    int li = mapAttributes.FindIndex(x => x.StartsWith("LIGHTNING(".ToUpper())); // LIGHTNING() placement in list of parameters
                    int lighti = mapAttributes.FindIndex(x => x.StartsWith("LIGHT(".ToUpper())); // LIGHT() placement in list of parameters
                    int mmi = mapAttributes.FindIndex(x => x.StartsWith("MINIMAP(".ToUpper())); // MINIMAP() placement in list of parameters
                    int bmi = mapAttributes.FindIndex(x => x.StartsWith("BIGMAP(".ToUpper())); // BIGMAP() placement in list of parameters

                    newMapInfo.NoTeleport = mapAttributes.Any(s => s.Contains("NOTELEPORT".ToUpper()));
                    newMapInfo.NoReconnect = mapAttributes.Any(x => x.StartsWith("NORECONNECT".ToUpper()));
                    newMapInfo.NoRandom = mapAttributes.Any(s => s.Contains("NORANDOMMOVE".ToUpper()));
                    newMapInfo.NoEscape = mapAttributes.Any(s => s.Contains("NOESCAPE".ToUpper()));
                    newMapInfo.NoRecall = mapAttributes.Any(s => s.Contains("NORECALL".ToUpper()));
                    newMapInfo.NoDrug = mapAttributes.Any(s => s.Contains("NODRUG".ToUpper()));
                    newMapInfo.NoPositionMove = mapAttributes.Any(s => s.Contains("NOPOSITIONMOVE".ToUpper()));
                    newMapInfo.NoThrowItem = mapAttributes.Any(s => s.Contains("NOTHROWITEM".ToUpper()));
                    newMapInfo.NoPlayerDrop = mapAttributes.Any(s => s.Contains("NOPLAYERDROP".ToUpper()));
                    newMapInfo.NoMonsterDrop = mapAttributes.Any(s => s.Contains("NOMONSTERDROP".ToUpper()));
                    newMapInfo.NoNames = mapAttributes.Any(s => s.Contains("NONAMES".ToUpper()));
                    newMapInfo.Fight = !mapAttributes.Any(s => s.Contains("SAFE".ToUpper()));
                    newMapInfo.Fire = mapAttributes.Any(x => x.StartsWith("FIRE".ToUpper()));
                    newMapInfo.Lightning = mapAttributes.Any(x => x.StartsWith("LIGHTNING".ToUpper()));
                    newMapInfo.MiniMap = mapAttributes.Any(x => x.StartsWith("MINIMAP".ToUpper()));
                    newMapInfo.BigMap = mapAttributes.Any(x => x.StartsWith("BIGMAP".ToUpper()));
                    newMapInfo.Mine = mapAttributes.Any(s => s.Contains("MINE".ToUpper()));
                    newMapInfo.Light = LightSetting.Normal;


                    if (newMapInfo.NoReconnect == true) // If there is a NORECONNECT attribute get its MapFile
                        newMapInfo.ReconnectMap = mapAttributes[nri].TrimStart("NORECONNECT(".ToCharArray()).TrimEnd(')');
                    if (newMapInfo.Fire == true) // If there is a FIRE attribute get its value
                        newMapInfo.FireDamage = Convert.ToInt16(mapAttributes[fi].TrimStart("FIRE(".ToCharArray()).TrimEnd(')'));
                    if (newMapInfo.Lightning == true) // If there is a LIGHTNING attribute get its value
                        newMapInfo.LightningDamage = Convert.ToInt16(mapAttributes[li].TrimStart("LIGHTNING(".ToCharArray()).TrimEnd(')'));

                    if (newMapInfo.MiniMap == true) // If there is a MINIMAP attribute get its value
                        newMapInfo.MiniMapNumber = Convert.ToUInt16(mapAttributes[mmi].TrimStart("MINIMAP(".ToCharArray()).TrimEnd(')'));
                    if (newMapInfo.BigMap == true) // If there is a BIGMAP attribute get its value
                        newMapInfo.BigMapNumber = Convert.ToUInt16(mapAttributes[bmi].TrimStart("BIGMAP(".ToCharArray()).TrimEnd(')'));
                    if (lighti != -1) // Check if there is a LIGHT attribute and get its value
                    {
                        switch (mapAttributes[lighti].TrimStart("LIGHT(".ToCharArray()).TrimEnd(')'))
                        {
                            case "Dawn":
                                newMapInfo.Light = LightSetting.Dawn;
                                break;
                            case "Day":
                                newMapInfo.Light = LightSetting.Day;
                                break;
                            case "Evening":
                                newMapInfo.Light = LightSetting.Evening;
                                break;
                            case "Night":
                                newMapInfo.Light = LightSetting.Night;
                                break;
                            case "Normal":
                                newMapInfo.Light = LightSetting.Normal;
                                break;
                            default:
                                newMapInfo.Light = LightSetting.Normal;
                                break;
                        }
                    }

                    // Check for light type
                    if (mapAttributes.Any(s => s.Contains("DAY".ToUpper()))) // DAY = Day
                        newMapInfo.Light = LightSetting.Day;
                    else if (mapAttributes.Any(s => s.Contains("DARK".ToUpper()))) // DARK = Night
                        newMapInfo.Light = LightSetting.Night;

                    MapInfo.Add(newMapInfo); // Add map to list
                }
            }

            for (int j = 0; j < MapInfo.Count; j++)
            {
                for (int k = 0; k < lines.Length; k++)
                {
                    try
                    {
                        if (lines[k].StartsWith(MapInfo[j].MapFile + " "))
                        {
                            MapMovements newmapMovements = new MapMovements();

                            lines[k] = lines[k].Replace('.', ','); // Replace point with comma
                            lines[k] = lines[k].Replace(":", ","); // Replace colon with comma
                            lines[k] = lines[k].Replace(", ", ","); // Remove space after comma
                            lines[k] = lines[k].Replace(" ,", ","); // Remove space before comma
                            lines[k] = System.Text.RegularExpressions.Regex.Replace(lines[k], @"\s+", " "); // Clear whitespace
                            lines[k] = lines[k].Replace(" ;", ";"); // Remove space before semi-colon

                            // Trim comment at the end of the line
                            if (lines[k].Contains(';'))
                                lines[k] = lines[k].Substring(0, lines[k].IndexOf(";", System.StringComparison.Ordinal));

                            var c = lines[k].Split(' ');

                            // START - Get values from line
                            if (c.Length == 7) // Every value has a space
                            {
                                c[1] = c[1] + "," + c[2];
                                c[2] = c[5] + "," + c[6];
                                c[3] = c[4];
                            }
                            else if (c.Length == 6) // One value has a space
                            {
                                if (c[2] == "->") // Space in to XY
                                {
                                    c[2] = c[4] + "," + c[5];
                                }
                                else if (c[3] == "->") // Space in from XY
                                {
                                    c[1] = c[1] + "," + c[2];
                                    c[2] = c[5];
                                    c[3] = c[4];
                                }
                            }
                            else if (c.Length == 5) // Proper format
                            {
                                c[2] = c[4];
                            }
                            else // Unreadable value count
                            {
                                continue;
                            }
                            // END - Get values from line

                            string[] d = c[1].Split(',');

                            string[] e = c[2].Split(',');

                            newmapMovements.fromIndex = MapInfo[MapInfo.FindIndex(a => a.MapFile.ToString() == MapInfo[j].MapFile)].Index; // Check MapInfo for MapFile (mapInfo[j].mapFile) and get it's index number
                            newmapMovements.fromX = d[0];
                            newmapMovements.fromY = d[1];
                            newmapMovements.toMap = MapInfo[MapInfo.FindIndex(a => a.MapFile.ToString() == c[3])].Index; // Check MapInfo for MapFile (c[3]) and get it's index number
                            newmapMovements.toX = e[0];
                            newmapMovements.toY = e[1];

                            MapMovements.Add(newmapMovements); // Add movements
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }

        public static void End()
        {
            MapInfo.Clear();
            MapMovements.Clear();
        }
    }


    public class MapInfo
    {
        public LightSetting
            Light;

        public int
            Index,
            FireDamage,
            LightningDamage;

        public ushort
            MiniMapNumber,
            BigMapNumber;

        public string
            MapFile,
            MapName,
            ReconnectMap = string.Empty; // for no reconnect

        public bool
            NoTeleport,
            NoReconnect,
            NoRandom,
            NoEscape,
            NoRecall,
            NoDrug,
            NoPositionMove,
            NoThrowItem,
            NoPlayerDrop,
            NoMonsterDrop,
            NoNames,
            Fight,
            Fire,
            Lightning,
            MiniMap,
            BigMap,
            Mine;

        public List<MapMovements>
            MapMovements = new List<MapMovements>();
    }

    public class MapMovements
    {
        public int
            fromIndex,
            toMap;

        public string
             fromX,
             fromY,

             toX,
             toY;
    }
}