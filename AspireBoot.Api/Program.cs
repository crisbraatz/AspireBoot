using AspireBoot.Api;
using AspireBoot.Core;
using AspireBoot.Infrastructure;
using AspireBoot.Infrastructure.Integrations.Rabbit;
using AspireBoot.ServiceDefaults;
using JasperFx;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
webApplicationBuilder.AddServiceDefaults();
webApplicationBuilder.Services.AddCore();
bool isDevelopment = webApplicationBuilder.Environment.IsDevelopment();
webApplicationBuilder.Services.AddApiInfrastructure(webApplicationBuilder.Configuration, isDevelopment);
webApplicationBuilder.Services.AddApi(webApplicationBuilder.Configuration, isDevelopment);
webApplicationBuilder.AddRabbit(typeof(Program).Assembly);

WebApplication webApplication = webApplicationBuilder.Build();

await webApplication.UseApiAsync().ConfigureAwait(false);
await webApplication.RunJasperFxCommands(args).ConfigureAwait(false);

namespace AspireBoot.Api
{
    public class Program
    {
        protected Program()
        {
        }
    }
}
