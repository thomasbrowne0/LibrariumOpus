START TRANSACTION;

ALTER TABLE "Loans" ADD "Status" integer;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305164032_AddStatusToLoans', '8.0.11');

COMMIT;

