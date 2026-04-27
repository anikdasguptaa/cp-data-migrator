using CP.Migrator.Business.Config;

namespace CP.Migrator.Business.Parser;

/// <summary>
/// Centralized guard that validates a CSV file path and size against
/// <see cref="CsvParserOptions"/> before any rows are read.
/// Throws standard BCL exceptions so callers can handle them uniformly.
/// </summary>
internal static class CsvParserGuard
{
    /// <summary>
    /// Validates <paramref name="filePath"/> and the file it points to.
    /// Returns the resolved absolute path that the parser should open.
    /// </summary>
    /// <exception cref="ArgumentException">Path is empty, has a wrong extension, is a UNC path (when disallowed), or falls outside the allowed base directory.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidOperationException">The file exceeds the configured size limit.</exception>
    internal static string ValidatePath(string filePath, CsvParserOptions options)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must not be empty.", nameof(filePath));

        if (options.RejectUncPaths && filePath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("UNC network paths are not permitted.", nameof(filePath));

        // Resolve symlinks and ..\ sequences to a canonical path
        var fullPath = Path.GetFullPath(filePath);

        if (!string.Equals(Path.GetExtension(fullPath), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must have a .csv extension.", nameof(filePath));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("CSV file not found.", fullPath);

        if (options.AllowedBaseDirectory is not null)
        {
            var allowedBase = Path.GetFullPath(options.AllowedBaseDirectory);
            if (!fullPath.StartsWith(allowedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, allowedBase, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Access to '{fullPath}' is not permitted. Files must reside under '{allowedBase}'.",
                    nameof(filePath));
            }
        }

        return fullPath;
    }

    /// <summary>
    /// Checks the file size against <see cref="CsvParserOptions.MaxFileSizeBytes"/>.
    /// Must be called after <see cref="ValidatePath"/> so the file is known to exist.
    /// </summary>
    /// <exception cref="InvalidOperationException">The file exceeds the configured size limit.</exception>
    internal static void ValidateFileSize(string fullPath, CsvParserOptions options)
    {
        var sizeBytes = new FileInfo(fullPath).Length;
        if (sizeBytes > options.MaxFileSizeBytes)
        {
            var limitMb = options.MaxFileSizeBytes / 1024.0 / 1024.0;
            var actualMb = sizeBytes / 1024.0 / 1024.0;
            throw new InvalidOperationException(
                $"CSV file is {actualMb:F1} MB which exceeds the {limitMb:F0} MB limit.");
        }
    }
}
