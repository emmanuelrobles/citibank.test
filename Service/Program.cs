using Service;
using Service.Dtos;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        var settings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        services.AddSingleton(settings);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
