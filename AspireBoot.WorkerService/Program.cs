using AspireBoot.Domain;
using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using AspireBoot.WorkerService;

HostApplicationBuilder hostApplicationBuilder = Host.CreateApplicationBuilder(args);
hostApplicationBuilder.AddServiceDefaults(nameof(ActivitySourceName.WorkerService));

AppSettings.Get(hostApplicationBuilder.Configuration);

hostApplicationBuilder.Services
    .AddInfrastructure(hostApplicationBuilder.Configuration)
    .AddWorker();

IHost host = hostApplicationBuilder.Build();
await host.RunAsync().ConfigureAwait(false);
