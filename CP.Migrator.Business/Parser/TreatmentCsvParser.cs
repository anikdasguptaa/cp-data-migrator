using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace CP.Migrator.Business.Parser
{
	/// <summary>
	/// Parses treatment.csv into a sequence of <see cref="ParsedRow{TreatmentCsvRow}"/>.
	/// Numeric fields (Price, Fee) are kept as raw strings so type-conversion failures
	/// surface as user-friendly validation errors rather than unhandled exceptions.
	/// </summary>
	internal class TreatmentCsvParser : ICsvParserService<TreatmentCsvRow>
	{
		public IEnumerable<ParsedRow<TreatmentCsvRow>> Parse(string filePath)
		{
			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				MissingFieldFound = null,    // silently ignore extra/missing columns
				HeaderValidated = null,      // do not throw on unexpected headers
				TrimOptions = TrimOptions.Trim
			};

			using var reader = new StreamReader(filePath);
			using var csv = new CsvReader(reader, config);

			csv.Context.RegisterClassMap<TreatmentCsvRowMap>();

			var results = new List<ParsedRow<TreatmentCsvRow>>();
			int rowIndex = 1;

			while (csv.Read())
			{
				ParsedRow<TreatmentCsvRow> parsedRow;

				try
				{
					var row = csv.GetRecord<TreatmentCsvRow>()!;
					row.RowIndex = rowIndex;
					parsedRow = new ParsedRow<TreatmentCsvRow>(row);
				}
				catch (Exception ex)
				{
					var errorRow = new TreatmentCsvRow
					{
						RowIndex = rowIndex,
						RawSourceId = $"row-{rowIndex}"
					};
					parsedRow = new ParsedRow<TreatmentCsvRow>(errorRow);
					parsedRow.Errors.Add(new ValidationError("CSV", $"Row could not be parsed: {ex.Message}"));
				}

				results.Add(parsedRow);
				rowIndex++;
			}

			return results;
		}
	}

	/// <summary>Maps CSV column names to <see cref="TreatmentCsvRow"/> properties.</summary>
	internal sealed class TreatmentCsvRowMap : ClassMap<TreatmentCsvRow>
	{
		public TreatmentCsvRowMap()
		{
			Map(m => m.RawSourceId).Name("Id");
			Map(m => m.RawPatientSourceId).Name("PatientID");
			Map(m => m.DentistId).Name("DentistID");
			Map(m => m.TreatmentItem).Name("TreatmentItem");
			Map(m => m.Description).Name("Description");
			Map(m => m.Price).Name("Price");
			Map(m => m.Fee).Name("Fee");
			Map(m => m.Date).Name("Date");
			Map(m => m.Paid).Name("Paid");
			Map(m => m.ToothNumber).Name("ToothNumber");
			Map(m => m.Surface).Name("Surface");
		}
	}
}

