namespace Application.Helpers;

/// <summary>
/// Helpers to move file around
/// </summary>
public static class FileMovementHelper
{
    /// <summary>
    /// Move a valid file after is done processing to its final location
    /// </summary>
    /// <param name="file"></param>
    public static void MoveValidProcessedFile(EventFile file)
        => MoveFromInputFile(file, FolderDistributionConstants.DoneFolder);
    
    /// <summary>
    /// Move an invalid file after is done processing to its final location
    /// </summary>
    /// <param name="file"></param>
    public static void MoveInvalidProcessedFile(EventFile file)
        => MoveFromInputFile(file, FolderDistributionConstants.ErrorFolder);

    /// <summary>
    /// Move files to a new location
    /// </summary>
    /// <param name="file"></param>
    /// <param name="newLoc"></param>
    public static void MoveFromInputFile(EventFile file, string newLoc)
    {
        var dataFileName = $"{file.FileId}{ExtensionFileConstants.DataFileExtension}";
        var checksumFileName = $"{file.FileId}{ExtensionFileConstants.CheckSumFileExtension}";

        var inputLoc = file.RootPath.AddToPath(FolderDistributionConstants.InputFolder);
        var newLocPath = file.RootPath.AddToPath(newLoc);
        
        File.Move(inputLoc.AddToPath(dataFileName), newLocPath.AddToPath(dataFileName));
        File.Move(inputLoc.AddToPath(checksumFileName), newLocPath.AddToPath(checksumFileName));
    }


}
