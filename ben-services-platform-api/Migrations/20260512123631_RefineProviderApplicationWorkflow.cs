using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenServicesPlatform.Api.Migrations
{
    /// <inheritdoc />
public partial class RefineProviderApplicationWorkflow : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        EnsureNullableLongTextColumn(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "AdminNotes",
            procedureName: "sp_ensure_pa_admin_notes_longtext");

        EnsureNullableLongTextColumn(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "MissingInfoReason",
            procedureName: "sp_addcol_pa_missing_info_reason");

        AddColumnIfMissing(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "RejectedAt",
            columnDefinitionSql: "datetime(6) NULL",
            procedureName: "sp_addcol_pa_rejected_at");

        EnsureNullableLongTextColumn(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "RejectionReason",
            procedureName: "sp_addcol_pa_rejection_reason");

        AddColumnIfMissing(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "ReviewedAt",
            columnDefinitionSql: "datetime(6) NULL",
            procedureName: "sp_addcol_pa_reviewed_at");

        EnsureNullableLongTextColumn(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "VerificationNotes",
            procedureName: "sp_addcol_pa_verification_notes");

        AddColumnIfMissing(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "VerifiedAt",
            columnDefinitionSql: "datetime(6) NULL",
            procedureName: "sp_addcol_pa_verified_at");

        AddColumnIfMissing(
            migrationBuilder,
            tableName: "provider_accounts",
            columnName: "MustChangePassword",
            columnDefinitionSql: "tinyint(1) NOT NULL DEFAULT 0",
            procedureName: "sp_addcol_pacc_must_change_pwd");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "MissingInfoReason",
            procedureName: "sp_dropcol_pa_missing_info_reason");

        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "RejectedAt",
            procedureName: "sp_dropcol_pa_rejected_at");

        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "RejectionReason",
            procedureName: "sp_dropcol_pa_rejection_reason");

        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "ReviewedAt",
            procedureName: "sp_dropcol_pa_reviewed_at");

        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "VerificationNotes",
            procedureName: "sp_dropcol_pa_verification_notes");

        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_applications",
            columnName: "VerifiedAt",
            procedureName: "sp_dropcol_pa_verified_at");

        DropColumnIfExists(
            migrationBuilder,
            tableName: "provider_accounts",
            columnName: "MustChangePassword",
            procedureName: "sp_dropcol_pacc_must_change_pwd");
    }

    private static void AddColumnIfMissing(
        MigrationBuilder migrationBuilder,
        string tableName,
        string columnName,
        string columnDefinitionSql,
        string procedureName)
    {
        migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS `{procedureName}`;");
        migrationBuilder.Sql($$"""
            CREATE PROCEDURE `{{procedureName}}`()
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = '{{tableName}}'
                      AND COLUMN_NAME = '{{columnName}}'
                ) THEN
                    ALTER TABLE `{{tableName}}` ADD COLUMN `{{columnName}}` {{columnDefinitionSql}};
                END IF;
            END
            """);
        migrationBuilder.Sql($"CALL `{procedureName}`();");
        migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS `{procedureName}`;");
    }

    private static void EnsureNullableLongTextColumn(
        MigrationBuilder migrationBuilder,
        string tableName,
        string columnName,
        string procedureName)
    {
        migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS `{procedureName}`;");
        migrationBuilder.Sql($$"""
            CREATE PROCEDURE `{{procedureName}}`()
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = '{{tableName}}'
                      AND COLUMN_NAME = '{{columnName}}'
                ) THEN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = '{{tableName}}'
                          AND COLUMN_NAME = '{{columnName}}'
                          AND DATA_TYPE = 'longtext'
                    ) THEN
                        ALTER TABLE `{{tableName}}`
                            MODIFY COLUMN `{{columnName}}` longtext CHARACTER SET utf8mb4 NULL;
                    END IF;
                ELSE
                    ALTER TABLE `{{tableName}}`
                        ADD COLUMN `{{columnName}}` longtext CHARACTER SET utf8mb4 NULL;
                END IF;
            END
            """);
        migrationBuilder.Sql($"CALL `{procedureName}`();");
        migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS `{procedureName}`;");
    }

    private static void DropColumnIfExists(
        MigrationBuilder migrationBuilder,
        string tableName,
        string columnName,
        string procedureName)
    {
        migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS `{procedureName}`;");
        migrationBuilder.Sql($$"""
            CREATE PROCEDURE `{{procedureName}}`()
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = '{{tableName}}'
                      AND COLUMN_NAME = '{{columnName}}'
                ) THEN
                    ALTER TABLE `{{tableName}}` DROP COLUMN `{{columnName}}`;
                END IF;
            END
            """);
        migrationBuilder.Sql($"CALL `{procedureName}`();");
        migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS `{procedureName}`;");
    }
}
}
