namespace finalProject
{
    internal static class InteractiveMenu
    {
        public static void Run()
        {
            while (true)
            {
                PrintMenu();
                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": InputWords(); break;
                    case "2": LoadWordsFile(); break;
                    case "3": SetSearchPath(); break;
                    case "4": SetOutput(); break;
                    case "5": SetExtensions(); break;
                    case "6": ToggleCase(); break;
                    case "7": Scanner.StartScan(); break;
                    case "8":
                        Console.WriteLine("До побачення!");
                        return;
                    default:
                        Console.WriteLine("Невірний вибір.");
                        break;
                }
            }
        }

        static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("МЕНЮ");
            Console.WriteLine("1)Ввести слова вручну");
            Console.WriteLine("2)Завантажити слова з файлу");
            Console.WriteLine("3)Папка пошуку");
            Console.WriteLine("4)Папка результатів");
            Console.WriteLine("5)Розширення(.txt,.cs,.xml,.json,.log,.md)");
            Console.WriteLine("6) Регістр");
            Console.WriteLine("7)ЗАПУСТИТИ СКАНУВАННЯ");
            Console.WriteLine("8)Вийти");
            Console.Write("Вибір: ");
        }


        static void InputWords()
        {
            Console.WriteLine("Введіть заборонені слова: ");
            AppConfig.Words.Clear();
            while (true)
            {
                Console.Write($" Слово: {AppConfig.Words.Count + 1}: ");
                string? word = Console.ReadLine();
                if (string.IsNullOrEmpty(word)) break;
                AppConfig.Words.Add(word);
                Console.WriteLine($"  Додано: {word}");
            }
            Console.WriteLine($"Всього слів: {AppConfig.Words.Count}");
        }
        static void LoadWordsFile()
        {
            Console.Write("Шлях до файлу зі словами: ");
            string? path = Console.ReadLine();
            if (!File.Exists(path))
            { 
                Console.WriteLine("Файл не знайдено"); 
                return; 
            }
            AppConfig.LoadWordsFromFile(path!);
            Console.WriteLine($"Завантажено {AppConfig.Words.Count} слів.");
        }

        static void SetSearchPath()
        {
            Console.Write("Папка де шукати: ");
            string? path = Console.ReadLine();

            if (path == "" || path == null )
                AppConfig.SearchPath = "";
            else
                AppConfig.SearchPath = path;

            if (AppConfig.SearchPath == "")
                Console.WriteLine("Буде шукати по всьому диску.");
            else
                Console.WriteLine($"Встановлено: {AppConfig.SearchPath}");
        }

        static void SetOutput()
        {
            Console.Write("Папка для результатів: ");
            string? path = Console.ReadLine();
            if (path != null) AppConfig.SetOutput(path);
            Console.WriteLine($"Встановлено: {AppConfig.Output}");
        }


        static void SetExtensions()
        {
            Console.Write("Розширення через кому (.txt,.cs,.log): ");
            string? line = Console.ReadLine();
            if (line != "" && line != null) AppConfig.SetExtensions(line);
            Console.Write("Встановлено: ");
            foreach (string ext in AppConfig.Exts)
                Console.Write(ext + " ");
            Console.WriteLine();
        }


        static void ToggleCase()
        {
            AppConfig.ToggleCase();
            if (AppConfig.CaseSens)
                Console.WriteLine("Регістр: чутливий");
            else
                Console.WriteLine("Регістр: ігнорувати");
        }

        public static void RunFromArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--words":
                        if (i + 1 < args.Length)
                            AppConfig.Words.AddRange(
                                args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "--wordfile":
                        if (i + 1 < args.Length && File.Exists(args[i + 1]))
                            AppConfig.LoadWordsFromFile(args[++i]);
                        break;
                    case "--search":
                        if (i + 1 < args.Length) AppConfig.SetSearchPath(args[++i]);
                        break;
                    case "--output":
                        if (i + 1 < args.Length) AppConfig.SetOutput(args[++i]);
                        break;
                    case "--ext":
                        if (i + 1 < args.Length) AppConfig.SetExtensions(args[++i]);
                        break;
                    case "--case":
                        AppConfig.CaseSens = true;
                        break;
                }
            }

            if (AppConfig.Words.Count == 0)
            {
                Console.WriteLine("Вкажіть слова: --words слово1,слово2");
                ConsoleUI.PrintUsage();
                return;
            }

            AppConfig.EnsureOutputDefault();
            Scanner.StartScan();
        }
    }
}
