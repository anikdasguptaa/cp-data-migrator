using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Business.Validation
{
	/// <summary>
	/// Validates a single parsed row against schema and data rules for its own record type.
	/// For validators that need cross-reference context from a different record type,
	/// see <see cref="ICrossRecordValidator{TRow,TContextRow}"/>.
	/// </summary>
	public interface IRecordValidator<TRow> where TRow : CsvRow
	{
		/// <summary>
		/// Validates <paramref name="parsedRow"/> in the context of <paramref name="allRows"/>
		/// and appends any errors directly to <paramref name="parsedRow"/>.
		/// </summary>
		void Validate(ParsedRow<TRow> parsedRow, IEnumerable<ParsedRow<TRow>> allRows);
	}
}
