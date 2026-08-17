using System.Security.Claims;
using ITCompliance.API.Models;

namespace ITCompliance.API.Services
{
    public static class ClaimsPrincipalExtensions
    {
        // Returns the department codes this user holds `role` for.
        // An empty list means the role is held globally/unscoped -
        // callers must already be inside an [Authorize(Roles=...)]
        // gate to tell that apart from "does not hold the role".
        public static IReadOnlyList<string> GetDepartmentScopes(
            this ClaimsPrincipal user,
            string role)
        {
            var prefix = role + "|";

            return user.FindAll(AppClaimTypes.DeptScope)
                .Select(c => c.Value)
                .Where(v => v.StartsWith(prefix, StringComparison.Ordinal))
                .Select(v => v[prefix.Length..])
                .ToList();
        }
    }
}
