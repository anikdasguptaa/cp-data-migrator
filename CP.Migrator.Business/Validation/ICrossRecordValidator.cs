using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Business.Validation
{
    /// <summary>
    /// Validates a single parsed row that requires context from a different record type.
    /// For example, a treatment row needs to verify its PatientID exists in the loaded
    /// patient data — that cross-reference context is passed via <paramref name="contextRows"/>
    /// rather than being injected at construction time, keeping the validator stateless
    /// and DI-friendly.
    /// </summary>
    /// <typeparam name="TRow">The row type being validated.</typeparam>
    /// <typeparam name="TContextRow">The row type that provides cross-reference context.</typeparam>
    public interface ICrossRecordValidator<TRow, TContextRow>
        where TRow : CsvRow
        where TContextRow : CsvRow
    {
        /// <summary>
        /// Validates <paramref name="parsedRow"/> in the context of its own
        /// <paramref name="allRows"/> and a set of <paramref name="contextRows"/>
        /// from a different record type. Appends errors directly to <paramref name="parsedRow"/>.
        /// </summary>
        void Validate(
            ParsedRow<TRow> parsedRow,
            IEnumerable<ParsedRow<TRow>> allRows,
            IEnumerable<ParsedRow<TContextRow>> contextRows);
    }
}
