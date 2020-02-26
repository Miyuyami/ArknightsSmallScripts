using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arknights.Data;

namespace Arknights.DisplayFurnitureThemeFastSetup
{
    public class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                DisplayHelp();
                return;
            }

            var baseData = BaseData.FromJson(File.ReadAllText(@"GameData\building_data.json"));

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

            var ordered = d.OrderBy(kvp => customData.Groups.Values.Select((g, i) => (g, i))
                                                                   .First(t => t.g.Furniture.Contains(kvp.Key)).i)
                           .ThenBy(kvp => customData.Groups.Values.SelectMany(g => g.Furniture.Select((s, i) => (s, i)))
                                                                   .First(t => t.s == kvp.Key).i);

            foreach (var kvp in ordered)
            {
                string name = customData.Furnitures[kvp.Key].Name;
                int count = kvp.Value;

                Console.WriteLine($"{count} - {name}");
            }
        }

        private static int CalculateAmbience(CustomData customData, Dictionary<string, int> d)
        {
            int piecesAmbience = d.Sum(kvp => customData.Furnitures[kvp.Key].Comfort * kvp.Value);
            int setsAmbience = d.Select(kvp => customData.Groups.Values.First(g => g.Furniture.Contains(kvp.Key)).Id)
                                .ToHashSet()
                                .Sum(s => customData.Groups[s].Comfort);

            return piecesAmbience + setsAmbience;
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("needs at least 1 argument as following:");
            Console.WriteLine("- furniture theme ID or NAME or \"all\" for all");
        }
    }
}
