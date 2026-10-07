namespace HQStudio.Services.BugReport
{
    /// <summary>
    /// Не даёт показывать несколько окон "отправить отчёт" одновременно, когда ошибки сыплются подряд.
    /// </summary>
    public static class CrashPromptGuard
    {
        private static int _active;

        public static bool TryEnter() => Interlocked.CompareExchange(ref _active, 1, 0) == 0;

        public static void Leave() => Interlocked.Exchange(ref _active, 0);
    }
}
