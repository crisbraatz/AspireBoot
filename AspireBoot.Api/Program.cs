using AspireBoot.Api;
using AspireBoot.Api.Middlewares;
using AspireBoot.Domain;
using AspireBoot.Infrastructure;
using AspireBoot.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularAndAspire", policy =>
    {
        policy.WithOrigins(AppSettings.AngularOrigin, AppSettings.AspireOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

AppSettings.Get(builder.Configuration);

builder.Services.AddInfrastructureForApi(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();
app.UseExceptionHandler();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<UnitOfWorkMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAngularAndAspire");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();