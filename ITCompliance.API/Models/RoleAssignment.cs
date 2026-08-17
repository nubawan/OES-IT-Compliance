using System.ComponentModel.DataAnnotations;

namespace ITCompliance.API.Models
{
    // App-owned role grants. EmployeeId references OESEmployees.EmpId
    // (the HR view) at the application level only - that table is a
    // read-only view, so there is no DB foreign key.
    public class RoleAssignment
    {
        [Key]
        public int Id { get; set; }

        public string EmployeeId { get; set; } = string.Empty;

        // One of RoleNames.AssignableRoles.
        public string Role { get; set; } = string.Empty;

        // Null = global/unscoped for this role. A value scopes the
        // grant to that department only. Multiple departments for
        // the same person/role are multiple rows.
        public string? DepartmentCode { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CreatedByEmpId { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? RevokedByEmpId { get; set; }
    }
}
