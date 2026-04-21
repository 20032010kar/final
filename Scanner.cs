using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace finalProject
{
    internal static class Scanner
    {
        private static bool _isPaused = false;
        private static Dictionary<string, int> _freq = new Dictionary<string, int>();

        public static void StartScan()
        {
            if (AppConfig.Words.Count == 0)
            {
                Console.WriteLine("[!] Спочатку введіть заборонені слова!");
                return;
            }

            AppConfig.EnsureOutputDefault();
            SyncState.Reset();
            _freq.Clear();
            _isPaused = false;
            Directory.CreateDirectory(AppConfig.Output);

            string reportPath = Path.Combine(AppConfig.Output, "report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

            Console.WriteLine();
            Console.WriteLine("--- СТАРТ СКАНУВАННЯ ---");
            Console.Write("Слова: ");
            foreach (string w in AppConfig.Words)
                Console.Write(w + " ");
            Console.WriteLine();

            if (AppConfig.SearchPath == "")
                Console.WriteLine("Шукати в: не вказано папку!");
            else
                Console.WriteLine("Шукати в: " + AppConfig.SearchPath);

            Console.WriteLine("Результати: " + AppConfig.Output);
            Console.WriteLine("P = пауза   S = зупинити");
            Console.WriteLine();

            var keyThread = new Thread(KeyListener) { IsBackground = true };
            keyThread.Start();

            var scanThread = new Thread(() => DoScan(reportPath)) { IsBackground = false };
            scanThread.Start();
            scanThread.Join();

            SyncState.Cts.Cancel();
        }

        static void KeyListener()
        {
            while (!SyncState.Cts.IsCancellationRequested)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var k = Console.ReadKey(intercept: true).Key;

                if (k == ConsoleKey.P)
                {
                    if (!_isPaused)
                    {
                        _isPaused = true;
                        Console.WriteLine("ПАУЗА. Натисніть P для продовження.");
                    }
                    else
                    {
                        _isPaused = false;
                        Console.WriteLine("ПРОДОВЖЕНО.");
                    }
                }
                else if (k == ConsoleKey.S)
                {
                    SyncState.Cts.Cancel();
                    _isPaused = false;
                    Console.WriteLine("ЗУПИНКА...");
                }
            }
        }

        static void DoScan(string reportPath)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var report = new StreamWriter(reportPath, false, Encoding.UTF8);
            report.WriteLine("=== Word Scanner - Звіт ===");
            report.WriteLine("Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            report.Write("Слова: ");
            foreach (string w in AppConfig.Words)
                report.Write(w + " ");
            report.WriteLine();
            report.WriteLine(new string('-', 50));

            try
            {
                if (AppConfig.SearchPath != "")
                {
                    if (!Directory.Exists(AppConfig.SearchPath))
                    {
                        Console.WriteLine("[!] Папка не існує: " + AppConfig.SearchPath);
                        return;
                    }
                    ScanFolder(AppConfig.SearchPath, report);
                }
                else
                {
                    Console.WriteLine("[!] Вкажіть папку для пошуку!");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Помилка: " + ex.Message);
            }

            sw.Stop();
            PrintResults(sw.Elapsed, reportPath, report);
        }

        static void ScanFolder(string folder, StreamWriter report)
        {
            List<string> files = GatherFiles(folder);
            Console.WriteLine("Файлів для перевірки: " + files.Count);
            report.WriteLine("Папка: " + folder + "  Файлів: " + files.Count);

            long total = files.Count;
            long done = 0;

            List<Task> tasks = new List<Task>();
            foreach (string f in files)
            {
                string fileCopy = f;
                Task t = Task.Run(() =>
                {
                    while (_isPaused)
                    {
                        if (SyncState.Cts.IsCancellationRequested) return;
                        Thread.Sleep(200);
                    }

                    if (SyncState.Cts.IsCancellationRequested) return;

                    ProcessFile(fileCopy, report);

                    lock (SyncState.ConsoleLock)
                    {
                        done++;
                        ShowProgress(done, total);
                    }
                });
                tasks.Add(t);
            }

            try
            {
                Task.WaitAll(tasks.ToArray(), SyncState.Cts.Token);
            }
            catch (OperationCanceledException) { }
        }

        static List<string> GatherFiles(string root)
        {
            List<string> list = new List<string>();
            try
            {
                string normalizedOutput = AppConfig.Output.Replace('/', '\\');
                if (!normalizedOutput.EndsWith("\\"))
                    normalizedOutput += "\\";

                foreach (string ext in AppConfig.Exts)
                {
                    foreach (string f in Directory.EnumerateFiles(root, "*" + ext, SearchOption.AllDirectories))
                    {
                        if (SyncState.Cts.IsCancellationRequested) break;

                        string normalizedFile = f.Replace('/', '\\');

                        int idx = string.Compare(normalizedFile, 0, normalizedOutput, 0, normalizedOutput.Length, ignoreCase: true);
                        if (idx != 0)
                            list.Add(f);
                    }
                }
            }
            catch { }
            return list;
        }

        static void ProcessFile(string path, StreamWriter report)
        {
            try
            {

                string content = File.ReadAllText(path, Encoding.UTF8);

                bool ignoreCase = !AppConfig.CaseSens;

                Dictionary<string, int> found = new Dictionary<string, int>();
                foreach (string w in AppConfig.Words)
                {
                    int n = CountOccurrences(content, w, ignoreCase);
                    if (n > 0) found[w] = n;
                }

                lock (SyncState.ConsoleLock) { SyncState.Scanned++; }
                if (found.Count == 0) return;

                int total = 0;
                foreach (int v in found.Values) total += v;

                string redacted = content;
                foreach (string w in found.Keys)
                {
                    while (redacted.IndexOf(w, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0)
                    {
                        int idx = redacted.IndexOf(w, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                        redacted = redacted.Substring(0, idx) + "*******" + redacted.Substring(idx + w.Length);
                    }

                    lock (_freq)
                    {
                        if (_freq.ContainsKey(w))
                            _freq[w] += found[w];
                        else
                            _freq[w] = found[w];
                    }
                }

                string safe = path.Replace(':', '_').Replace('\\', '_').Replace('/', '_');
                while (safe.Length > 0 && safe[0] == '_')
                    safe = safe.Substring(1);

                string origDst = Path.Combine(AppConfig.Output, "originals", safe);
                string redDst = Path.Combine(AppConfig.Output, "redacted", safe);

                Directory.CreateDirectory(Path.GetDirectoryName(origDst)!);
                Directory.CreateDirectory(Path.GetDirectoryName(redDst)!);
                File.Copy(path, origDst, true);
                File.WriteAllText(redDst, redacted, Encoding.UTF8);

                lock (SyncState.ConsoleLock)
                {
                    SyncState.Found++;
                    SyncState.Replacements += total;

                    report.WriteLine("Файл: " + path);
                    report.WriteLine("  Замін: " + total);
                    foreach (var kv in found)
                        report.WriteLine("  '" + kv.Key + "' — " + kv.Value + " разів");

                    Console.WriteLine();
                    Console.WriteLine("  Знайдено: " + Path.GetFileName(path) + " (" + total + " замін)");
                }
            }
            catch { }
        }

        static void ShowProgress(long done, long total)
        {
            if (total == 0) return;

            int width = 50;
            int percent = (int)((double)done / total * 100);
            int filled = percent * width / 100;

            string bar = "["
                + new string('#', filled)
                + new string(' ', width - filled)
                + $"] {percent}% ({done}/{total})";

            Console.CursorLeft = 0;
            Console.Write(bar);
        }

        static void PrintResults(TimeSpan elapsed, string reportPath, StreamWriter report)
        {
            List<KeyValuePair<string, int>> top10 = new List<KeyValuePair<string, int>>(_freq);
            top10.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (top10.Count > 10)
                top10 = top10.GetRange(0, 10);

            Console.WriteLine();
            Console.WriteLine("--- РЕЗУЛЬТАТИ ---");
            Console.WriteLine("Перевірено файлів : " + SyncState.Scanned);
            Console.WriteLine("Знайдено файлів   : " + SyncState.Found);
            Console.WriteLine("Замін зроблено    : " + SyncState.Replacements);
            Console.WriteLine("Час роботи        : " + elapsed.ToString(@"hh\:mm\:ss"));

            if (top10.Count > 0)
            {
                Console.WriteLine("ТОП слів:");
                for (int i = 0; i < top10.Count; i++)
                    Console.WriteLine("  " + (i + 1) + ". " + top10[i].Key + " — " + top10[i].Value + " разів");
            }

            Console.WriteLine("Звіт: " + reportPath);

            report.WriteLine(new string('-', 50));
            report.WriteLine("ТОП-10:");
            for (int i = 0; i < top10.Count; i++)
                report.WriteLine("  " + (i + 1) + ". " + top10[i].Key + " — " + top10[i].Value + " разів");
            report.WriteLine("Перевірено: " + SyncState.Scanned + ", знайдено: " + SyncState.Found + ", замін: " + SyncState.Replacements);
            report.WriteLine("Час: " + elapsed.ToString(@"hh\:mm\:ss"));
        }

        static int CountOccurrences(string src, string word, bool ignoreCase)
        {
            int count = 0;
            int i = 0;
            while ((i = src.IndexOf(word, i, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) >= 0)
            {
                count++;
                i += word.Length;
            }
            return count;
        }
    }
}
