START TRANSACTION;

CREATE UNIQUE INDEX "IX_Members_Email" ON "Members" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305162825_EnforceEmailUniqueness', '8.0.11');

COMMIT;

