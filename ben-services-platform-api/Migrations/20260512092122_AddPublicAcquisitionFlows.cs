using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenServicesPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicAcquisitionFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "provider_applications",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ConvertedProviderId",
                table: "provider_applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceFileUrl",
                table: "provider_applications",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LicenseFileUrl",
                table: "provider_applications",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "provider_applications",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "provider_applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "W9FileUrl",
                table: "provider_applications",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "client_service_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClientType = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompanyName = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServiceCategory = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Urgency = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZipCode = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(3000)", maxLength: 3000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredDateTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhotoFileUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminNotes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_service_requests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "provider_accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_accounts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_provider_applications_ConvertedProviderId",
                table: "provider_applications",
                column: "ConvertedProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_applications_UserId",
                table: "provider_applications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_client_service_requests_City",
                table: "client_service_requests",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_client_service_requests_ClientType",
                table: "client_service_requests",
                column: "ClientType");

            migrationBuilder.CreateIndex(
                name: "IX_client_service_requests_CreatedAt",
                table: "client_service_requests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_client_service_requests_Email",
                table: "client_service_requests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_client_service_requests_State",
                table: "client_service_requests",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_client_service_requests_Status",
                table: "client_service_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_Email",
                table: "provider_accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_Role",
                table: "provider_accounts",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_Status",
                table: "provider_accounts",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_provider_applications_provider_accounts_UserId",
                table: "provider_applications",
                column: "UserId",
                principalTable: "provider_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_provider_applications_provider_accounts_UserId",
                table: "provider_applications");

            migrationBuilder.DropTable(
                name: "client_service_requests");

            migrationBuilder.DropTable(
                name: "provider_accounts");

            migrationBuilder.DropIndex(
                name: "IX_provider_applications_ConvertedProviderId",
                table: "provider_applications");

            migrationBuilder.DropIndex(
                name: "IX_provider_applications_UserId",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "ConvertedProviderId",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "InsuranceFileUrl",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "LicenseFileUrl",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "W9FileUrl",
                table: "provider_applications");
        }
    }
}
