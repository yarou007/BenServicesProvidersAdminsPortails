using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenServicesPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderComplianceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoiFilePath",
                table: "providers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CoiUploadedAt",
                table: "providers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "W9FilePath",
                table: "providers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "W9UploadedAt",
                table: "providers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoiFilePath",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "CoiUploadedAt",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "W9FilePath",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "W9UploadedAt",
                table: "providers");
        }
    }
}
