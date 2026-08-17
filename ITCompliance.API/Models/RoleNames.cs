namespace ITCompliance.API.Models
{
    // Canonical role names. Every logged-in user always holds
    // Employee; the rest are granted via RoleAssignment rows
    // (see Controllers/AdminController.cs).
    public static class RoleNames
    {
        public const string Employee = "Employee";
        public const string ITOfficer = "ITOfficer";
        public const string HOD = "HOD";
        public const string SecurityHead = "SecurityHead";
        public const string Boss = "Boss";
        public const string Admin = "Admin";

        public static readonly string[] AssignableRoles =
        {
            ITOfficer, HOD, SecurityHead, Boss, Admin
        };
    }
}
