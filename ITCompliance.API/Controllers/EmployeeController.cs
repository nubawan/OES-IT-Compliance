using System.Security.Claims;
using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    [Authorize(Roles = RoleNames.Employee)]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                string? employeeId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.OESEmployees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpId == employeeId);

                if (employee == null)
                {
                    await HttpContext.SignOutAsync();

                    return RedirectToAction("Login", "Account");
                }

                var requests = await _context.InternetAccessRequests
                    .AsNoTracking()
                    .Where(r => r.EmployeeId == employee.EmpId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.Employee = employee;

                return View(requests);
            }
            catch (Exception ex)
            {
                return Content(
                    "DASHBOARD ERROR\n\n" +
                    ex.Message +
                    "\n\nINNER ERROR\n" +
                    (ex.InnerException?.Message ?? "No inner error")
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> SubmitRequest()
        {
            try
            {
                string? employeeId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.OESEmployees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpId == employeeId);

                if (employee == null)
                {
                    await HttpContext.SignOutAsync();

                    return RedirectToAction("Login", "Account");
                }

                ViewBag.Employee = employee;

                return View("Request");
            }
            catch (Exception ex)
            {
                return Content(
                    "REQUEST PAGE ERROR\n\n" +
                    ex.Message +
                    "\n\nINNER ERROR\n" +
                    (ex.InnerException?.Message ?? "No inner error")
                );
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(
            [FromForm] string? website,
            [FromForm] string? reason,
            [FromForm] string? duration,
            [FromForm] string? deviceName,
            [FromForm] string? lanMacId,
            [FromForm] string? cellularId,
            [FromForm] string? lanLaptopId,
            [FromForm] string? ipAddress)
        {
            var employeeId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var fields = new[]
            {
                website, reason, duration, deviceName,
                lanMacId, cellularId, lanLaptopId, ipAddress
            };

            if (fields.Any(string.IsNullOrWhiteSpace))
            {
                TempData["ErrorMessage"] =
                    "Please fill in all employee device " +
                    "information and request fields.";

                return RedirectToAction(nameof(SubmitRequest));
            }

            var employee = await _context.OESEmployees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpId == employeeId);

            if (employee == null)
            {
                await HttpContext.SignOutAsync();

                return RedirectToAction("Login", "Account");
            }

            _context.InternetAccessRequests.Add(
                new InternetAccessRequest
                {
                    EmployeeId = employeeId,
                    EmployeeEmail =
                        User.FindFirstValue(ClaimTypes.Email) ?? "",
                    DepartmentCode = employee.DepartmentCode ?? "",

                    DeviceName = deviceName!.Trim(),
                    LanMacId = lanMacId!.Trim(),
                    CellularId = cellularId!.Trim(),
                    LanLaptopId = lanLaptopId!.Trim(),
                    IpAddress = ipAddress!.Trim(),

                    Website = website!.Trim(),
                    Reason = reason!.Trim(),
                    Duration = duration!.Trim(),

                    Status = "Pending",
                    CreatedAt = DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your internet access request has been " +
                "submitted successfully.";

            return RedirectToAction(nameof(Dashboard));
        }
    }
}