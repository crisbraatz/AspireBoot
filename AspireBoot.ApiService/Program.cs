using AspireBoot.ApiService;
using AspireBoot.ApiService.Middlewares;
using AspireBoot.Domain;
using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using Microsoft.AspNetCore.HttpOverrides;
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
    .UseMiddleware<ExceptionMiddleware>();
webApplication.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (webApplication.Environment.IsDevelopment())
{
    webApplication.MapOpenApi();
    webApplication.MapScalarApiReference();
}

webApplication.MapDefaultEndpoints();
webApplication
    .UseRouting()
    .UseCors("AllowAngular")
    .Use(async (context, next) =>
    {
        if (context.Request.Host.Host.Equals(AppSettings.AngularHost, StringComparison.OrdinalIgnoreCase) ||
            context.Request.Host.Host.Equals(AppSettings.AspireHost, StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next().ConfigureAwait(false);

            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Forbidden").ConfigureAwait(false);
    })
    .UseAuthentication()
    .UseAuthorization()
    .UseMiddleware<UnitOfWorkMiddleware>();
webApplication.MapControllers();

await webApplication.RunAsync().ConfigureAwait(false);
