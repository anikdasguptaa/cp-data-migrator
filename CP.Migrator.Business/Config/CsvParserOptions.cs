namespace CP.Migrator.Business.Config;

/// <summary>
/// Tunable safety limits applied by the CSV parsers before and during file reading.
/// Defaults are conservative limits suitable for a dental-practice migration workload.
/// Override individual properties via DI registration or configuration binding.
/// </summary>
public class CsvParserOptions
{
    /// <summary>
    /// Maximum permitted file size in bytes. Files larger than this are rejected before
    /// any rows are read. Default is 50 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50L * 1024 * 1024;

    /// <summary>
    /// Maximum number of data rows that will be parsed from a single file.
    /// If this limit is reached the parser throws rather than continuing to
    /// materialise rows into memory. Default is 100 000.
    /// </summary>
    public int MaxRowCount { get; set; } = 100_000;

    /// <summary>
    /// When <see langword="true"/> (default) the parser rejects UNC network paths
    /// (e.g. <c>\\server\share\file.csv</c>).
    /// </summary>
    public bool RejectUncPaths { get; set; } = true;

    /// <summary>
    /// When set, the resolved absolute path of the supplied file must start with this
    /// directory. Use this to restrict parsing to a known safe folder.
    /// Leave <see langword="null"/> (default) to allow any local path.
    /// </summary>
    public string? AllowedBaseDirectory { get; set; } = null;
}
