namespace HQStudio.Services
{
    /// <summary>Единое правило: кого считать администратором. Им пользуются меню, страница «Сайт» и инструкция.</summary>
    public static class AdminAccess
    {
        public const string AdminRole = "Admin";

        public static bool IsAdmin(string? role) =>
            string.Equals(role?.Trim(), AdminRole, StringComparison.OrdinalIgnoreCase);

        public static bool IsCurrentUserAdmin() => IsAdmin(DataService.Instance.CurrentUser?.Role);
    }
}
