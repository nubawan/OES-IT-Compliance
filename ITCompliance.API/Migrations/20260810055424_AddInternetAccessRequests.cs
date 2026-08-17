using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITCompliance.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInternetAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternetAccessRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ITOfficerRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HODRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BossRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityHeadRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternetAccessRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternetAccessRequests");
        }
    }
}
