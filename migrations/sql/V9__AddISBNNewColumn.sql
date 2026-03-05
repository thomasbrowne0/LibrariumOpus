START TRANSACTION;

ALTER TABLE "Books" ADD "ISBNNew" character varying(50);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305165439_AddISBNNewColumn', '8.0.11');

COMMIT;

