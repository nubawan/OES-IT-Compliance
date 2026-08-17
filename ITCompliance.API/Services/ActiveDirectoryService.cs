using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.RegularExpressions;

namespace ITCompliance.API.Services
{
    public enum AdAuthStatus
    {
        Success,

        // Credentials are right but the account has a problem
        PasswordExpired,
        PasswordMustChange,
        AccountLocked,
        AccountDisabled,

        InvalidCredentials,
        UserNotFound,
        ServerUnavailable,
        UnknownError
    }

    public class AdAuthResult
    {
        public AdAuthStatus Status { get; init; }

        public bool Success =>
            Status == AdAuthStatus.Success;

        public string Detail { get; init; } = "";
    }

    public class ActiveDirectoryService
    {
        // Overridable via config key Ldap:Server
        // (appsettings or Ldap__Server environment variable)
        // without a code change.
        private readonly string _ldapServer;

        private readonly ILogger<ActiveDirectoryService> _logger;

        private const int LdapPort = 389;

        // LDAP_INVALID_CREDENTIALS - AD packs a more specific
        // sub-error code into the error data ("data 532" etc).
        private const int ErrorInvalidCredentials = 49;

        // AD sub-error codes returned with error 49
        private static readonly Dictionary<string, AdAuthStatus>
            SubErrorMap = new()
            {
                ["525"] = AdAuthStatus.UserNotFound,
                ["52e"] = AdAuthStatus.InvalidCredentials,
                ["530"] = AdAuthStatus.InvalidCredentials,      // logon hours restriction
                ["531"] = AdAuthStatus.AccountDisabled,         // not permitted at this workstation
                ["532"] = AdAuthStatus.PasswordExpired,
                ["533"] = AdAuthStatus.AccountDisabled,
                ["701"] = AdAuthStatus.AccountDisabled,         // account expired
                ["773"] = AdAuthStatus.PasswordMustChange,
                ["775"] = AdAuthStatus.AccountLocked
            };

        public ActiveDirectoryService(
            IConfiguration configuration,
            ILogger<ActiveDirectoryService> logger)
        {
            _ldapServer =
                configuration["Ldap:Server"]
                ?? "DC-AD-01.orient-power.com";

            _logger = logger;
        }

        /// <summary>
        /// Authenticates against Active Directory.
        /// Accepts a full email OR a plain AD username.
        /// If the input is an email, the local part (before @)
        /// is retried as an sAMAccountName - some domains have
        /// UPNs that do not match the email addresses.
        /// </summary>
        public AdAuthResult Authenticate(
            string emailOrUsername,
            string password)
        {
            var identity = (emailOrUsername ?? "").Trim();

            var attempts = new List<string> { identity };

            if (identity.Contains('@'))
            {
                var localPart = identity.Split('@')[0];

                if (localPart.Length > 0 &&
                    !string.Equals(
                        localPart,
                        identity,
                        StringComparison.OrdinalIgnoreCase))
                {
                    attempts.Add(localPart);
                }
            }

            AdAuthResult last =
                new() { Status = AdAuthStatus.InvalidCredentials };

            foreach (var attempt in attempts)
            {
                last = TryBind(attempt, password);

                // Stop on anything that is not a plain
                // invalid-credentials/user-not-found result -
                // account problems and server problems will be
                // the same for every identity format.
                if (last.Status != AdAuthStatus.InvalidCredentials &&
                    last.Status != AdAuthStatus.UserNotFound)
                {
                    return last;
                }
            }

            return last;
        }

        private AdAuthResult TryBind(
            string identity,
            string password)
        {
            try
            {
                var identifier = new LdapDirectoryIdentifier(
                    _ldapServer,
                    LdapPort);

                using var connection =
                    new LdapConnection(identifier);

                connection.AuthType = AuthType.Negotiate;

                connection.SessionOptions.ProtocolVersion = 3;

                // Fail fast instead of hanging when the DC is
                // not reachable.
                connection.Timeout = TimeSpan.FromSeconds(5);

                connection.Credential = new NetworkCredential(
                    identity,
                    password);

                connection.Bind();

                _logger.LogInformation(
                    "AD login success for {Identity}",
                    identity);

                return new AdAuthResult
                {
                    Status = AdAuthStatus.Success
                };
            }
            catch (LdapException ex)
            {
                if (ex.ErrorCode == ErrorInvalidCredentials)
                {
                    var result = new AdAuthResult
                    {
                        Status = ParseSubError(
                            ex.ServerErrorMessage ?? ex.Message)
                    };

                    _logger.LogWarning(
                        "AD login rejected for {Identity}: {Status} " +
                        "(server error: {ServerError})",
                        identity,
                        result.Status,
                        ex.ServerErrorMessage ?? ex.Message);

                    return result;
                }

                _logger.LogError(
                    ex,
                    "AD server problem for {Identity} on {Server}",
                    identity,
                    _ldapServer);

                return new AdAuthResult
                {
                    Status = AdAuthStatus.ServerUnavailable,
                    Detail = ex.Message
                };
            }
            catch (Exception ex)
            {
                // TimeoutException, socket errors etc.
                _logger.LogError(
                    ex,
                    "AD connection failed for {Identity} on {Server}",
                    identity,
                    _ldapServer);

                return new AdAuthResult
                {
                    Status = AdAuthStatus.ServerUnavailable,
                    Detail = ex.Message
                };
            }
        }

        /// <summary>
        /// AD reports the fine-grained reason inside the error
        /// data, e.g. "... data 532, v2580" - 532 means the
        /// Windows password has expired.
        /// </summary>
        private static AdAuthStatus ParseSubError(
            string serverError)
        {
            if (string.IsNullOrEmpty(serverError))
            {
                return AdAuthStatus.InvalidCredentials;
            }

            var match = Regex.Match(
                serverError,
                @"data\s+([0-9a-fA-F]+)",
                RegexOptions.IgnoreCase);

            if (match.Success &&
                SubErrorMap.TryGetValue(
                    match.Groups[1].Value.ToLowerInvariant(),
                    out var status))
            {
                return status;
            }

            return AdAuthStatus.InvalidCredentials;
        }
    }
}
