using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITCompliance.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeEmailToInternetAccessRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeEmail",
                table: "InternetAccessRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_HODdetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeptCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeptName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HODEmpID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HODName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HODEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DirectorEmpId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DirectorName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_HODdetails", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_HODdetails");

            migrationBuilder.DropColumn(
                name: "EmployeeEmail",
                table: "InternetAccessRequests");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Employees");
        }
    }
}
