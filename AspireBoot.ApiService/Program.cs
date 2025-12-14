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
        .AddPolicy("AllowAngular", y => y
            .WithOrigins(AppSettings.AngularOrigin)
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
    .UseCors("AllowAngular")
    .UseAuthentication()
    .UseAuthorization();
webApplication.MapControllers();
webApplication.Use(async (context, next) =>
{
    string host = context.Request.Host.Host;

    if (host.Equals(AppSettings.AngularHost, StringComparison.OrdinalIgnoreCase) ||
        host.Equals(AppSettings.AspireHost, StringComparison.OrdinalIgnoreCase) ||
        context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
    {
        await next().ConfigureAwait(false);

        return;
    }

    context.Response.StatusCode = 403;
    await context.Response.WriteAsync("Forbidden").ConfigureAwait(false);
});

await webApplication.RunAsync().ConfigureAwait(false);
