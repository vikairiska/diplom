namespace VerhozinaIvanovDiplom
{
    /// <summary>
    /// Допустимые специализации и города для профиля пользователя (не роли доступа в системе).
    /// </summary>
    public static class UserSpecialtyOptions
    {
        /// <summary>Пункт фильтра «без ограничения».</summary>
        public const string FilterAllLabel = "Все";

        public static readonly string[] Specialties =
        {
            "Специалист по детейлингу",
            "Специалист по электрике",
            "Механик"
        };

        public static readonly string[] Cities =
        {
            "Москва",
            "Воронеж",
            "Иркутск"
        };
    }
}
