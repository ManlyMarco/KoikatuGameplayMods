using System;
using System.Collections.Generic;
using System.Linq;
using ADV;

namespace KK_Pregnancy.Features
{
    internal static class CsvHelper
    {
        public static List<ScenarioData.Param> ReadFomCsv(string[] inputLines)
        {
            var results = inputLines
                .Where(row => !string.IsNullOrEmpty(row))
                .Select(row =>
                {
                    try
                    {
                        row = row.Trim();
                        if (row.Length == 0 || row.StartsWith("//")) return null;
                        if (!row.StartsWith("\"") || !row.EndsWith("\"")) throw new Exception("All values must be double quoted! Delimiters must be exactly \",\" with no spaces");

                        var items = row.Substring(1, row.Length - 2).Split(new string[] { "\",\"" }, StringSplitOptions.None);
                        return CreateFromCsv(items);
                    }
                    catch (Exception ex)
                    {
                        PregnancyPlugin.Logger.LogWarning($"Failed to read line - {row} because of exception: {ex}");
                        return null;
                    }
                }).Where(x => x != null).ToList();

            return results;
        }

        private static ScenarioData.Param CreateFromCsv(string[] csvFields)
        {
            var multi = bool.Parse(csvFields[0]);
            var command = (Command)Enum.Parse(typeof(Command), csvFields[1]);
            var args = csvFields.Skip(2).ToArray();
            var param = new ScenarioData.Param(multi, command, args);
            return param;
        }
    }
}
