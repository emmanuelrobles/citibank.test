using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Application;
using Application.Helpers;
using Service.Settings;

namespace Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ServiceSettings _serviceSettings;
    private readonly Subject<(EventFile file, IEnumerable<EventRow> rows)> _validFilesQueue;
    private readonly Subject<(EventFile file, IEnumerable<string> errors)> _invalidFilesQueue;
    
    public Worker(ILogger<Worker> logger, ServiceSettings serviceSettings)
    {
        _logger = logger;
        _serviceSettings = serviceSettings;
        _validFilesQueue = new Subject<(EventFile file, IEnumerable<EventRow> rows)>();
        _invalidFilesQueue = new Subject<(EventFile file, IEnumerable<string> errors)>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        using (_validFilesQueue
                   .ObserveOn(new NewThreadScheduler())
                   .Subscribe(tuple =>
               {
                   _logger.LogInformation("new valid file");
                   FileCreationHelper.CreateOutputFileWithRows(tuple.file,tuple.rows);
                   FileCreationHelper.CreateOutputHashFile(tuple.file);
                   FileMovementHelper.MoveValidProcessedFile(tuple.file);
               }))

        using (_invalidFilesQueue
                   .ObserveOn(new NewThreadScheduler())
                   .Subscribe(tuple =>
               {
                   _logger.LogInformation("Bad queue {fileId}, errors: {@Errors}", tuple.file.FileId, tuple.errors);
                   FileCreationHelper.CreateErrorFileWithMessages(tuple.file, tuple.errors);
                   FileMovementHelper.MoveInvalidProcessedFile(tuple.file);
               }))
            
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting to process files");
            
            FileValidationHelper.GetAllFiles(_serviceSettings.RootPath)
                .AsParallel()
                .ForAll(eventFile =>
                {
                    using (_logger.BeginScope("Processing file {FileID}, starting at row {StartingRow}", eventFile.FileId, eventFile.StartingAtRow))
                    {
                        
                        _logger.LogInformation("Checking checksum of file {FileId}", eventFile.FileId);
                        if (!FileHashHelper.IsHashValidForFileId(eventFile))
                        {
                            _invalidFilesQueue.OnNext((eventFile,new []{"MD5 doesnt match"}));
                            return;
                        }
                        _logger.LogTrace("Checksum match");
                        
                        // Getting raw data
                        _logger.LogInformation("Starting to parse data");
                        var rawData = eventFile.GetEventRawDataFromEventFile();
                        _logger.LogInformation("headers parsed");

                        if (rawData.Right is not null)
                        {
                            _logger.LogWarning("Errors found on the headers");
                            // add bad file to bad files subject
                            _invalidFilesQueue.OnNext((eventFile,rawData.Right));
                            return;
                        }

                        _logger.LogInformation("starting to parse body");
                        
                        Debug.Assert(rawData.Left != null, "rawData.Left != null");
                        // processing the rows given a header index
                        var rows = EventRowProcessorHelper.EnumerateInMemoryEventRows(
                            rawData.Left.RawRows.
                                Select((rawRow, rowNumber) =>
                                rawData.Left.HeadersIndexes
                                    .GetRowFromString(rawRow)
                                    // enrich errors with the row number
                                    .EnrichErrorWithRowNumber(rowNumber)));
                        
                        if (rows.Right is not null)
                        {
                            _logger.LogWarning("Errors found on the body");
                            _invalidFilesQueue.OnNext((eventFile,rows.Right));
                            return;
                        }
                        
                        Debug.Assert(rows.Left != null, "rows.Left != null");
                        _validFilesQueue.OnNext((eventFile,rows.Left
                            .DistinctBy(r => r.EventId)
                            .OrderBy(r => r.Timestamp)
                            .ThenBy(r => r.EventId)));
                    }
                });

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        }
    }
}
