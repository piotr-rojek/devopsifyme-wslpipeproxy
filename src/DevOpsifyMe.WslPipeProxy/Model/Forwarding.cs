namespace DevOpsifyMe.WslSocketProxy.Model
{
    public class Forwarding
    {
        public string? Distribution { get; set; }

        public string Npipe { get; set; } = null!;

        public string Unix { get; set; } = null!;
    }
}
