-- Data Cleanup Script for Duplicate Email Addresses
-- This script must be run BEFORE applying V4__EnforceEmailUniqueness.sql
-- Purpose: Identify and resolve duplicate email addresses in Members table

-- Step 1: Identify duplicate email addresses
-- Run this query to see which emails have duplicates
SELECT 
    "Email",
    COUNT(*) as duplicate_count,
    ARRAY_AGG("Id" ORDER BY "Id") as member_ids
FROM "Members"
GROUP BY "Email"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- Step 2: Review the duplicate members
-- Examine each duplicate to decide which to keep
SELECT 
    m."Id",
    m."FirstName",
    m."LastName",
    m."Email",
    m."PhoneNumber",
    COUNT(l."Id") as loan_count,
    MAX(l."LoanDate") as last_loan_date
FROM "Members" m
LEFT JOIN "Loans" l ON m."Id" = l."MemberId"
WHERE m."Email" IN (
    SELECT "Email"
    FROM "Members"
    GROUP BY "Email"
    HAVING COUNT(*) > 1
)
GROUP BY m."Id", m."FirstName", m."LastName", m."Email", m."PhoneNumber"
ORDER BY m."Email", m."Id";

-- Step 3: Cleanup Strategy - Keep Oldest Member, Reassign Loans
-- This approach keeps the member with the lowest ID (oldest account)
-- and reassigns all loans from duplicate accounts to the kept account

-- IMPORTANT: Review the identified duplicates above before running this!
-- Uncomment and modify as needed:

/*
-- For each duplicate email, this will:
-- 1. Keep the member with the lowest ID
-- 2. Reassign all loans from duplicate accounts to the kept account
-- 3. Delete the duplicate member records

BEGIN;

-- Create a temporary table with the resolution strategy
CREATE TEMP TABLE member_duplicates AS
WITH duplicates AS (
    SELECT 
        "Email",
        ARRAY_AGG("Id" ORDER BY "Id") as member_ids
    FROM "Members"
    GROUP BY "Email"
    HAVING COUNT(*) > 1
),
resolution AS (
    SELECT 
        "Email",
        member_ids[1] as keep_id,
        member_ids[2:] as delete_ids
    FROM duplicates
)
SELECT 
    "Email",
    keep_id,
    UNNEST(delete_ids) as delete_id
FROM resolution;

-- Show what will be changed
SELECT 
    md."Email",
    md.keep_id as "KeepMemberId",
    m_keep."FirstName" || ' ' || m_keep."LastName" as "KeepMemberName",
    md.delete_id as "DeleteMemberId",
    m_del."FirstName" || ' ' || m_del."LastName" as "DeleteMemberName",
    COUNT(l."Id") as "LoansToReassign"
FROM member_duplicates md
JOIN "Members" m_keep ON md.keep_id = m_keep."Id"
JOIN "Members" m_del ON md.delete_id = m_del."Id"
LEFT JOIN "Loans" l ON l."MemberId" = md.delete_id
GROUP BY md."Email", md.keep_id, m_keep."FirstName", m_keep."LastName", 
         md.delete_id, m_del."FirstName", m_del."LastName"
ORDER BY md."Email", md.delete_id;

-- If the above looks correct, uncomment the following to execute:

-- -- Reassign loans from duplicate members to the kept member
-- UPDATE "Loans"
-- SET "MemberId" = md.keep_id
-- FROM member_duplicates md
-- WHERE "Loans"."MemberId" = md.delete_id;

-- -- Delete duplicate member records
-- DELETE FROM "Members"
-- WHERE "Id" IN (SELECT delete_id FROM member_duplicates);

-- -- Verify no more duplicates exist
-- SELECT "Email", COUNT(*) 
-- FROM "Members" 
-- GROUP BY "Email" 
-- HAVING COUNT(*) > 1;

COMMIT;

-- Drop temporary table
DROP TABLE IF EXISTS member_duplicates;
*/

-- Alternative Strategy: Add Suffix to Duplicate Emails
-- This approach keeps all members but makes their emails unique by adding a suffix
-- Uncomment if preferred:

/*
BEGIN;

WITH duplicates AS (
    SELECT 
        "Id",
        "Email",
        ROW_NUMBER() OVER (PARTITION BY "Email" ORDER BY "Id") as rn
    FROM "Members"
),
to_update AS (
    SELECT 
        "Id",
        "Email",
        "Email" || '_duplicate_' || (rn - 1) as new_email
    FROM duplicates
    WHERE rn > 1
)
UPDATE "Members" m
SET "Email" = tu.new_email
FROM to_update tu
WHERE m."Id" = tu."Id";

-- Verify the changes
SELECT "Email", COUNT(*) 
FROM "Members" 
GROUP BY "Email" 
HAVING COUNT(*) > 1;

COMMIT;
*/

-- Step 4: Verification
-- After cleanup, verify there are no duplicates remaining
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN 'SUCCESS: No duplicate emails found'
        ELSE 'ERROR: Duplicates still exist'
    END as status
FROM (
    SELECT "Email"
    FROM "Members"
    GROUP BY "Email"
    HAVING COUNT(*) > 1
) duplicates;
