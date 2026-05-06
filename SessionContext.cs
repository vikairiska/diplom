namespace VerhozinaIvanovDiplom
{
    using System.Collections.Generic;

    public static class SessionContext
    {
        private static readonly HashSet<int> _readArticleIds = new HashSet<int>();

        public static int? CurrentUserId { get; set; }
        public static string CurrentUserLogin { get; set; }
        public static string CurrentRole { get; set; }

        public static bool IsAdmin => string.Equals(CurrentRole, "admin", System.StringComparison.OrdinalIgnoreCase);
        public static bool IsUser => string.Equals(CurrentRole, "user", System.StringComparison.OrdinalIgnoreCase);

        public static void MarkArticleAsRead(int articleId)
        {
            if (articleId > 0)
                _readArticleIds.Add(articleId);
        }

        public static bool IsArticleRead(int articleId)
        {
            return articleId > 0 && _readArticleIds.Contains(articleId);
        }

        public static void Clear()
        {
            CurrentUserId = null;
            CurrentUserLogin = null;
            CurrentRole = null;
            _readArticleIds.Clear();
        }
    }
}
