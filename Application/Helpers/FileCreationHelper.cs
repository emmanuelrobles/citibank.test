namespace Application.Helpers;

/// <summary>
/// Helpers for file creation
/// </summary>
public static class FileCreationHelper
{
    /// <summary>
    /// Creates an output file
    /// </summary>
    /// <param name="file"></param>
    /// <param name="rows"></param>
    /// <returns></returns>
    public static void CreateOutputFileWithRows(EventFile file, IEnumerable<EventRow> rows)
    {
        static string RowToString(EventRow row)
            => $"{row.Timestamp}{DataConstants.Separator}{row.EventId}{DataConstants.Separator}{row.Details}";

        static string StringHeaders() =>
            $"{HeaderConstants.Timestamp}{DataConstants.Separator}{HeaderConstants.EventId}{DataConstants.Separator}{HeaderConstants.Details}";

        static IEnumerable<string> Content(IEnumerable<EventRow> rows)
        {
            yield return StringHeaders();

            foreach (var stringRow in rows.Select(RowToString))
            {
                yield return stringRow;
            }
        }


        CreateFile(file.RootPath, FolderDistributionConstants.OutputFolder,
            $"{file.FileId}{FileIdentifiersConstants.Result}{ExtensionFileConstants.DataFileExtension}", Content(rows));
    }

    /// <summary>
    /// Given a file creates a checksum
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static void CreateOutputHashFile(EventFile file)
        => CreateFile(file.RootPath, FolderDistributionConstants.OutputFolder,
            $"{file.FileId}{FileIdentifiersConstants.Result}{ExtensionFileConstants.CheckSumFileExtension}",
            new []{FileHashHelper.GetHashForOutputFileId(file)});


    /// <summary>
    /// Create a file with the errors found in a transaction
    /// </summary>
    /// <param name="file"></param>
    /// <param name="errors"></param>
    /// <returns></returns>
    public static void CreateErrorFileWithMessages(EventFile file, IEnumerable<string> errors)
        => CreateFile(file.RootPath, FolderDistributionConstants.ErrorFolder,
            $"{file.FileId}{FileIdentifiersConstants.Error}{ExtensionFileConstants.TextFileExtension}",
            errors);
    
    
    /// <summary>
    /// Creates a file on the given path
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="folder"></param>
    /// <param name="fileName"></param>
    /// <param name="data"></param>
    /// <returns> return full path</returns>
    public static void CreateFile(string rootPath, string folder, string fileName, IEnumerable<string> data)
    {
        var path = rootPath.AddToPath(folder).AddToPath(fileName);
        File.WriteAllLines(path, data);
    }


}
