using ITCompliance.API.Data;
using ITCompliance.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITCompliance.API.Controllers
{
    public class AccountController : Controller
    {
        // DEV ONLY - change this value to rotate the dev password.
        // PowerLogin returns 404 outside the Development environment.
        private const string PowerLoginPassword = "Power@2026";

        private readonly AppDbContext _context;
        private readonly ActiveDirectoryService _adService;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            AppDbContext context,
            ActiveDirectoryService adService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _adService = adService;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string EmployeeId,
            string Password)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(EmployeeId) ||
                    string.IsNullOrWhiteSpace(Password))
                {
                    ViewBag.Error =
                        "Email and Password are required.";

                    return View();
                }

                EmployeeId = EmployeeId.Trim();

                var adResult = _adService.Authenticate(
                    EmployeeId,
                    Password);

                if (!adResult.Success)
                {
                    ViewBag.Error = adResult.Status switch
                    {
                        AdAuthStatus.PasswordExpired =>
                            "Your Windows password has expired. " +
                            "Change it on any company PC " +
                            "(Ctrl+Alt+Del > Change a password) " +
                            "and try again.",

                        AdAuthStatus.PasswordMustChange =>
                            "You must change your Windows password " +
                            "before signing in. Change it on any " +
                            "company PC (Ctrl+Alt+Del > Change a " +
                            "password) and try again.",

                        AdAuthStatus.AccountLocked =>
                            "Your account is locked out. Wait a few " +
                            "minutes or contact IT to unlock it.",

                        AdAuthStatus.AccountDisabled =>
                            "Your account is disabled or expired. " +
                            "Contact IT.",

                        AdAuthStatus.UserNotFound =>
                            "No Active Directory account matches " +
                            "this email or username. Check the " +
                            "spelling or contact IT.",

                        AdAuthStatus.ServerUnavailable =>
                            "Cannot reach the Active Directory server. " +
                            "Check the network or contact IT.",

                        AdAuthStatus.UnknownError =>
                            "Login failed. " + adResult.Detail,

                        _ =>
                            "Invalid Active Directory email or password."
                    };

                    return View();
                }

                var loginId = EmployeeId.ToLower();

                var employee = await _context.OESEmployees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.Email != null &&
                        (e.Email.Trim().ToLower() == loginId ||
                         (!loginId.Contains('@') &&
                          e.Email.Trim().ToLower().StartsWith(
                              loginId + "@"))));

                if (employee == null)
                {
                    ViewBag.Error =
                        "Your AD login worked, but your email is not " +
                        "set up in the HR system yet. Ask HR to update " +
                        "your email record, or contact IT.";

                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.Name,
                        employee.Name ?? ""),

                    new Claim(
                        ClaimTypes.NameIdentifier,
                        employee.EmpId ?? ""),

                    new Claim(
                        ClaimTypes.Email,
                        employee.Email ?? ""),

                    // Employee role
                    new Claim(
                        ClaimTypes.Role,
                        "Employee")
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

                return RedirectToAction(
                    "Dashboard",
                    "Employee");
            }
            catch
            {
                ViewBag.Error =
                    "Login failed - the employee database is not " +
                    "reachable right now. Contact IT if it continues.";

                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                "Login",
                "Account");
        }

        [HttpGet("/PowerLogin")]
        public IActionResult PowerLogin()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            return View();
        }

        [HttpGet("/PowerLogin/Employees")]
        public async Task<IActionResult> PowerLoginEmployees()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                var employees = await _context.OESEmployees
                    .AsNoTracking()
                    .Where(e =>
                        e.Email != null &&
                        e.Email != "")
                    .OrderBy(e => e.Name)
                    .ToListAsync();

                return Ok(employees.Select(e => new
                {
                    value = e.Email,
                    text = e.Name + " (" + e.Email + ")"
                }));
            }
            catch
            {
                return StatusCode(503, new
                {
                    message = "Employee database is not reachable."
                });
            }
        }

        [HttpPost("/PowerLogin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PowerLogin(
            string? Email,
            string? Password)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            try
            {

                if (!string.Equals(
                        Password,
                        PowerLoginPassword,
                        StringComparison.Ordinal))
                {
                    ViewBag.Error =
                        "Invalid developer password.";

                    return View();
                }

                var email = (Email ?? "").Trim().ToLower();

                var employee = await _context.OESEmployees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.Email != null &&
                        e.Email.Trim().ToLower() == email);

                if (employee == null)
                {
                    ViewBag.Error =
                        "Selected employee was not found in HR database.";

                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.Name,
                        employee.Name ?? ""),

                    new Claim(
                        ClaimTypes.NameIdentifier,
                        employee.EmpId ?? ""),

                    new Claim(
                        ClaimTypes.Email,
                        employee.Email ?? ""),

                    // Same role as a normal employee login.
                    // Will resolve the real role from the roles
                    // tables once they are implemented.
                    new Claim(
                        ClaimTypes.Role,
                        "Employee")
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        AllowRefresh = true
                    });

                return Redirect("/Employee/Dashboard");
            }
            catch
            {
                ViewBag.Error =
                    "Power login failed. Check the database connection.";

                return View();
            }
        }
    }
}