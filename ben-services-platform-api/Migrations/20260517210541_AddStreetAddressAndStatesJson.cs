using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenServicesPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStreetAddressAndStatesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatesJson",
                table: "providers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "providers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StatesJson",
                table: "provider_applications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                table: "provider_applications",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatesJson",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "StatesJson",
                table: "provider_applications");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                table: "provider_applications");
        }
    }
}
