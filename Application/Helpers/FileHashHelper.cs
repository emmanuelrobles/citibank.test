using System.Security.Cryptography;
using System.Text;

namespace Application.Helpers;

public static class FileHashHelper
{
    /// <summary>
    /// Curried function to get the hash from a file
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static string GetHashFromFile(Stream stream)
    {
        static string ToHex(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString("X2"));

            return result.ToString();
        }

        using var md5 = MD5.Create();
        return ToHex(md5.ComputeHash(stream));
    }

    public static string GetHashForInputFileId(EventFile file)
        => GetHashFromFile(FileStreamHelper.GetStreamForInputDataFile(file));

    public static string GetHashForOutputFileId(EventFile file)
        => GetHashFromFile(FileStreamHelper.GetStreamForOutputDataFile(file));


    /// <summary>
    /// Check if the file id has a valid hash
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static bool IsHashValidForFileId(EventFile file)
    {
        static string HashForFieldId(EventFile file)
            => File.ReadAllText(
                $"{file.RootPath}{Path.DirectorySeparatorChar}{FolderDistributionConstants.InputFolder}{Path.DirectorySeparatorChar}{file.FileId}{ExtensionFileConstants.CheckSumFileExtension}");

        var actualHash = GetHashForInputFileId(file);
        var expectedHash = HashForFieldId(file).ToUpper();
        return expectedHash == actualHash;
    }
}
