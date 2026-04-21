CREATE TABLE IF NOT EXISTS tblPatient (
    PatientId INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientIdentifier TEXT NOT NULL,
    PatientNo TEXT,
    Firstname TEXT NOT NULL,
    Lastname TEXT NOT NULL,
    Middlename TEXT,
    PreferredName TEXT,
    DateOfBirth TEXT, -- MSSQL: datetime; stored as ISO 8601 TEXT in SQLite (type mismatch)
    Title TEXT,
    Sex TEXT,
    Email TEXT,
    HomePhone TEXT,
    Mobile TEXT,
    Occupation TEXT,
    CompanyName TEXT,
    AddressLine1 TEXT,
    AddressLine2 TEXT,
    Suburb TEXT,
    Postcode TEXT,
    State TEXT,
    Country TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0 -- MSSQL: bit; SQLite has no boolean type, stored as INTEGER 0/1 (type mismatch)
);

CREATE TABLE IF NOT EXISTS tblInvoice (
    InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceIdentifier TEXT NOT NULL,
    InvoiceNo INTEGER NOT NULL,
    InvoiceDate TEXT, -- MSSQL: datetime; stored as ISO 8601 TEXT in SQLite (type mismatch)
    DueDate TEXT, -- MSSQL: datetime; stored as ISO 8601 TEXT in SQLite (type mismatch)
    Note TEXT,
    Total REAL NOT NULL, -- MSSQL: decimal(19,4); SQLite REAL is IEEE 754 double (floating-point) — exact decimal precision is NOT guaranteed (type mismatch)
    Paid REAL, -- MSSQL: decimal(19,4); same precision caveat as Total
    Discount REAL, -- MSSQL: decimal(19,4); same precision caveat as Total
    PatientId INTEGER NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0, -- MSSQL: bit; SQLite has no boolean type, stored as INTEGER 0/1 (type mismatch)
    FOREIGN KEY (PatientId) REFERENCES tblPatient(PatientId)
);

CREATE TABLE IF NOT EXISTS tblTreatment (
    TreatmentId INTEGER PRIMARY KEY AUTOINCREMENT,
    TreatmentIdentifier TEXT NOT NULL,
    CompleteDate TEXT, -- MSSQL: datetime; stored as ISO 8601 TEXT in SQLite (type mismatch)
    Description TEXT NOT NULL,
    ItemCode TEXT NOT NULL,
    Tooth TEXT,
    Surface TEXT,
    Quantity INTEGER NOT NULL,
    Fee REAL NOT NULL, -- MSSQL: decimal(19,4); SQLite REAL is IEEE 754 double (floating-point) — exact decimal precision is NOT guaranteed (type mismatch)
    InvoiceId INTEGER,
    PatientId INTEGER NOT NULL,
    IsPaid INTEGER NOT NULL DEFAULT 0, -- MSSQL: bit; SQLite has no boolean type, stored as INTEGER 0/1 (type mismatch)
    IsVoided INTEGER NOT NULL DEFAULT 0, -- MSSQL: bit; SQLite has no boolean type, stored as INTEGER 0/1 (type mismatch)
    FOREIGN KEY (PatientId) REFERENCES tblPatient(PatientId),
    FOREIGN KEY (InvoiceId) REFERENCES tblInvoice(InvoiceId)
);

CREATE TABLE IF NOT EXISTS tblInvoiceLineItem (
    InvoiceLineItemId INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceLineItemIdentifier TEXT NOT NULL,
    Description TEXT NOT NULL,
    ItemCode TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitAmount REAL NOT NULL, -- MSSQL: decimal(19,4); SQLite REAL is IEEE 754 double (floating-point) — exact decimal precision is NOT guaranteed (type mismatch)
    LineAmount REAL NOT NULL, -- MSSQL: decimal(19,4); same precision caveat as UnitAmount
    PatientId INTEGER NOT NULL,
    TreatmentId INTEGER NOT NULL,
    InvoiceId INTEGER NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES tblPatient(PatientId),
    FOREIGN KEY (TreatmentId) REFERENCES tblTreatment(TreatmentId),
    FOREIGN KEY (InvoiceId) REFERENCES tblInvoice(InvoiceId)
);
