using DevOpsifyMe.WslSocketProxy.Model;
using Microsoft.Extensions.Options;

namespace DevOpsifyMe.WslSocketProxy;

public class Worker : BackgroundService
{
    private readonly MyOptions _options;
    private readonly IServiceProvider _services;
    private readonly ILogger<Worker> _logger;

    public Worker(IOptions<MyOptions> options, IServiceProvider services, ILogger<Worker> logger)
    {
        _options = options.Value;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var forwarding in _options.Forwardings)
        {
            _logger.LogInformation("Starting socket manager for distribution '{distribution}' {unix} <= {npipe}",
                forwarding.Distribution, forwarding.Unix, forwarding.Npipe);

            var manager = _services.GetRequiredService<PipeServersManager>();
            manager.Run(forwarding, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000);
        }
    }
}
