using System.Security.Claims;
using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Roles")]
        public async Task<IActionResult> Roles()
        {
            var assignments = await _context.RoleAssignments
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Role)
                .ThenBy(r => r.EmployeeId)
                .ToListAsync();

            var employeeIds = assignments
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToList();

            ViewBag.Employees = await _context.OESEmployees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.EmpId))
                .ToDictionaryAsync(e => e.EmpId);

            return View(assignments);
        }

        [HttpGet("Roles/Create")]
        public async Task<IActionResult> CreateRole()
        {
            ViewBag.Roles = RoleNames.AssignableRoles;

            ViewBag.Departments = await _context.OESEmployees
                .AsNoTracking()
                .Where(e => e.DepartmentCode != "")
                .Select(e => new { e.DepartmentCode, e.DepartmentName })
                .Distinct()
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            return View();
        }

        // Grants one role (optionally department-scoped) to every
        // selected employee in a single submit.
        [HttpPost("Roles/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(
            [FromForm] List<string> employeeIds,
            [FromForm] string role,
            [FromForm] string? departmentCode)
        {
            employeeIds = (employeeIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (employeeIds.Count == 0 ||
                !RoleNames.AssignableRoles.Contains(role))
            {
                TempData["ErrorMessage"] =
                    "Select at least one employee and a valid role.";
                return RedirectToAction(nameof(CreateRole));
            }

            departmentCode = string.IsNullOrWhiteSpace(departmentCode)
                ? null
                : departmentCode.Trim();

            var employees = await _context.OESEmployees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.EmpId))
                .ToDictionaryAsync(e => e.EmpId);

            var existing = await _context.RoleAssignments
                .Where(r =>
                    employeeIds.Contains(r.EmployeeId) &&
                    r.Role == role &&
                    r.IsActive &&
                    r.DepartmentCode == departmentCode)
                .Select(r => r.EmployeeId)
                .ToListAsync();

            var currentAdminId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var granted = new List<string>();
            var skipped = new List<string>();

            foreach (var employeeId in employeeIds)
            {
                if (!employees.ContainsKey(employeeId) ||
                    existing.Contains(employeeId))
                {
                    skipped.Add(employeeId);
                    continue;
                }

                _context.RoleAssignments.Add(new RoleAssignment
                {
                    EmployeeId = employeeId,
                    Role = role,
                    DepartmentCode = departmentCode,
                    CreatedByEmpId = currentAdminId
                });

                granted.Add(employees[employeeId].Name);
            }

            await _context.SaveChangesAsync();

            var scopeText = departmentCode != null
                ? $" for department {departmentCode}"
                : "";

            TempData["SuccessMessage"] = granted.Count switch
            {
                0 => "No new role assignments were made - " +
                     "everyone selected already holds that role.",
                _ => $"Granted {role} role{scopeText} to " +
                     $"{string.Join(", ", granted)}. " +
                     "Takes effect next time they log in." +
                     (skipped.Count > 0
                        ? $" ({skipped.Count} skipped - already held or not found.)"
                        : "")
            };

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost("Roles/Revoke/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeRole(int id)
        {
            var assignment = await _context.RoleAssignments
                .FirstOrDefaultAsync(r => r.Id == id);

            if (assignment == null || !assignment.IsActive)
            {
                TempData["ErrorMessage"] = "Role assignment not found.";
                return RedirectToAction(nameof(Roles));
            }

            assignment.IsActive = false;
            assignment.RevokedAt = DateTime.Now;
            assignment.RevokedByEmpId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Role assignment revoked. Takes effect next time " +
                "that person logs in.";
            return RedirectToAction(nameof(Roles));
        }

        // JSON employee picker for the CreateRole combobox. Not a
        // reuse of /PowerLogin/Employees - that one 404s outside
        // Development, and this must work in Production.
        [HttpGet("Roles/Employees")]
        public async Task<IActionResult> Employees()
        {
            var employees = await _context.OESEmployees
                .AsNoTracking()
                .Where(e => e.EmpId != "")
                .OrderBy(e => e.Name)
                .ToListAsync();

            return Ok(employees.Select(e => new
            {
                value = e.EmpId,
                text = e.Name + " (" + e.Email + ")"
            }));
        }
    }
}
