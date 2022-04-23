namespace Application;

/// <summary>
/// Class that hold all the static extensions
/// </summary>
public static class Extensions
{
    internal static string AddToPath(this string basePath, string newPath)
        => $"{basePath}{Path.DirectorySeparatorChar}{newPath}";

}
