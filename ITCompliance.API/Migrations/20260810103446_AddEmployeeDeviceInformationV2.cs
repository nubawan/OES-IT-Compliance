using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITCompliance.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDeviceInformationV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CellularId",
                table: "InternetAccessRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "InternetAccessRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "InternetAccessRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanLaptopId",
                table: "InternetAccessRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanMacId",
                table: "InternetAccessRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellularId",
                table: "InternetAccessRequests");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "InternetAccessRequests");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "InternetAccessRequests");

            migrationBuilder.DropColumn(
                name: "LanLaptopId",
                table: "InternetAccessRequests");

            migrationBuilder.DropColumn(
                name: "LanMacId",
                table: "InternetAccessRequests");
        }
    }
}
