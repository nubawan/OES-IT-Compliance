using System.Security.Claims;
using ITCompliance.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ITCompliance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ActiveDirectoryService _adService;

        public AuthController(ActiveDirectoryService adService)
        {
            _adService = adService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new
                    {
                        message = "Username and Password are required."
                    });
                }

                // Authenticate against Active Directory
                var adResult = _adService.Authenticate(
                    request.Email.Trim(),
                    request.Password);

                if (!adResult.Success)
                {
                    return adResult.Status switch
                    {
                        AdAuthStatus.ServerUnavailable =>
                            StatusCode(503, new
                            {
                                message =
                                    "Active Directory server is not reachable."
                            }),

                        _ =>
                            Unauthorized(new
                            {
                                message =
                                    "Invalid Active Directory username or password."
                            })
                    };
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, request.Email),
                    new Claim(ClaimTypes.Name, request.Email),
                    new Claim(ClaimTypes.Email, request.Email)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        AllowRefresh = true
                    });

                return Ok(new { message = "Login successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Authentication failed.",
                    error = ex.Message
                });
            }
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}