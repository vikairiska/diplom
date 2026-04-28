namespace VerhozinaIvanovDiplom
{
    public static class SessionContext
    {
        public static int? CurrentUserId { get; set; }
        public static string CurrentUserLogin { get; set; }
        public static string CurrentRole { get; set; }

        public static bool IsAdmin => string.Equals(CurrentRole, "admin", System.StringComparison.OrdinalIgnoreCase);
        public static bool IsUser => string.Equals(CurrentRole, "user", System.StringComparison.OrdinalIgnoreCase);

        public static void Clear()
        {
            CurrentUserId = null;
            CurrentUserLogin = null;
            CurrentRole = null;
        }
    }
}
