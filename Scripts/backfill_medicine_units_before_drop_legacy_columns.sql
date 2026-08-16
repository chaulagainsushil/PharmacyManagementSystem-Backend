-- =============================================================================
-- One-time data migration: backfill MedicineUnit rows from legacy columns
-- =============================================================================
-- PURPOSE
--   The legacy Medicines table carries four columns that are being removed:
--     • TabletPrice       – per-tablet selling price
--     • StripPrice        – per-strip selling price
--     • TabletsPerStrip   – tablets per strip (conversion factor)
--     • StrapsPerBox      – strips per box   (NOT yet used in MedicineUnits)
--
--   The AddUomAndMedicineUnits migration (20260805053919) already ran a partial
--   backfill for medicines that existed AT THAT TIME.  This script is the
--   safety net for any medicines created after that migration but before this
--   schema change is applied — i.e. rows whose MedicineId is not yet represented
--   in MedicineUnits.
--
-- SAFETY
--   All INSERT statements use ON CONFLICT DO NOTHING so re-running this script
--   is idempotent.  The unique index IX_MedicineUnits_MedicineId_UnitOfMeasureId
--   prevents duplicate rows.
--
-- PREREQUISITE
--   Run this script BEFORE executing the EF Core migration that drops the four
--   legacy columns.  The EF migration is:
--       RemoveLegacyMedicinePricingColumns (timestamp: 20260811...)
--
-- HOW TO RUN (from psql or any SQL client connected to the target database):
--   \i backfill_medicine_units_before_drop_legacy_columns.sql
--
-- =============================================================================

BEGIN;

-- ── 1. Tablet unit (base + default) for medicines that have no MedicineUnit yet ──
--
-- Every medicine must have exactly one IsBaseUnit=true row.  We only insert
-- where none exists yet (the ON CONFLICT clause handles idempotency).
--
INSERT INTO "MedicineUnits"
    ("MedicineId", "UnitOfMeasureId", "ConversionFactorToBase",
     "IsBaseUnit", "IsDefault", "IsActive", "UnitPrice")
SELECT
    m."MedicineId",
    1,                  -- UoM = Tablet  (seeded in AddUomAndMedicineUnits migration)
    1.0,                -- 1 tablet = 1 base unit
    true,               -- IsBaseUnit
    true,               -- IsDefault
    true,               -- IsActive
    GREATEST(m."TabletPrice", 0)   -- guard against NULL / negative legacy data
FROM "Medicines" m
WHERE NOT EXISTS (
    SELECT 1 FROM "MedicineUnits" mu
    WHERE mu."MedicineId" = m."MedicineId"
)
ON CONFLICT ("MedicineId", "UnitOfMeasureId") DO NOTHING;


-- ── 2. Strip unit for medicines that have more than 1 tablet per strip ──
--
-- Only insert where TabletsPerStrip > 1 and no Strip unit exists yet.
--
INSERT INTO "MedicineUnits"
    ("MedicineId", "UnitOfMeasureId", "ConversionFactorToBase",
     "IsBaseUnit", "IsDefault", "IsActive", "UnitPrice")
SELECT
    m."MedicineId",
    2,                              -- UoM = Strip  (seeded in AddUomAndMedicineUnits migration)
    m."TabletsPerStrip"::numeric,   -- 1 strip = N tablets
    false,                          -- IsBaseUnit
    false,                          -- IsDefault
    true,                           -- IsActive
    GREATEST(m."StripPrice", 0)     -- guard against NULL / negative legacy data
FROM "Medicines" m
WHERE m."TabletsPerStrip" > 1
  AND NOT EXISTS (
      SELECT 1 FROM "MedicineUnits" mu
      WHERE mu."MedicineId"      = m."MedicineId"
        AND mu."UnitOfMeasureId" = 2
  )
ON CONFLICT ("MedicineId", "UnitOfMeasureId") DO NOTHING;


-- ── 3. Verification queries (review before committing) ────────────────────────

-- How many medicines still have no base unit after backfill?
-- Expected result: 0 rows
SELECT m."MedicineId", m."Name"
FROM   "Medicines" m
WHERE  NOT EXISTS (
    SELECT 1 FROM "MedicineUnits" mu
    WHERE  mu."MedicineId" = m."MedicineId"
      AND  mu."IsBaseUnit" = true
);

-- How many medicines have more than one base unit? (invariant violation)
-- Expected result: 0 rows
SELECT "MedicineId", COUNT(*) AS base_count
FROM   "MedicineUnits"
WHERE  "IsBaseUnit" = true
GROUP  BY "MedicineId"
HAVING COUNT(*) > 1;

-- Summary counts
SELECT
    (SELECT COUNT(*) FROM "Medicines")     AS total_medicines,
    (SELECT COUNT(DISTINCT "MedicineId")
     FROM   "MedicineUnits")               AS medicines_with_units,
    (SELECT COUNT(*) FROM "MedicineUnits") AS total_unit_rows;

COMMIT;
