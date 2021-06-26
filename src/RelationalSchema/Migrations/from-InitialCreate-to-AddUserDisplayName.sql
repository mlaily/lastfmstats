BEGIN TRANSACTION;

ALTER TABLE "Users" ADD "DisplayName" TEXT NOT NULL DEFAULT 'Empty user name';

UPDATE Users
SET DisplayName = Name;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210626175418_AddUserDisplayName', '5.0.4');

COMMIT;