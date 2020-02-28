using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arknights.Data;

namespace Arknights.DisplayFurnitureThemeFastSetup
{
    public class Program
    {
        // ColumnName - min padding
        private static Dictionary<string, int> Columns = new Dictionary<string, int>()
        {
            { "Count", 1 },
            { "Furniture Name", 1 },
            { "Ambience", 4 },
            { "Cost", 4 },
        };

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                DisplayHelp();
                return;
            }

            string buildingDataJsonPath;
            if (args.Length >= 2)
            {
                buildingDataJsonPath = args[1];
            }
            else
            {
                buildingDataJsonPath = @"GameData\building_data.json";
            }

            try
            {
                var baseData = BaseData.FromJson(File.ReadAllText(buildingDataJsonPath));

                if (args[0] == "all")
                {
                    DisplayAllThemes(baseData.CustomData);
                    return;
                }

                if (!baseData.CustomData.Themes.TryGetValue(args[0], out Theme theme))
                {
                    Console.WriteLine("theme id not found");
                    Console.WriteLine("looking for theme by name");

                    theme = baseData.CustomData.Themes.Values.FirstOrDefault(t => t.Name.Contains(args[0], StringComparison.OrdinalIgnoreCase));
                    if (theme == null)
                    {
                        Console.WriteLine("theme name not found");
                        Console.WriteLine();
                        DisplayHelp();
                        return;
                    }
                }

                DisplayTheme(baseData.CustomData, theme);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine();
                DisplayHelp();
            }
        }

        private static void DisplayAllThemes(CustomData customData)
        {
            foreach (var theme in customData.Themes.Values)
            {
                DisplayTheme(customData, theme);
                Console.WriteLine();
            }
        }

        private static void DisplayTheme(CustomData customData, Theme theme)
        {
            Console.WriteLine("Theme name: " + theme.Name);

            var d = new Dictionary<string, int>();

            foreach (var qs in theme.QuickSetup)
            {
                if (d.TryGetValue(qs.FurnitureId, out int count))
                {
                    d[qs.FurnitureId] = count + 1;
                }
                else
                {
                    d[qs.FurnitureId] = 1;
                }
            }

            Console.WriteLine("Total ambience: " + CalculateAmbience(customData, d));
            Console.WriteLine("Total cost: " + CalculateCost(customData, d));

            var ordered = d.OrderBy(kvp => customData.Groups.Values.Select((g, i) => (g, i))
                                                                   .First(t => t.g.Furniture.Contains(kvp.Key)).i)
                           .ThenBy(kvp => customData.Groups.Values.SelectMany(g => g.Furniture.Select((s, i) => (s, i)))
                                                                   .First(t => t.s == kvp.Key).i)
                           .ToList();

            var longestName = ordered.Max(kvp => customData.Furnitures[kvp.Key].Name.Length);
            var columnPaddings = new List<int>()
            {
                CalculatePadding(1),
                CalculatePadding(2, longestName),
                CalculatePadding(3),
                CalculatePadding(4),
            };
            var columnHeaders = Columns.Keys.ToList();

            Console.WriteLine(new string('-', columnPaddings.Sum() + columnPaddings.Count * 3));
            Console.WriteLine(
                $"{columnHeaders[1].PadRight(columnPaddings[1])} | " +
                $"{columnHeaders[0].PadRight(columnPaddings[0])} | " +
                $"{columnHeaders[2].PadRight(columnPaddings[2])} | " +
                $"{columnHeaders[3].PadRight(columnPaddings[3])}"
            );
            Console.WriteLine(new string('-', columnPaddings.Sum() + columnPaddings.Count * 3));

            foreach (var kvp in ordered)
            {
                var furniture = customData.Furnitures[kvp.Key];
                string name = furniture.Name;
                int count = kvp.Value;

                Console.WriteLine(
                    $"{name.PadRight(columnPaddings[1])} | " +
                    $"{count.ToString().PadLeft(columnPaddings[0])} | " +
                    $"{CalculateAmbience(furniture).ToString().PadLeft(columnPaddings[2])} | " +
                    $"{CalculateCost(furniture).ToString().PadLeft(columnPaddings[3])}"
                );
            }
        }

        private static int CalculatePadding(int columnNumber, int minPadding = 0)
        {
            int columnIndex = columnNumber - 1;
            int columnNamePadding = Columns.Keys.Skip(columnIndex).First().Length;
            int columnMinPadding = Columns.Values.Skip(columnIndex).First();

            return Math.Max(minPadding, Math.Max(columnNamePadding, columnMinPadding));
        }

        private static int CalculateAmbience(CustomData customData, Dictionary<string, int> d)
        {
            int piecesAmbience = d.Sum(kvp => CalculateAmbience(customData.Furnitures[kvp.Key]) * kvp.Value);
            int setsAmbience = d.Select(kvp => customData.Groups.Values.First(g => g.Furniture.Contains(kvp.Key)).Id)
                                .ToHashSet()
                                .Sum(s => customData.Groups[s].Comfort);

            return piecesAmbience + setsAmbience;
        }

        private static int CalculateAmbience(Furniture furniture)
        {
            return furniture.Comfort;
        }

        private static int CalculateCost(CustomData customData, Dictionary<string, int> d)
        {
            int piecesCost = d.Sum(kvp => CalculateCost(customData.Furnitures[kvp.Key]) * kvp.Value);

            return piecesCost;
        }

        private static int CalculateCost(Furniture furniture)
        {
            var value = furniture.ProcessedProductCount * 2;
            value -= value % 5;
            return value;
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("needs at least 1 argument as following:");
            Console.WriteLine("- 1: furniture theme ID or NAME or \"all\" for all");
            Console.WriteLine("- 2: (optional) path to building_data.json; empty for default \"GameData\\building_data.json\"");
        }
    }
}
