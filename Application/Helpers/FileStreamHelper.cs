namespace Application.Helpers;

/// <summary>
/// Helper for stream files
/// </summary>
public static class FileStreamHelper
{
    /// <summary>
    /// Curried func to get the stream from a file
    /// </summary>
    /// <param name="rootPath"></param>
    /// <returns></returns>
    public static Func<string, Func<string, Stream>> GetFileStream(string rootPath)
        => folder
            => fileName => File.OpenRead(
                $"{rootPath}{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}{fileName}");


    /// <summary>
    /// Gets the stream from a data input file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static Stream GetStreamForInputDataFile(EventFile file)
        => GetFileStream(file.RootPath)(FolderDistributionConstants.InputFolder)(
            $"{file.FileId}{ExtensionFileConstants.DataFileExtension}");

    /// <summary>
    /// Gets the stream from a data output file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static Stream GetStreamForOutputDataFile(EventFile file)
        => GetFileStream(file.RootPath)(FolderDistributionConstants.OutputFolder)(
            $"{file.FileId}{FileIdentifiersConstants.Result}{ExtensionFileConstants.DataFileExtension}");

}
