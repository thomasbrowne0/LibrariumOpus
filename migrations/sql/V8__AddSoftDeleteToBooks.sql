START TRANSACTION;

ALTER TABLE "Books" ADD "RetiredAt" timestamp with time zone;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305164833_AddSoftDeleteToBooks', '8.0.11');

COMMIT;

