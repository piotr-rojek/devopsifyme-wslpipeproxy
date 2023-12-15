using System.Diagnostics;

namespace DevOpsifyMe.WslSocketProxy
{
    public class SocatProcessFactory
    {
        private readonly ILogger<SocatProcessFactory> _logger;

        public SocatProcessFactory(ILogger<SocatProcessFactory> logger)
        {
            _logger = logger;
        }

        public Process CreateWslProcess(string? distributionName, string unixSocketPath)
        {
            string distributionArg = distributionName == null ? string.Empty : $"-d {distributionName}";

            var info = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                FileName = "wsl.exe",
                Arguments = $"{distributionArg} -e socat - UNIX-CONNECT:{unixSocketPath}"
            };

            _logger.LogInformation("Starting {application}{arguments}", info.FileName, info.Arguments);

            var p = Process.Start(info);
            return p ?? throw new ArgumentException("Cannot start wsl / socat");
        }
    }
}
