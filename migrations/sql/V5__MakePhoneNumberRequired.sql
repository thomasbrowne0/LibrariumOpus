START TRANSACTION;


                UPDATE "Members"
                SET "PhoneNumber" = '000-000-0000'
                WHERE "PhoneNumber" IS NULL;
            

UPDATE "Members" SET "PhoneNumber" = '' WHERE "PhoneNumber" IS NULL;
ALTER TABLE "Members" ALTER COLUMN "PhoneNumber" SET NOT NULL;
ALTER TABLE "Members" ALTER COLUMN "PhoneNumber" SET DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305162919_MakePhoneNumberRequired', '8.0.11');

COMMIT;

