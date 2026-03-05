START TRANSACTION;

ALTER TABLE "Members" ADD "PhoneNumber" character varying(20);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260305162700_AddPhoneNumberToMembers', '8.0.11');

COMMIT;

