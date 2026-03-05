START TRANSACTION;


                UPDATE "Loans" 
                SET "Status" = CASE 
                    WHEN "ReturnDate" IS NULL THEN 0 
                    ELSE 1 
                END 
                WHERE "Status" IS NULL;
            

UPDATE "Loans" SET "Status" = 0 WHERE "Status" IS NULL;
ALTER TABLE "Loans" ALTER COLUMN "Status" SET NOT NULL;
ALTER TABLE "Loans" ALTER COLUMN "Status" SET DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305164131_MakeLoanStatusRequired', '8.0.11');

COMMIT;

