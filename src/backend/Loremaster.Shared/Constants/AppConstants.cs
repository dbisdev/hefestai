namespace Loremaster.Shared.Constants;

public static class AppConstants
{
    public const string ApplicationName = "Loremaster";
    public const string ApiVersion = "v1";
    
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class Policies
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireUser = "RequireUser";
    }

    public static class Cache
    {
        public const int DefaultExpirationMinutes = 5;
        public const int LongExpirationMinutes = 30;
    }
}
