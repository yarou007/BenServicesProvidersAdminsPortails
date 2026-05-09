CREATE DATABASE IF NOT EXISTS `benservices` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `benservices`;

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `provider_applications` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FullName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessName` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `ServiceType` varchar(24) CHARACTER SET utf8mb4 NOT NULL,
    `ServicesOfferedJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CitiesCoveredJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `City` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `State` varchar(16) CHARACTER SET utf8mb4 NOT NULL,
    `ZipCodesJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `YearsOfExperience` int NOT NULL,
    `EmergencyService` tinyint(1) NOT NULL,
    `WorkingHours` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `Message` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `Source` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `SubmittedAt` datetime(6) NOT NULL,
    `LicenseFileName` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_provider_applications` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `providers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FullName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `BusinessName` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `ServiceType` varchar(24) CHARACTER SET utf8mb4 NOT NULL,
    `ServicesOfferedJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `City` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `State` varchar(16) CHARACTER SET utf8mb4 NOT NULL,
    `ZipCodesJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Region` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `EmergencyService` tinyint(1) NOT NULL,
    `Availability` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `WorkingHours` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `VerificationStatus` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `Source` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `YearsOfExperience` int NOT NULL,
    `Notes` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `AdminComments` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `VerifiedAt` datetime(6) NULL,
    CONSTRAINT `PK_providers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_provider_applications_City` ON `provider_applications` (`City`);
CREATE INDEX `IX_provider_applications_State` ON `provider_applications` (`State`);
CREATE INDEX `IX_provider_applications_Status` ON `provider_applications` (`Status`);
CREATE INDEX `IX_provider_applications_SubmittedAt` ON `provider_applications` (`SubmittedAt`);

CREATE INDEX `IX_providers_City` ON `providers` (`City`);
CREATE INDEX `IX_providers_IsActive` ON `providers` (`IsActive`);
CREATE INDEX `IX_providers_Region` ON `providers` (`Region`);
CREATE INDEX `IX_providers_State` ON `providers` (`State`);
CREATE INDEX `IX_providers_VerificationStatus` ON `providers` (`VerificationStatus`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260508233646_InitialCreate', '9.0.0');

COMMIT;
