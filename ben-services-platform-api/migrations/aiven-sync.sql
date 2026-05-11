CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

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

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

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

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_provider_applications_City` ON `provider_applications` (`City`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_provider_applications_State` ON `provider_applications` (`State`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_provider_applications_Status` ON `provider_applications` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_provider_applications_SubmittedAt` ON `provider_applications` (`SubmittedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_providers_City` ON `providers` (`City`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_providers_IsActive` ON `providers` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_providers_Region` ON `providers` (`Region`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_providers_State` ON `providers` (`State`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    CREATE INDEX `IX_providers_VerificationStatus` ON `providers` (`VerificationStatus`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260508233646_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260508233646_InitialCreate', '9.0.0');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510040658_AddProviderComplianceDocuments') THEN

    ALTER TABLE `providers` ADD `CoiFilePath` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510040658_AddProviderComplianceDocuments') THEN

    ALTER TABLE `providers` ADD `CoiUploadedAt` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510040658_AddProviderComplianceDocuments') THEN

    ALTER TABLE `providers` ADD `W9FilePath` varchar(500) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510040658_AddProviderComplianceDocuments') THEN

    ALTER TABLE `providers` ADD `W9UploadedAt` datetime(6) NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510040658_AddProviderComplianceDocuments') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260510040658_AddProviderComplianceDocuments', '9.0.0');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    CREATE TABLE `admins` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FullName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Username` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Role` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `MustChangePassword` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        `CreatedByAdminId` int NULL,
        CONSTRAINT `PK_admins` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_admins_admins_CreatedByAdminId` FOREIGN KEY (`CreatedByAdminId`) REFERENCES `admins` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    CREATE INDEX `IX_admins_CreatedByAdminId` ON `admins` (`CreatedByAdminId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    CREATE UNIQUE INDEX `IX_admins_Email` ON `admins` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    CREATE INDEX `IX_admins_IsActive` ON `admins` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    CREATE INDEX `IX_admins_Role` ON `admins` (`Role`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    CREATE UNIQUE INDEX `IX_admins_Username` ON `admins` (`Username`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260510150257_AddAdminAuthentication') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260510150257_AddAdminAuthentication', '9.0.0');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

