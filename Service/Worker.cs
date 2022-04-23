using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Application;
using Service.Dtos;

namespace Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ServiceSettings _serviceSettings;
    private readonly Subject<(EventFile file, IEnumerable<EventRow> rows)> _goodFilesQueue;
    private readonly Subject<(EventFile file, IEnumerable<string> errors)> _badFilesQueue;
    
    public Worker(ILogger<Worker> logger, ServiceSettings serviceSettings)
    {
        _logger = logger;
        _serviceSettings = serviceSettings;
        _goodFilesQueue = new Subject<(EventFile file, IEnumerable<EventRow> rows)>();
        _badFilesQueue = new Subject<(EventFile file, IEnumerable<string> errors)>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var goodQueueTask = _goodFilesQueue.Do(tuple =>
        {
            _logger.LogInformation("Good queue {fileId}", tuple.file.FileId);
        }).Subscribe();
        
        var badQueueTask = _badFilesQueue.Do(tuple =>
        {
            _logger.LogInformation("Bad queue {fileId}, errors: {@Errors}", tuple.file.FileId, tuple.errors);
        }).Subscribe();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting to process files");
            
            FilesExtensions.GetAllFiles(_serviceSettings.RootPath)
                .AsParallel().ForAll(eventFile =>
                {
                    using (_logger.BeginScope("Processing file {FileID}, starting at row {StartingRow}", eventFile.FileId, eventFile.StartingAtRow))
                    {
                        
                        _logger.LogInformation("Checking checksum of file {FileId}", eventFile.FileId);
                        if (!FilesExtensions.IsHashValidForFileId(eventFile.RootPath, eventFile.FileId))
                        {
                            _badFilesQueue.OnNext((eventFile,new []{"MD5 doesnt match"}));
                            return;
                        }
                        _logger.LogTrace("Checksum match");
                        
                        // Getting raw data
                        _logger.LogInformation("Starting to parse data");
                        var rawData = Extensions.GetEventRawDataFromEventFile(eventFile);
                        _logger.LogInformation("headers parsed");

                        if (rawData.Right is not null)
                        {
                            _logger.LogWarning("Errors found on the headers");
                            // add bad file to bad files subject
                            _badFilesQueue.OnNext((eventFile,rawData.Right));
                            return;
                        }

                        _logger.LogInformation("starting to parse body");
                        
                        Debug.Assert(rawData.Left != null, "rawData.Left != null");
                        var rows = FilesExtensions.EnumerateInMemoryEventRows(
                            rawData.Left.RawRows.Select(rawRow =>
                                Extensions.GetRowFromString(rawData.Left.HeadersIndexes, rawRow)));
                        
                        if (rows.Right is not null)
                        {
                            _logger.LogWarning("Errors found on the body");
                            _badFilesQueue.OnNext((eventFile,rows.Right));
                            return;
                        }
                        
                        Debug.Assert(rows.Left != null, "rows.Left != null");
                        _goodFilesQueue.OnNext((eventFile,rows.Left));
                    }
                });

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        }
    }
}
