using System.Diagnostics;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using LangExtensions;

namespace Application;

public class FilesExtensions
{
    public static IObservable<EventFile> GetAllFilesObservable(string rootPath)
        => GetAllFiles(rootPath).ToObservable();

    public static IEnumerable<EventFile> GetAllFiles(string rootPath)
        => Extensions
            .GetValidInputs(
                Directory.GetFiles($"{rootPath}{Path.DirectorySeparatorChar}{FolderDistributionConstants.InputFolder}"))
            .Select(fileId => new EventFile(rootPath, fileId, 0));
    

    public static ValueEither<IEnumerable<EventRow>, IEnumerable<string>> EnumerateInMemoryEventRows(IEnumerable<ValueEither<EventRow,IEnumerable<string>>> data)
    {
        // enumerate the rows
        var linkedList = new LinkedList<EventRow>();

        // loop trough them
        foreach (var eitherRow in data)
        {
            // check for errors
            if (eitherRow.Right is not null)
            {
                // return if any
                return new ValueEither<IEnumerable<EventRow>, IEnumerable<string>>(eitherRow.Right);
            }

            Debug.Assert(eitherRow.Left != null, "eitherRow.Left != null");
            linkedList.AddFirst(eitherRow.Left);
        }
        
        return new ValueEither<IEnumerable<EventRow>, IEnumerable<string>>(linkedList);
    }


    public static bool IsHashValidForFileId(string rootPath, string fileId)
    {
        static string ToHex(byte[] bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Length*2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString("X2"));

            return result.ToString();
        }
        using var md5 = MD5.Create();
        using var stream = Extensions.GetDataInputStream(rootPath,fileId);
        var s1 = Extensions.GetHashForFieldId(rootPath, fileId).ToUpper();
        var s2 = ToHex(md5.ComputeHash(stream));
        return s1 == s2;
    } 
    
}
