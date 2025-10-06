using AspireBoot.Domain;
using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using AspireBoot.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults("worker");

AppSettings.Get(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorker();

var host = builder.Build();
host.Run();