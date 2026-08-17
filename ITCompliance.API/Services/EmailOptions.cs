namespace ITCompliance.API.Services
{
    // Non-secret structural keys live in appsettings.json under
    // "Email". Password is never checked in - dotnet user-secrets
    // set "Email:Password" "..." in dev, Email__Password env var
    // in prod, same pattern as ConnectionStrings:DefaultConnection.
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "IT Compliance Portal";

        // When true, every email is redirected to DevModeRedirectTo
        // instead of its real recipients - the subject line still
        // shows who it would have gone to. Set via
        // appsettings.Development.json only, so it's on for local
        // testing and off by default in Production.
        public bool DevMode { get; set; } = false;
        public string DevModeRedirectTo { get; set; } = "zayan.shahid@orient-power.com";
    }
}
