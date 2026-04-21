-- =======================================================================
-- Migration: Add ClinicId to all tables for multi-clinic data isolation.
--
-- In a real-world application, each dental practice (clinic) that exports
-- CSV data must be tracked separately so that duplicate detection, invoice
-- numbering, and patient matching are scoped to the originating clinic.
-- The provided requirements and schema do not include clinic metadata, so
-- we add a simple non-nullable ClinicId (INTEGER) column as a lightweight
-- tenant discriminator without introducing a full clinic table.
-- =======================================================================

ALTER TABLE tblPatient ADD COLUMN ClinicId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tblInvoice ADD COLUMN ClinicId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tblTreatment ADD COLUMN ClinicId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tblInvoiceLineItem ADD COLUMN ClinicId INTEGER NOT NULL DEFAULT 0;
