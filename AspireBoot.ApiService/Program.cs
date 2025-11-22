using AspireBoot.ApiService;
using AspireBoot.ApiService.Middlewares;
using AspireBoot.Domain;
using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using Scalar.AspNetCore;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
webApplicationBuilder.AddServiceDefaults(nameof(ActivitySourceName.ApiService));
webApplicationBuilder.Services
    .AddProblemDetails()
    .AddOpenApi()
    .AddCors(x => x
        .AddPolicy("AllowAngularAndAspire", y => y
            .WithOrigins(AppSettings.AngularOrigin, AppSettings.AspireOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

AppSettings.Get(webApplicationBuilder.Configuration);

webApplicationBuilder.Services
    .AddInfrastructure(webApplicationBuilder.Configuration)
    .AddApplication();

WebApplication webApplication = webApplicationBuilder.Build();
webApplication
    .UseExceptionHandler()
    .UseMiddleware<ExceptionMiddleware>()
    .UseMiddleware<UnitOfWorkMiddleware>();

if (webApplication.Environment.IsDevelopment())
{
    webApplication.MapOpenApi();
    webApplication.MapScalarApiReference();
}

webApplication.MapDefaultEndpoints();
webApplication
    .UseHttpsRedirection()
    .UseRouting()
    .UseCors("AllowAngularAndAspire")
    .UseAuthentication()
    .UseAuthorization();
webApplication.MapControllers();

await webApplication.RunAsync().ConfigureAwait(false);
