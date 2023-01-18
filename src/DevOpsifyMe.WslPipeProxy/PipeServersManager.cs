using DevOpsifyMe.WslSocketProxy.Model;
using System.Collections.Concurrent;

namespace DevOpsifyMe.WslSocketProxy
{
    public class PipeServersManager
    {
        private const int MinimumNumberOfServers = 2;
        private readonly IServiceProvider _services;
        private readonly ILogger<PipeServersManager> _logger;
        protected ConcurrentDictionary<PipeServer, Task> _knownServers = new ConcurrentDictionary<PipeServer, Task>();

        public PipeServersManager(IServiceProvider services, ILogger<PipeServersManager> logger)
        {
            _services = services;
            _logger = logger;
        }

        public void Run(Forwarding forwarding, CancellationToken cancellationToken)
        {
            for (int i = 0; i < MinimumNumberOfServers; i++)
            {
                AddServerAndStart(forwarding, cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                Task.WaitAny(_knownServers.Values.ToArray(), cancellationToken);

                if (_knownServers.Count < MinimumNumberOfServers)
                {
                    AddServerAndStart(forwarding, cancellationToken);
                }
            }
        }

        protected PipeServer AddServerAndStart(Forwarding forwarding, CancellationToken cancellationToken)
        {
            var pipeServer = CreateServer(forwarding, cancellationToken);
            var pipeServerTask = Task.Run(async () => await pipeServer.StartAsync(forwarding, cancellationToken));
            _knownServers.AddOrUpdate(pipeServer, pipeServerTask, (key, current) => current);
            return pipeServer;
        }

        protected PipeServer CreateServer(Forwarding forwarding, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Creating pipe server for distribition {distribution} {unix} <= {npipe}",
                forwarding.Distribution, forwarding.Unix, forwarding.Npipe);

            var pipeServer = _services.GetRequiredService<PipeServer>();
            pipeServer.ClientConnected += (sender, args) =>
            {
                var server = AddServerAndStart(forwarding, cancellationToken);

            };
            pipeServer.ClientDisconnected += (sender, args) => RemoveServer((PipeServer)sender!);

            _logger.LogDebug("Created pipe server for distribition {distribution} {unix} <= {npipe}",
                forwarding.Distribution, forwarding.Unix, forwarding.Npipe);

            return pipeServer;
        }

        protected void RemoveServer(PipeServer pipeServer)
        {
            _knownServers.Remove(pipeServer, out _);
        }
    }
}
