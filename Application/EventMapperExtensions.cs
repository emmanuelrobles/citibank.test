using System.Diagnostics;
using System.Globalization;
using Application.Helpers;
using LangExtensions;

namespace Application;

public static class EventMapperExtensions
{
    /// <summary>
    /// Gets event header index or a list of errors
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
    /// Get a row from a string or a list of errors
    /// </summary>
    /// <param name="header">headers metadata</param>
    /// <param name="rawRow">raw row data</param>
    /// <returns>return a either monad with an event or a list of errors</returns>
    public static ValueEither<EventRow, IEnumerable<string>> GetRowFromString(this EventHeaderIndexes header, string? rawRow)
    {
        if (string.IsNullOrEmpty(rawRow))
        {
            return new ValueEither<EventRow, IEnumerable<string>>(new[] { "Empty Row" });
        }

        var rowArr = rawRow.Split(DataConstants.Separator);

        if (rowArr.Length != 3)
        {
            if (rowArr.Length > 3)
            {
                return new ValueEither<EventRow, IEnumerable<string>>(new[] { "Extra field" });
            };
            return new ValueEither<EventRow, IEnumerable<string>>(new[] { "Missing a field" }); 
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
        var isValidTimeStamp = DateTime.TryParseExact(rowArr[header.TimeStampIndex], "M/d/yyyy h:mm:ss tt",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp);

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
    /// Gets either a list of errors or a Event raw data
    /// </summary>
    /// <param name="eventFile"></param>
    /// <returns></returns>
    public static ValueEither<EventRawData, IEnumerable<string>> GetEventRawDataFromEventFile(this EventFile eventFile)
    {
        // get a stream to the file
        var stream = FileStreamHelper.GetStreamForInputDataFile(eventFile);
        // open the reader
        var streamReader = new StreamReader(stream, leaveOpen: true);
        // get the headers
        var rawHeaders = streamReader.ReadLine();

        // gets the row
        static IEnumerable<string> Rows(StreamReader stream, EventFile eventFile)
        {
            // discard already processed data
            for (var i = 0; i < eventFile.StartingAtRow; i++)
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
        return new ValueEither<EventRawData, IEnumerable<string>>(new EventRawData(eitherHeaders.Left,
            Rows(streamReader, eventFile)));
    }

    /// <summary>
    /// Check for possible errors on the row creation, if any enrich the details with row number
    /// </summary>
    /// <param name="eitherRawData"></param>
    /// <param name="rowNumber"></param>
    /// <returns></returns>
    public static ValueEither<EventRow, IEnumerable<string>> EnrichErrorWithRowNumber(this ValueEither<EventRow, IEnumerable<string>> eitherRawData,
        int rowNumber)
    {
        // check for errors
        if (eitherRawData.Right is not null)
        {
            return new ValueEither<EventRow, IEnumerable<string>>(
                eitherRawData.Right.Select(e => $"{e}, on Row number {rowNumber}"));
        }

        return eitherRawData;
    }
}
