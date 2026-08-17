using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITCompliance.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndDepartmentScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "InternetAccessRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmpId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByEmpId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_EmployeeId",
                table: "RoleAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_Role_DepartmentCode",
                table: "RoleAssignments",
                columns: new[] { "Role", "DepartmentCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "InternetAccessRequests");
        }
    }
}
