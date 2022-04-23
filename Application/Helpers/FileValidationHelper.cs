using System.Reactive.Linq;

namespace Application.Helpers;

/// <summary>
/// Helps with the file validation
/// </summary>
public static class FileValidationHelper
{
    /// <summary>
    /// Get a list of file ids that have a .dat and .md5
    /// </summary>
    /// <param name="files">list of all the files</param>
    /// <returns>Files </returns>
    public static IEnumerable<string> GetValidInputs(IEnumerable<string> files)
        => files.GroupBy(Path.GetFileNameWithoutExtension)
            .Where(groupedResources =>
                groupedResources.Any(
                    resource => Path.GetExtension(resource) == ExtensionFileConstants.DataFileExtension) &&
                groupedResources.Any(resource =>
                    Path.GetExtension(resource) == ExtensionFileConstants.CheckSumFileExtension))
            .Select(validResources => validResources.Key ?? string.Empty);
    
    /// <summary>
    /// Gets all files ready to be process as observalble
    /// </summary>
    /// <param name="rootPath"></param>
    /// <returns></returns>
    public static IObservable<EventFile> GetAllFilesObservable(string rootPath)
        => GetAllFiles(rootPath).ToObservable();

    /// <summary>
    /// Gets all files ready to be process, it will start to process the rows at position 0 
    /// </summary>
    /// <param name="rootPath"></param>
    /// <returns></returns>
    public static IEnumerable<EventFile> GetAllFiles(string rootPath)
        => GetValidInputs(
                Directory.GetFiles($"{rootPath}{Path.DirectorySeparatorChar}{FolderDistributionConstants.InputFolder}"))
            .Select(fileId => new EventFile(rootPath, fileId, 0));
}
