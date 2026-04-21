using System.Threading;

namespace finalProject
{
    internal static class SyncState
    {
        public static readonly object ConsoleLock = new();
        public static CancellationTokenSource Cts = new();
        public static int Replacements;
        public static int Scanned;
        public static int Found;

        public static void Reset()
        {
            Cts = new CancellationTokenSource();
            Scanned = 0;
            Found = 0;
        }
    }
}
