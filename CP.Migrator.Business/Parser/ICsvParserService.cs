using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;

namespace CP.Migrator.Business.Parser
{
	/// <summary>
	/// Reads a CSV file and returns one <see cref="ParsedRow{TRow}"/> per data line.
	/// Implementations are responsible for mapping raw column text to the typed row model
	/// and assigning RowIndex / RawSourceId on each row.
	/// Validation is NOT performed here — parsing and validation are kept separate.
	/// </summary>
	public interface ICsvParserService<TRow> where TRow : CsvRow
	{
		/// <summary>Parses the file at <paramref name="filePath"/> and returns all rows.</summary>
		IEnumerable<ParsedRow<TRow>> Parse(string filePath);
	}
}
