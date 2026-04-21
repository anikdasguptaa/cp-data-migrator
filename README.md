# CP Data Migrator

A .NET 10 WinForms application that helps Core Practice support staff migrate historical patient and treatment data from legacy dental-practice CSV files into the Core Practice cloud platform.

## Overview

Many dental practices still use legacy desktop software that exports data as CSV files. Migrating this data into Core Practice is complex and error-prone. This tool provides a user-friendly interface for non-technical staff to load, validate, fix, and ingest CSV data safely.

## Architecture

The solution follows a layered architecture with clear separation of concerns:

| Project | Target | Purpose |
|---|---|---|
| **CP.Migrator.UI** | .NET 10 (WinForms, `net10.0-windows`) | User interface - file loading, tabbed data grids, action buttons, ingestion report dialog |
| **CP.Migrator.Business** | .NET 10 | Business logic - parsing, validation, auto-fix, ingestion, export, undo/redo |
| **CP.Migrator.Models** | .NET 10 | Shared models - CSV row types, database entities, result/report types |
| **CP.Migrator.Data** | .NET 10 | Data-access abstractions - repository interfaces, unit of work, connection factory |
| **CP.Migrator.Data.SQLite** | .NET 10 | SQLite implementation - Dapper-based repositories, DbUp schema migrations |
| **CP.Migrator.Test** | .NET 10 | Unit and integration tests (xUnit) |

### Key Dependencies

| Package | Version | Used In |
|---|---|---|
| CsvHelper | 33.0.1 | CP.Migrator.Business |
| Dapper | 2.1.72 | CP.Migrator.Data.SQLite, CP.Migrator.Test |
| Microsoft.Data.Sqlite | 10.0.6 | CP.Migrator.Data.SQLite, CP.Migrator.Test |
| dbup-sqlite | 6.0.4 | CP.Migrator.Data.SQLite |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | CP.Migrator.UI |
| xUnit | 2.9.3 | CP.Migrator.Test |

## Import Pipeline

The tool processes data through a well-defined pipeline:

1. **Parse** - Load `patient.csv` and `treatment.csv` into typed row models (`PatientCsvRow` / `TreatmentCsvRow`) using CsvHelper. Each row is wrapped in a `ParsedRow<T>` that carries validation state through the rest of the pipeline.
2. **Validate** - Run validators against each row (mandatory fields, date/email/phone/postcode formats, valid Australian states and genders, foreign-key integrity, duplicate detection). Errors and warnings are stored per row and displayed in the UI grid with color coding.
3. **Auto-fix** *(user-triggered)* - Automatically correct common issues (trim whitespace, normalise dates to `yyyy-MM-dd`, normalise phone/mobile numbers, standardise gender and paid values). Every fix is recorded in the undo/redo stack.
4. **Re-validate** - Validators re-run automatically after auto-fix so the grid reflects what was resolved.
5. **Ingest** - Persist valid rows into the SQLite database within a single transaction (scoped per ingestion run):
   - `patient.csv` -> `tblPatient` (duplicate detection by composite key: `ClinicId + PatientNo + Firstname + Lastname`)
   - `treatment.csv` -> `tblTreatment` + `tblInvoice` + `tblInvoiceLineItem`
   - Invoices are created by grouping treatments that share the same patient and date. Invoice totals are calculated from related line item fees.
   - A **Clinic ID** (entered by the operator before ingestion) scopes all records for multi-clinic isolation.
6. **Report** - An `IngestionReport` modal dialog shows per-row outcomes (Inserted / Skipped / Duplicate / Failed) and can be exported to CSV.
7. **Post-ingestion cleanup** - Successfully ingested rows (Inserted / Duplicate) are removed from the in-memory grid. Remaining rows (still invalid) stay for further editing.

## Database Schema

The SQLite schema is managed by DbUp via embedded SQL migration scripts. Migration history is tracked automatically in a `SchemaVersions` table. The database file (`CorePractice.db`) is created next to the executable on first run.

| Migration | Description |
|---|---|
| `0001_InitialSetup.sql` | Creates `tblPatient`, `tblInvoice`, `tblTreatment`, `tblInvoiceLineItem` |
| `0002_AddClinicId.sql` | Adds `ClinicId INTEGER` to all four tables for multi-clinic data isolation |

> **SQLite type notes:** `datetime` columns are stored as ISO 8601 `TEXT`; `bit` columns are stored as `INTEGER 0/1`; `decimal(19,4)` columns are stored as `REAL` (IEEE 754 double - exact decimal precision is not guaranteed at the storage layer).

## Key Design Decisions

- **CSV IDs are reference-only.** Original IDs from the CSV are stored as `PatientNo` (patient) and `RawPatientSourceId` (treatment cross-reference) but are never used as database primary keys, because CSV IDs are not guaranteed to be numeric or unique across databases.
- **Validation is configurable.** Rules (valid Australian states, accepted genders, date formats, regex patterns for email/mobile/phone/postcode, max patient age) are driven by `PatientValidationOptions` and `TreatmentValidationOptions`, registered as singletons and overridable without code edits.
- **Two-tier validation severity.** Each `ValidationError` carries either `Error` (blocks ingestion) or `Warning` (informational - row is still ingested) severity. Rows with only warnings are color-coded yellow in the grid.
- **Duplicate detection.** Patients are checked by composite key (`ClinicId + PatientNo + Firstname + Lastname`) before insertion. Duplicate patients are mapped to the existing DB record so their treatments are still ingested correctly.
- **Transactional ingestion.** All inserts within a run happen within a single database transaction - if anything fails mid-run, the entire batch is rolled back.
- **Undo/Redo.** Manual cell edits in the UI grid and auto-fix operations are both tracked by the generic `UndoRedoManager<T>` (stack-based) so operators can step backwards and forwards before committing. History is cleared after ingestion.
- **Invalid row export.** Operators can export invalid rows from the active tab to a CSV file for external correction and re-import.
- **Ingestion report export.** The post-ingestion report dialog can be saved as a CSV file for audit purposes.
- **DI-first, scoped ingestion.** The UI creates a new DI scope per ingestion run (`IServiceProvider.CreateScope()`) so each run gets a fresh `IUnitOfWork` (and therefore a fresh SQLite connection + transaction).
- **`InternalsVisibleTo`** is configured on both `CP.Migrator.Business` and `CP.Migrator.Data.SQLite` so the test project can access internal types without making them public.

## Project Structure

```
cp-data-migrator.slnx
+-- CP.Migrator.UI/
|   +-- Forms/
|   |   +-- MainForm.cs / .Designer.cs            -> Application shell
|   |   +-- IngestionReportForm.cs / .Designer.cs -> Post-ingestion report dialog
|   +-- ViewModels/
|   |   +-- IRowItem.cs
|   |   +-- PatientRowItem.cs
|   |   +-- TreatmentRowItem.cs
|   +-- Helpers/
|   |   +-- CsvRowExtensions.cs                   -> Clone/CopyTo for undo snapshots
|   +-- Program.cs                                -> DI setup + app entry point
+-- CP.Migrator.Business/
|   +-- AutoFix/       -> PatientAutoFix, TreatmentAutoFix, AutoFixBase
|   +-- Config/        -> PatientValidationOptions, TreatmentValidationOptions
|   +-- Export/        -> RowExportService
|   +-- History/       -> UndoRedoManager<T>
|   +-- Ingestion/     -> IngestionService
|   +-- Parser/        -> PatientCsvParser, TreatmentCsvParser
|   +-- Validation/    -> PatientValidator, TreatmentValidator
|   +-- ServiceCollectionExtensions.cs
+-- CP.Migrator.Models/
|   +-- Csv/           -> CsvRow (base), PatientCsvRow, TreatmentCsvRow
|   +-- Entities/      -> Patient, Invoice, Treatment, InvoiceLineItem
|   +-- Results/       -> ParsedRow<T>, ValidationError, IngestionReport, IngestionStatus
+-- CP.Migrator.Data/
|   +-- Repositories/  -> IRepository<T>, IPatientRepository, IInvoiceRepository,
|   |                     ITreatmentRepository, IInvoiceLineItemRepository
|   +-- IUnitOfWork.cs
|   +-- IConnectionFactory.cs
|   +-- IDatabaseInitializer.cs
+-- CP.Migrator.Data.SQLite/
|   +-- Repositories/  -> SQLitePatientRepository, SQLiteInvoiceRepository,
|   |                     SQLiteTreatmentRepository, SQLiteInvoiceLineItemRepository
|   +-- Migrations/
|   |   +-- 0001_InitialSetup.sql
|   |   +-- 0002_AddClinicId.sql
|   +-- SQLiteConnectionFactory.cs
|   +-- SQLiteUnitOfWork.cs
|   +-- SQLiteDatabaseInitializer.cs
|   +-- ServiceCollectionExtensions.cs
+-- CP.Migrator.Test/
   +-- Business/
   |   +-- AutoFix/      -> PatientAutoFixTests, TreatmentAutoFixTests
   |   +-- Export/       -> RowExportServiceTests
   |   +-- History/      -> UndoRedoManagerTests
   |   +-- Ingestion/    -> IngestionServiceTests
   |   +-- Parser/       -> PatientCsvParserTests, TreatmentCsvParserTests
   |   +-- Validation/   -> PatientValidatorTests, TreatmentValidatorTests
   +-- Integration/
   |   +-- SQLite/
   |       +-- DatabaseInitializerTests.cs
   |       +-- Repositories/  -> SQLitePatientRepositoryTests, SQLiteInvoiceRepositoryTests,
   |                             SQLiteInvoiceLineItemRepositoryTests, SQLiteTreatmentRepositoryTests
   +-- Shared/
      +-- FixedConnectionFactory.cs  -> In-memory SQLite helper for integration tests
```

## Getting Started

### Prerequisites

- Visual Studio 2022+ with the **.NET 10 SDK** installed
- No external database required - for simplicity, the app creates a local SQLite file (`CorePractice.db`) next to the executable on first run; this can be easily extended in the future to support a custom folder location as well

### Running the Application

1. Open `cp-data-migrator.slnx` in Visual Studio.
2. Set **CP.Migrator.UI** as the startup project.
3. Press **F5** to build and run.
4. Enter a **Clinic ID** (numeric, >= 1) in the top bar.
5. Load `patient.csv` and/or `treatment.csv` using the **Load** buttons.
6. Click **Validate** to check all rows. Rows are color-coded:
   - Red - has errors (will be skipped on ingestion)
   - Yellow - has warnings only (will still be ingested)
   - Blue - auto-fixed
   - White - fully valid
7. Optionally click **Auto-Fix** to automatically correct common issues.
8. Edit individual cells directly in the grid. Use **Undo / Redo** to step through changes.
9. Use **Export Invalid** to save rows that still have errors to a CSV for external correction.
10. Click **Ingest** to persist all valid rows. A report dialog will appear with per-row outcomes.

### Running Tests

Tests use xUnit. Run from **Test Explorer** in Visual Studio or via:

```
dotnet test
```

Integration tests use an in-memory SQLite database (`FixedConnectionFactory`) - no setup required.

## CSV File Format

### patient.csv

| Column | Description |
|---|---|
| Id | Legacy patient ID (stored as `PatientNo`; reference only - not used as DB PK) |
| FirstName | Patient first name (required) |
| LastName | Patient last name (required) |
| DOB | Date of birth (`yyyy-MM-dd` after auto-fix) |
| Gender | `M` / `F` / `O` (auto-fixed to uppercase) |
| Email | Email address (validated by regex) |
| MobileNumber | Australian mobile - 10 digits starting with `04` (auto-normalised) |
| PhoneNumber | Australian landline - 10 digits (auto-normalised) |
| Street | Street address |
| Suburb | Suburb |
| State | Australian state/territory code: `NSW`, `VIC`, `QLD`, `SA`, `WA`, `TAS`, `NT`, `ACT` |
| Postcode | 4-digit Australian postcode |

### treatment.csv

| Column | Description |
|---|---|
| Id | Legacy treatment ID (reference only) |
| PatientID | Must match a patient `Id` from `patient.csv` (stored as `RawPatientSourceId`) |
| DentistID | Dentist identifier |
| TreatmentItem | Item code (required) |
| Description | Treatment description (required) |
| Price | Price amount (optional) |
| Fee | Fee amount (required, must be a positive decimal) |
| Date | Treatment date (`yyyy-MM-dd` after auto-fix) |
| Paid | `Yes` / `No` (auto-fixed to title case) |
| ToothNumber | Tooth number (optional) |
| Surface | Tooth surface (optional) |
