using DevOpsifyMe.WslSocketProxy;
using DevOpsifyMe.WslSocketProxy.Model;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddOptions<MyOptions>().BindConfiguration("");
        services.AddTransient<PipeServer>();
        services.AddTransient<PipeServersManager>();
        services.AddTransient<SocatProcessFactory>();
    })
    .Build();

host.Run();
