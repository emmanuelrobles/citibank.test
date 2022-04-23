namespace Application;

/// <summary>
/// DTO that represent 1 file
/// </summary>
/// <param name="FileId">Field id</param>
/// <param name="StartingAtRow">what position start processing the rows from</param>
public record EventFile(string RootPath, string FileId, int StartingAtRow);

/// <summary>
/// DTO that holds the indexes of a event row
/// </summary>
/// <param name="EventIdIndex">Event Id index</param>
/// <param name="DetailsIndex">Details Index</param>
/// <param name="TimeStampIndex">TimeStamp index</param>
public record EventHeaderIndexes(int EventIdIndex, int DetailsIndex, int TimeStampIndex);

/// <summary>
/// DTO that holds the indexes header and the rows
/// </summary>
/// <param name="HeadersIndexes">Headers indexes</param>
/// <param name="RawRows">Raw rows data</param>
public record EventRawData(EventHeaderIndexes HeadersIndexes, IEnumerable<string> RawRows);

/// <summary>
/// DTO that holds the event data
/// </summary>
/// <param name="EventId"></param>
/// <param name="Details"></param>
/// <param name="Timestamp"></param>
public record EventRow(long EventId, string Details, DateTime Timestamp);
