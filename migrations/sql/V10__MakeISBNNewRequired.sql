START TRANSACTION;


                UPDATE "Books" 
                SET "ISBNNew" = "ISBN" 
                WHERE "ISBNNew" IS NULL;
            

UPDATE "Books" SET "ISBNNew" = '' WHERE "ISBNNew" IS NULL;
ALTER TABLE "Books" ALTER COLUMN "ISBNNew" SET NOT NULL;
ALTER TABLE "Books" ALTER COLUMN "ISBNNew" SET DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305165534_MakeISBNNewRequired', '8.0.11');

COMMIT;

