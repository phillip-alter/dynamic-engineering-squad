namespace InfrastructureApp.Services
{
    public static class AvatarCatalog
    {

        //My allowed avatars
        //Prevents someone from saving random strings (security)
        public static readonly IReadOnlyList<string> Keys = new List<string>
        {
            "boy_blue_hair",
            "boy_green_hair",
            "boy_purple_hair",
            "girl_brown_hair",
            "girl_pink_hair",
            "girl_purple_hair"
        };

        public static string ToUrl(string? key)
        {
            if(string.IsNullOrWhiteSpace(key) || !Keys.Contains(key))
                return "/images/avatar/boy_blue_hair.png";

            return $"/images/avatar/{key}.png";    
        }

        public static bool IsValid(string? key)
            => !string.IsNullOrWhiteSpace(key) && Keys.Contains(key);
    }
}