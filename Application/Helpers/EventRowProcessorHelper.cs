using System.Diagnostics;
using LangExtensions;

namespace Application.Helpers;

public static class EventRowProcessorHelper
{
    /// <summary>
    /// Enumerate the events on memory, THIS IS BAD, i just didnt have enough time to create one in memory
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static ValueEither<IEnumerable<EventRow>, IEnumerable<string>> EnumerateInMemoryEventRows(
        IEnumerable<ValueEither<EventRow, IEnumerable<string>>> data)
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
    
}
