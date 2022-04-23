using System.Diagnostics;
using System.Globalization;
using LangExtensions;

namespace Application;

/// <summary>
/// Class that hold all the static extensions
/// </summary>
public static class Extensions
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
    /// Gets the stream from a data input field
    /// </summary>
    /// <param name="rootPath">rootPath</param>
    /// <param name="fieldId"></param>
    /// <returns></returns>
    public static Stream GetDataInputStream(string rootPath, string fieldId)
        => File.OpenRead(
            $"{rootPath}{Path.DirectorySeparatorChar}{FolderDistributionConstants.InputFolder}{Path.DirectorySeparatorChar}{fieldId}{ExtensionFileConstants.DataFileExtension}");


    public static string GetHashForFieldId(string rootPath, string fieldId)
        => File.ReadAllText(
            $"{rootPath}{Path.DirectorySeparatorChar}{FolderDistributionConstants.InputFolder}{Path.DirectorySeparatorChar}{fieldId}{ExtensionFileConstants.CheckSumFileExtension}");
    
    
    /// <summary>
    /// Try to get the headers from a raw string
    /// </summary>
    /// <param name="rawHeaders">optional raw headers</param>
    /// <returns>an either monad with a result or a set of errors</returns>
    public static ValueEither<EventHeaderIndexes, IEnumerable<string>> GetHeaderFromString(string? rawHeaders)
    {
        if (string.IsNullOrEmpty(rawHeaders))
        {
            return new ValueEither<EventHeaderIndexes, IEnumerable<string>>(new[] { "Empty headers" });
        }

        // worst case scenario 3 errors
        var errorsList = new List<string>(3);

        var headerArr = rawHeaders.Split(DataConstants.Separator);

        // we can add more checking if required and give a more precise error
        if (headerArr.Length is not 3)
        {
            return new ValueEither<EventHeaderIndexes, IEnumerable<string>>(new[] { "Extra or missing headers" });
        }

        // checking for eventId
        var eventIdIndex = Array.IndexOf(headerArr, HeaderConstants.EventId);

        if (eventIdIndex is -1)
        {
            errorsList.Add("Missing EventId header");
        }

        // checking for details
        var detailsIndex = Array.IndexOf(headerArr, HeaderConstants.Details);

        if (detailsIndex is -1)
        {
            errorsList.Add("Missing Details header");
        }

        // checking for timestamp
        var timeStampIndex = Array.IndexOf(headerArr, HeaderConstants.Timestamp);

        if (timeStampIndex is -1)
        {
            errorsList.Add("Missing Timestamp header");
        }

        // if any error return them
        if (errorsList.Any())
        {
            return new ValueEither<EventHeaderIndexes, IEnumerable<string>>(errorsList);
        }

        // return the event header 
        return new ValueEither<EventHeaderIndexes, IEnumerable<string>>(new EventHeaderIndexes(eventIdIndex,
            detailsIndex, timeStampIndex));
    }


    /// <summary>
    /// Get a row from a string
    /// </summary>
    /// <param name="header">headers metadata</param>
    /// <param name="rawRow">raw row data</param>
    /// <returns>return a either monad with an event or a list of errors</returns>
    public static ValueEither<EventRow, IEnumerable<string>> GetRowFromString(EventHeaderIndexes header, string? rawRow)
    {
        if (string.IsNullOrEmpty(rawRow))
        {
            return new ValueEither<EventRow, IEnumerable<string>>(new[] { "Empty Row" });
        }

        var rowArr = rawRow.Split(DataConstants.Separator);

        if (rowArr.Length != 3)
        {
            return new ValueEither<EventRow, IEnumerable<string>>(new[] { "Extra or missing Field" });
        }
        
        // maximum capacity
        var errorsList = new List<string>(2);

        // check for valid event Id
        var isValidEventId = long.TryParse(rowArr[header.EventIdIndex], out var eventId);

        if (!isValidEventId)
        {
            errorsList.Add("Not a valid event Id");
        }
        
        // check for a valid timestamp
        var isValidTimeStamp = DateTime.TryParseExact(rowArr[header.TimeStampIndex], "M/d/yyyy h:mm:ss tt",CultureInfo.InvariantCulture, DateTimeStyles.None,out var timestamp);

        if (!isValidTimeStamp)
        {
            errorsList.Add("Not a valid timestamp");
        }

        if (errorsList.Any())
        {
            return new ValueEither<EventRow, IEnumerable<string>>(errorsList);
        }

        return new ValueEither<EventRow, IEnumerable<string>>(new EventRow(eventId, rowArr[header.DetailsIndex],
            timestamp));
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventFile"></param>
    /// <returns></returns>
    public static ValueEither<EventRawData,IEnumerable<string>> GetEventRawDataFromEventFile(EventFile eventFile)
    {
        // get a stream to the file
        var stream = GetDataInputStream(eventFile.RootPath, eventFile.FileId);
        // open the reader
        var streamReader = new StreamReader(stream, leaveOpen:true);
        // get the headers
        var rawHeaders = streamReader.ReadLine();
        
        // gets the row
        static IEnumerable<string> GetRows(StreamReader stream, EventFile eventFile)
        {
            // discard already processed data
            for(var i = 0; i < eventFile.StartingAtRow; i++)
            {
                stream.ReadLine();
            }
            
            // read the rest
            while (!stream.EndOfStream)
            {
                yield return stream.ReadLine() ?? string.Empty;
            }
            // close and flush
            stream.Close();
        }

        var eitherHeaders = GetHeaderFromString(rawHeaders);

        // check for errors
        if (eitherHeaders.Right is not null)
        {
            return new ValueEither<EventRawData, IEnumerable<string>>(eitherHeaders.Right);
        }

        Debug.Assert(eitherHeaders.Left != null, "eitherHeaders.Left != null");
        return new ValueEither<EventRawData, IEnumerable<string>>(new EventRawData(eitherHeaders.Left, GetRows(streamReader, eventFile)));
    }
}
