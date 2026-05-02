# CP Data Migrator

A .NET 10 WinForms application that helps Core Practice support staff migrate historical patient and treatment data from legacy dental-practice CSV files into the Core Practice cloud platform.

## Overview

Many dental practices still use legacy desktop software that exports data as CSV files. Migrating this data into Core Practice is complex and error-prone. This tool provides a user-friendly interface for non-technical staff to load, validate, fix, and ingest CSV data safely.

## Architecture

The solution has 7 projects with clear layer boundaries:

| Project | Target | Purpose |
|---|---|---|
| **CP.Migrator.UI** | .NET 10 (`net10.0-windows`) | WinForms UI only. Depends on Application facade interfaces and models. |
| **CP.Migrator.Application** | .NET 10 | Application orchestration layer. Exposes `IMigratorPipelineService` and `IAppStartup` to the UI; `IIngestionService` is an internal coordination seam used by the pipeline. |
| **CP.Migrator.Business** | .NET 10 | Parsing, validation, auto-fix, export, undo/redo services. |
| **CP.Migrator.Data** | .NET 10 | Data abstractions and data entities (`IUnitOfWork`, repositories, DB entities). |
| **CP.Migrator.Data.SQLite** | .NET 10 | SQLite implementation (Dapper repositories, DbUp migrations, unit of work). |
| **CP.Migrator.Models** | .NET 10 | Shared CSV and result models (`CsvRow`, `ParsedRow<T>`, `ValidationError`, `IngestionReport`). |
| **CP.Migrator.Test** | .NET 10 | Unit and integration tests (xUnit) across business/application/data layers. |

### Project References

- `CP.Migrator.UI` -> `CP.Migrator.Application`, `CP.Migrator.Data.SQLite`, `CP.Migrator.Models`
- `CP.Migrator.Application` -> `CP.Migrator.Business`, `CP.Migrator.Data`, `CP.Migrator.Models`
- `CP.Migrator.Business` -> `CP.Migrator.Models`
- `CP.Migrator.Data.SQLite` -> `CP.Migrator.Data`
- `CP.Migrator.Test` -> `CP.Migrator.Application`, `CP.Migrator.Business`, `CP.Migrator.Data`, `CP.Migrator.Data.SQLite`

### Key Dependencies

| Package | Version | Used In |
|---|---|---|
| CsvHelper | 33.0.1 | CP.Migrator.Business |
| Dapper | 2.1.72 | CP.Migrator.Data.SQLite, CP.Migrator.Test |
| Microsoft.Data.Sqlite | 10.0.6 | CP.Migrator.Data.SQLite, CP.Migrator.Test |
| dbup-sqlite | 6.0.4 | CP.Migrator.Data.SQLite |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | CP.Migrator.UI |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0-preview.5.25277.114 | CP.Migrator.Application, CP.Migrator.Business, CP.Migrator.Data.SQLite |
| xunit | 2.9.3 | CP.Migrator.Test |

## Import Pipeline

1. **Load + Parse** - Load `patient.csv` and/or `treatment.csv`. Rows are parsed into `ParsedRow<T>` using CsvHelper-based parser services.
2. **Initial Validate (automatic)** - Validation runs immediately after file load so users see errors right away.
3. **Validate All (manual, optional)** - User can re-run full validation with the toolbar button after additional edits/deletes.
4. **Auto-Fix All (user-triggered)** - Applies automatic corrections (trim, date normalization, phone/mobile normalization, gender/paid normalization), then re-validates.
5. **Ingest** - Valid rows are ingested into SQLite in one transaction:
   - `patient.csv` -> `tblPatient`
   - `treatment.csv` -> grouped into invoices (one per patient + date), then inserted into `tblInvoice`, `tblTreatment`, and `tblInvoiceLineItem` in that order
   - Patient duplicates are detected by composite key: `ClinicId + PatientNo + Firstname + Lastname`
6. **Report** - An `IngestionReport` dialog shows row outcomes: `Inserted`, `Skipped`, `Duplicate`, `Failed`, with CSV export support.
7. **Post-ingestion cleanup** - Inserted/duplicate patient rows are removed; valid treatment rows are removed; remaining rows stay for further correction.

## Database Schema

Schema is managed by DbUp via embedded SQL scripts. Migration history is tracked in `SchemaVersions`. The database file (`CorePractice.db`) is created next to the executable on first run.

| Migration | Description |
|---|---|
| `0001_InitialSetup.sql` | Creates `tblPatient`, `tblInvoice`, `tblTreatment`, `tblInvoiceLineItem` |
| `0002_AddClinicId.sql` | Adds `ClinicId INTEGER NOT NULL` to all four tables |

> **SQLite type notes:** `datetime` -> ISO 8601 `TEXT`; `bit` -> `INTEGER` (0/1); `decimal(19,4)` -> `REAL` (IEEE 754 double, no exact decimal guarantee at the storage layer).

## Key Design Decisions

- **Application facade in front of business/data services**: UI works through `IMigratorPipelineService` and `IAppStartup`, not directly against business/data internals.
- **CSV IDs are reference-only**: `Id` values are stored as reference fields (`PatientNo`, `RawPatientSourceId`) and never used as DB primary keys.
- **Data entities moved to data layer**: persistence entities (`Patient`, `Invoice`, `Treatment`, `InvoiceLineItem`) are in `CP.Migrator.Data.Entities`, while `CP.Migrator.Models` now contains CSV/result models only.
- **Two-tier validation severity**: `ValidationError` supports `Error` (blocking) and `Warning` (non-blocking).
- **Transactional ingestion**: all inserts for a run share one unit of work + transaction; failure rolls back the batch.
- **Duplicate-aware patient mapping**: duplicate patient rows are mapped to existing DB patient IDs so treatment ingestion can continue.
- **Scoped undo/redo history**: manual edits and auto-fixes are tracked per session with `UndoRedoManager<T>` and cleared after ingestion.
- **Active-tab invalid export**: invalid rows can be exported from the selected tab.
- **Startup abstraction**: DB initialization is hidden behind `IAppStartup` so UI startup stays decoupled from data infrastructure.
- **Internals visible to tests**: `CP.Migrator.Business`, `CP.Migrator.Data.SQLite`, and `CP.Migrator.Application` expose internals to `CP.Migrator.Test`.

## Project Structure

```
cp-data-migrator.slnx
|-- CP.Migrator.UI/
|   |-- Forms/
|   |   |-- MainForm.cs / MainForm.Designer.cs
|   |   |-- IngestionReportForm.cs / IngestionReportForm.Designer.cs
|   |-- ViewModels/
|   |   |-- IRowItem.cs
|   |   |-- PatientRowItem.cs
|   |   |-- TreatmentRowItem.cs
|   |-- Program.cs
|
|-- CP.Migrator.Application/
|   |-- Ingestion/
|   |   |-- IIngestionService.cs
|   |   |-- IngestionService.cs
|   |-- Pipeline/
|   |   |-- IMigratorPipelineService.cs
|   |   |-- MigratorPipelineService.cs
|   |-- Startup/
|   |   |-- IAppStartup.cs
|   |   |-- AppStartup.cs
|   |-- ServiceCollectionExtensions.cs
|
|-- CP.Migrator.Business/
|   |-- AutoFix/
|   |-- Config/
|   |-- Export/
|   |-- History/
|   |-- Parser/
|   |-- Validation/
|   |-- ServiceCollectionExtensions.cs
|
|-- CP.Migrator.Data/
|   |-- Entities/
|   |   |-- Patient.cs
|   |   |-- Invoice.cs
|   |   |-- Treatment.cs
|   |   |-- InvoiceLineItem.cs
|   |-- Repositories/
|   |   |-- IRepository.cs
|   |   |-- IPatientRepository.cs
|   |   |-- IInvoiceRepository.cs
|   |   |-- ITreatmentRepository.cs
|   |   |-- IInvoiceLineItemRepository.cs
|   |-- IConnectionFactory.cs
|   |-- IDatabaseInitializer.cs
|   |-- IUnitOfWork.cs
|
|-- CP.Migrator.Data.SQLite/
|   |-- Repositories/
|   |   |-- SQLitePatientRepository.cs
|   |   |-- SQLiteInvoiceRepository.cs
|   |   |-- SQLiteTreatmentRepository.cs
|   |   |-- SQLiteInvoiceLineItemRepository.cs
|   |-- Migrations/
|   |   |-- 0001_InitialSetup.sql
|   |   |-- 0002_AddClinicId.sql
|   |-- SQLiteConnectionFactory.cs
|   |-- SQLiteUnitOfWork.cs
|   |-- SQLiteDatabaseInitializer.cs
|   |-- ServiceCollectionExtensions.cs
|
|-- CP.Migrator.Models/
|   |-- Csv/
|   |   |-- CsvRow.cs
|   |   |-- PatientCsvRow.cs
|   |   |-- TreatmentCsvRow.cs
|   |-- Extensions/
|   |   |-- CsvRowExtensions.cs
|   |-- Results/
|       |-- ParsedRow.cs
|       |-- ValidationError.cs
|       |-- IngestionReport.cs
|
`-- CP.Migrator.Test/
    |-- Business/
    |   |-- AutoFix/
    |   |-- Export/
    |   |-- History/
    |   |-- Ingestion/
    |   |-- Parser/
    |   `-- Validation/
    |-- Integration/SQLite/
    |   |-- DatabaseInitializerTests.cs
    |   `-- Repositories/
    `-- Shared/
        `-- FixedConnectionFactory.cs
```

## Getting Started

### Prerequisites

- Visual Studio 2022+ with .NET 10 SDK
- No external DB setup; `CorePractice.db` is created next to the executable on first run (can be easily extended to support specific location as well)

### Run the Application

1. Open `cp-data-migrator.slnx`.
2. Set `CP.Migrator.UI` as startup project.
3. Build and run.
4. Enter a Clinic ID (numeric, >= 1).
5. Load `patient.csv` and/or `treatment.csv`.
6. Review validation results (row colors: red errors, yellow warnings, blue auto-fixed, white valid).
7. Use `Auto-Fix All` and/or edit rows directly. Use Undo/Redo as needed.
8. Optionally delete selected rows from the active tab.
9. Optionally export invalid rows from the active tab.
10. Click `Ingest All` to write valid rows and review the ingestion report dialog.

### Run Tests
Tests use xUnit. Run from **Test Explorer** in Visual Studio or via:

```bash
dotnet test
```

Integration tests use an in-memory SQLite helper (`FixedConnectionFactory`) - no setup required.

## CSV File Format

### patient.csv

| Column | Description |
|---|---|
| Id | Legacy patient ID (stored as `PatientNo`; reference only) |
| FirstName | Patient first name (required) |
| LastName | Patient last name (required) |
| DOB | Date of birth (normalized to `yyyy-MM-dd` by auto-fix where possible) |
| Gender | `M` / `F` / `O` (auto-fixed to uppercase) |
| Email | Email address (regex validated) |
| MobileNumber | Australian mobile; 10 digits starting with `04` (normalized) |
| PhoneNumber | Australian phone number (normalized) |
| Street | Street address |
| Suburb | Suburb |
| State | Australian state/territory: `NSW`, `VIC`, `QLD`, `SA`, `WA`, `TAS`, `NT`, `ACT` |
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
| Fee | Fee amount (required, positive decimal) |
| Date | Treatment date (normalized to `yyyy-MM-dd` by auto-fix where possible) |
| Paid | `Yes` / `No` (auto-fixed to title case) |
| ToothNumber | Tooth number (optional) |
| Surface | Tooth surface (optional) |
