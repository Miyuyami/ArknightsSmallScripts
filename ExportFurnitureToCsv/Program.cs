using System;
using System.IO;
using System.Linq;
using Arknights.Data;

namespace ExportFurnitureToCsv
{
    public class Program
    {
        private static void Main(string[] args)
        {
            string exportFolder;
            if (args.Length >= 1)
            {
                exportFolder = args[0];
            }
            else
            {
                exportFolder = Directory.GetCurrentDirectory();
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
                Directory.CreateDirectory(exportFolder);
                var baseData = BaseData.FromJson(File.ReadAllText(buildingDataJsonPath));
                var customData = baseData.CustomData;

                using var themesWriter = new StreamWriter(File.OpenWrite(Path.Combine(exportFolder, "themes.csv")));
                themesWriter.WriteCsvRow("Name", "Total Ambience", "Total Cost");
                foreach (var theme in customData.Themes.Values)
                {
                    themesWriter.WriteCsvRow(theme.Name, CalculateAmbience(customData, theme), CalculateCost(customData, theme));
                }

                using var setsWriter = new StreamWriter(File.OpenWrite(Path.Combine(exportFolder, "sets.csv")));
                setsWriter.WriteCsvRow("Name", "Ambience", "Total Ambience", "Total Cost");
                foreach (var group in customData.Groups.Values)
                {
                    var theme = customData.Themes[group.ThemeId];

                    setsWriter.WriteCsvRow(group.Name, group.Comfort, CalculateAmbience(customData, group), CalculateCost(customData, group));
                }

                using var furnituresWriter = new StreamWriter(File.OpenWrite(Path.Combine(exportFolder, "furnitures.csv")));
                furnituresWriter.WriteCsvRow("Name", "Set Name", "Theme Name", "Ambience", "Set Ambience", "Cost", "Location", "Rarity");
                foreach (var furniture in customData.Furnitures.Values)
                {
                    var set = GetSet(customData, furniture);
                    var theme = GetTheme(customData, furniture);
                    int setAmbience = set?.Comfort ?? 0;

                    furnituresWriter.WriteCsvRow(furniture.Name, set?.Name ?? "No Set", theme?.Name ?? "No Theme", furniture.Comfort, setAmbience, CalculateCost(furniture), furniture.Location, furniture.Rarity);
                }

                using var furnituresDumpWriter = new StreamWriter(File.OpenWrite(Path.Combine(exportFolder, "furnitures_dump.csv")));
                furnituresDumpWriter.WriteCsvRow("Name", "Location", "Type", "Category", "Rarity", "Ambience", "Cost");
                foreach (var furniture in customData.Furnitures.Values)
                {
                    furnituresDumpWriter.WriteCsvRow(furniture.Name, furniture.Location, furniture.Type, furniture.Category, furniture.Rarity, furniture.Comfort, CalculateCost(furniture));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine();
                DisplayHelp();
            }
        }

        private static void WriteCsvRow(TextWriter textWriter, params object[] data)
        {
            textWriter.WriteLine(String.Join(',', data));
        }

        private static int CalculateAmbience(CustomData customData, Theme theme)
        {
            var groupsInTheme = customData.Groups.Values.Where(g => g.ThemeId == theme.Id);

            return groupsInTheme.Sum(g => CalculateAmbience(customData, g));
        }

        private static int CalculateAmbience(CustomData customData, Group group)
        {
            int piecesAmbience = group.Furniture.Sum(s => customData.Furnitures[s].Comfort);
            int setAmbience = group.Comfort;

            return piecesAmbience + setAmbience;
        }

        private static int CalculateCost(CustomData customData, Theme theme)
        {
            var groupsInTheme = customData.Groups.Values.Where(g => g.ThemeId == theme.Id);

            return groupsInTheme.Sum(g => CalculateCost(customData, g));
        }

        private static int CalculateCost(CustomData customData, Group group)
        {
            int piecesCost = group.Furniture.Sum(s => CalculateCost(customData.Furnitures[s]));

            return piecesCost;
        }

        private static int CalculateCost(Furniture furniture)
        {
            var value = furniture.ProcessedProductCount * 2;
            value -= value % 5;
            return value;
        }

        private static Group GetSet(CustomData customData, Furniture furniture)
        {
            return customData.Groups.Values.SingleOrDefault(g => g.Furniture.Contains(furniture.Id));
        }

        private static Theme GetTheme(CustomData customData, Furniture furniture)
        {
            var set = GetSet(customData, furniture);
            if (set == null)
            {
                return null;
            }

            return customData.Themes[set.ThemeId];
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("needs at least 1 argument as following:");
            Console.WriteLine("- 1: (optional) folder where to export the .csv files, empty to default to current folder");
            Console.WriteLine("- 2: (optional) path to building_data.json; empty to default to \"GameData\\building_data.json\"");
        }
    }
}
