namespace ITCompliance.API.Models
{
    public static class AppClaimTypes
    {
        // Value shape: "{Role}|{DepartmentCode}". One claim per
        // department-scoped RoleAssignment row. A role with no
        // claim of this type is global/unscoped for that role.
        public const string DeptScope = "itc_dept_scope";
    }
}
