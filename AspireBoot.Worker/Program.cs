using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using AspireBoot.Worker;
using JasperFx;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
webApplicationBuilder.AddServiceDefaults();
webApplicationBuilder.Services.AddWorkerInfrastructure(
    webApplicationBuilder.Configuration, webApplicationBuilder.Environment.IsDevelopment());
webApplicationBuilder.AddWorker();

WebApplication webApplication = webApplicationBuilder.Build();
webApplication.MapDefaultEndpoints();

await webApplication.RunJasperFxCommands(args).ConfigureAwait(false);
