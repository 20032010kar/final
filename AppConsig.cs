using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace finalProject
{
    internal static class AppConfig
    {
        public static List<string> Words = new();
        public static string Output = "";
        public static string SearchPath = "";
        public static string[] Exts = { ".txt", ".cs", ".xml", ".json", ".log", ".md" };
        public static bool CaseSens = false;

        public static void LoadWordsFromFile(string path)
        {
            Words = File.ReadAllLines(path)
                .Where(l => l.Length > 0)
                .ToList();
        }

        public static void SetOutput(string path)
        {
            if (path != "")
                Output = path;
        }

        public static void SetSearchPath(string path)
        {
            if (path != "")
                SearchPath = path;
        }

        public static void SetExtensions(string line)
        {
            var parts = line.Split(',')
                            .Select(x => x.Trim())
                            .Where(x => x.StartsWith("."))
                            .ToArray();
            if (parts.Length > 0)
                Exts = parts;
        }

        public static void ToggleCase()
        {
            CaseSens = !CaseSens;
        }

        public static void EnsureOutputDefault()
        {
            if (Output == "")
                Output = Directory.GetCurrentDirectory() + "\\WordScannerOutput";
        }
    }
}
