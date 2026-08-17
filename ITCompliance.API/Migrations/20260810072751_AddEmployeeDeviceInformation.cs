using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITCompliance.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDeviceInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CellularId",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanLaptopId",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanMacId",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellularId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LanLaptopId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LanMacId",
                table: "Employees");
        }
    }
}
