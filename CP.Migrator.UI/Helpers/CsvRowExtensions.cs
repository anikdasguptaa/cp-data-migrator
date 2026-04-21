using CP.Migrator.Models.Csv;

namespace CP.Migrator.UI.Helpers;

/// <summary>
/// Extension methods for shallow-cloning and field-copying CSV row models.
/// Used by the undo/redo subsystem so it can snapshot row state before
/// and after an edit without modifying the business-layer models.
/// </summary>
internal static class CsvRowExtensions
{
    // PatientCsvRow

    /// <summary>Returns a shallow clone of <paramref name="src"/>, copying all fields including identity columns.</summary>
    public static PatientCsvRow Clone(this PatientCsvRow src) => new()
    {
        RowIndex = src.RowIndex,
        RawSourceId = src.RawSourceId,
        FirstName = src.FirstName,
        LastName = src.LastName,
        DOB = src.DOB,
        Gender = src.Gender,
        Email = src.Email,
        MobileNumber = src.MobileNumber,
        PhoneNumber = src.PhoneNumber,
        Street = src.Street,
        Suburb = src.Suburb,
        State = src.State,
        Postcode = src.Postcode,
    };

    /// <summary>Copies all editable fields from <paramref name="src"/> into <paramref name="dest"/>, leaving identity columns unchanged.</summary>
    public static void CopyTo(this PatientCsvRow src, PatientCsvRow dest)
    {
        dest.FirstName = src.FirstName;
        dest.LastName = src.LastName;
        dest.DOB = src.DOB;
        dest.Gender = src.Gender;
        dest.Email = src.Email;
        dest.MobileNumber = src.MobileNumber;
        dest.PhoneNumber = src.PhoneNumber;
        dest.Street = src.Street;
        dest.Suburb = src.Suburb;
        dest.State = src.State;
        dest.Postcode = src.Postcode;
    }

    // TreatmentCsvRow

    /// <summary>Returns a shallow clone of <paramref name="src"/>, copying all fields including identity columns.</summary>
    public static TreatmentCsvRow Clone(this TreatmentCsvRow src) => new()
    {
        RowIndex = src.RowIndex,
        RawSourceId = src.RawSourceId,
        RawPatientSourceId = src.RawPatientSourceId,
        DentistId = src.DentistId,
        TreatmentItem = src.TreatmentItem,
        Description = src.Description,
        Price = src.Price,
        Fee = src.Fee,
        Date = src.Date,
        Paid = src.Paid,
        ToothNumber = src.ToothNumber,
        Surface = src.Surface,
    };

    /// <summary>Copies all editable fields from <paramref name="src"/> into <paramref name="dest"/>, leaving identity columns unchanged.</summary>
    public static void CopyTo(this TreatmentCsvRow src, TreatmentCsvRow dest)
    {
        dest.RawPatientSourceId = src.RawPatientSourceId;
        dest.DentistId = src.DentistId;
        dest.TreatmentItem = src.TreatmentItem;
        dest.Description = src.Description;
        dest.Price = src.Price;
        dest.Fee = src.Fee;
        dest.Date = src.Date;
        dest.Paid = src.Paid;
        dest.ToothNumber = src.ToothNumber;
        dest.Surface = src.Surface;
    }
}
