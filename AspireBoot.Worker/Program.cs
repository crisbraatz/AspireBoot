using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using AspireBoot.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddInfrastructureForWorker(builder.Configuration);
builder.Services.AddWorker();

var host = builder.Build();
host.Run();