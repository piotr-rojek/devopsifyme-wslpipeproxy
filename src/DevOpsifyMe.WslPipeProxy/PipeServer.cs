using DevOpsifyMe.WslSocketProxy.Model;
using System.Diagnostics;
using System.IO.Pipes;

namespace DevOpsifyMe.WslSocketProxy
{
    public class PipeServer
    {
        private const int BufferSize = 1024*1024;

        public event EventHandler? ClientConnected;
        public event EventHandler? ClientDisconnected;

        SocatProcessFactory _processFactory;
        private readonly ILogger<PipeServer> _logger;

        public PipeServer(SocatProcessFactory processFactory, ILogger<PipeServer> logger)
        {
            _processFactory = processFactory;
            _logger = logger;
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
            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var readTask = CopyStreamToAsync(pipeStream, socatProcess.StandardInput.BaseStream, cancellationSource.Token);
            var writeTask = CopyStreamToAsync(socatProcess.StandardOutput.BaseStream, pipeStream, cancellationSource.Token);

            await Task.WhenAny(readTask, writeTask);
            cancellationSource.Cancel();
        }

        protected async Task CopyStreamToAsync(Stream reader, Stream writer, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            while ((bytesRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await writer.WriteAsync(buffer[0..bytesRead], cancellationToken);
            }
        }
    }
}
