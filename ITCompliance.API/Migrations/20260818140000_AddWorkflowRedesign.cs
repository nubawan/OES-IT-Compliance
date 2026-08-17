using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITCompliance.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingDepartmentCode",
                table: "InternetAccessRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    StageStatus = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ActorEmpId = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestTransactions_InternetAccessRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "InternetAccessRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogs_InternetAccessRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "InternetAccessRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTransactions_RequestId",
                table: "RequestTransactions",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_RequestId",
                table: "EmailLogs",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestTransactions");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "PendingDepartmentCode",
                table: "InternetAccessRequests");
        }
    }
}
