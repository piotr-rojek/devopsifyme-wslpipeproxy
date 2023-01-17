using DevOpsifyMe.WslSocketProxy.Model;
using System.Diagnostics;
using System.IO.Pipes;

namespace DevOpsifyMe.WslSocketProxy
{
    public class PipeServer
    {
        private const int BufferSize = 10000;

        public event EventHandler? ClientConnected;
        public event EventHandler? ClientDisconnected;

        private StreamWriter _stoutWriter;
        SocatProcessFactory _processFactory;
        private readonly ILogger<PipeServer> _logger;

        public PipeServer(SocatProcessFactory processFactory, ILogger<PipeServer> logger)
        {
            _processFactory = processFactory;
            _logger = logger;
            var stdout = Console.OpenStandardOutput();
            _stoutWriter = new StreamWriter(stdout);
        }

        public async Task StartAsync(Forwarding forwarding, CancellationToken cancellationToken)
        {
            var pipeServer = new NamedPipeServerStream(forwarding.Npipe, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await pipeServer.WaitForConnectionAsync();

            _logger.LogInformation("Client connected on {unix} < {npipe}", forwarding.Unix, forwarding.Npipe);

            ClientConnected?.Invoke(this, EventArgs.Empty);

            try
            {
                using var socatProcess = _processFactory.CreateWslProcess(forwarding.Distribution, forwarding.Unix);
                await HandleClientConnectionAsync(socatProcess, pipeServer, cancellationToken);
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            finally
            {
                _logger.LogInformation("Client disconnected on {unix} < {npipe}", forwarding.Unix, forwarding.Npipe);

                ClientDisconnected?.Invoke(this, EventArgs.Empty);
                pipeServer.Close();
            }
        }

        protected async Task HandleClientConnectionAsync(Process socatProcess, PipeStream pipeStream, CancellationToken cancellationToken)
        {
            using var pipeReader = new StreamReader(pipeStream);
            using var pipeWriter = new StreamWriter(pipeStream)
            {
                AutoFlush = true
            };

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var readTask = CopyStreamToAsync(pipeReader, socatProcess.StandardInput, cancellationSource.Token);
            var writeTask = CopyStreamToAsync(socatProcess.StandardOutput, pipeWriter, cancellationSource.Token);

            await Task.WhenAny(readTask, writeTask);
            cancellationSource.Cancel();
        }

        protected async Task CopyStreamToAsync(StreamReader reader, StreamWriter writer, CancellationToken cancellationToken)
        {
            char[] buffer = new char[BufferSize];
            int bytesRead;
            while ((bytesRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await writer.WriteAsync(buffer[0..bytesRead], cancellationToken);
                await _stoutWriter.WriteAsync(buffer[0..bytesRead], cancellationToken);
            }
        }
    }
}
