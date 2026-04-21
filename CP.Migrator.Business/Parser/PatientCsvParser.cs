using CP.Migrator.Models.Csv;
using CP.Migrator.Models.Results;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace CP.Migrator.Business.Parser
{
	/// <summary>
	/// Parses patient.csv into a sequence of <see cref="ParsedRow{PatientCsvRow}"/>.
	/// Each data row is returned even if it could not be parsed — parse failures are
	/// recorded as <see cref="ValidationError"/> entries so they appear in the UI grid.
	/// </summary>
	internal class PatientCsvParser : ICsvParserService<PatientCsvRow>
	{
		public IEnumerable<ParsedRow<PatientCsvRow>> Parse(string filePath)
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

			csv.Context.RegisterClassMap<PatientCsvRowMap>();

			var results = new List<ParsedRow<PatientCsvRow>>();
			int rowIndex = 1;

			while (csv.Read())
			{
				ParsedRow<PatientCsvRow> parsedRow;

				try
				{
					var row = csv.GetRecord<PatientCsvRow>()!;
					row.RowIndex = rowIndex;
					parsedRow = new ParsedRow<PatientCsvRow>(row);
				}
				catch (Exception ex)
				{
					// Produce a skeleton row so the UI can display the failure at the correct line
					var errorRow = new PatientCsvRow
					{
						RowIndex = rowIndex,
						RawSourceId = $"row-{rowIndex}"
					};
					parsedRow = new ParsedRow<PatientCsvRow>(errorRow);
					parsedRow.Errors.Add(new ValidationError("CSV", $"Row could not be parsed: {ex.Message}"));
				}

				results.Add(parsedRow);
				rowIndex++;
			}

			return results;
		}
	}

	/// <summary>Maps CSV column names to <see cref="PatientCsvRow"/> properties.</summary>
	internal sealed class PatientCsvRowMap : ClassMap<PatientCsvRow>
	{
		public PatientCsvRowMap()
		{
			Map(m => m.RawSourceId).Name("Id");
			Map(m => m.FirstName).Name("FirstName");
			Map(m => m.LastName).Name("LastName");
			Map(m => m.DOB).Name("DOB");
			Map(m => m.Gender).Name("Gender");
			Map(m => m.Email).Name("Email");
			Map(m => m.MobileNumber).Name("MobileNumber");
			Map(m => m.PhoneNumber).Name("PhoneNumber");
			Map(m => m.Street).Name("Street");
			Map(m => m.Suburb).Name("Suburb");
			Map(m => m.State).Name("State");
			Map(m => m.Postcode).Name("Postcode");
		}
	}
}

