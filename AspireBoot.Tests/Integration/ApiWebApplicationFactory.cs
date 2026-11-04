using System.Net;
using AspireBoot.Core.Cqrs;
using AspireBoot.Core.Integrations.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ApiProgram = AspireBoot.Api.Program;

namespace AspireBoot.Tests.Integration;

public class ApiWebApplicationFactory(
    IReadOnlyDictionary<string, string?> configuration,
    Action<IServiceCollection>? action = null)
    : WebApplicationFactory<ApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(serviceCollection =>
        {
            IEmailSender emailSender = Substitute.For<IEmailSender>();
            emailSender.SendAsync(Arg.Any<EmailDetails>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Result.Success()));
            serviceCollection.Replace(ServiceDescriptor.Singleton(emailSender));
            action?.Invoke(serviceCollection);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configurationBuilder =>
            configurationBuilder.AddInMemoryCollection(configuration));
        IHost host = base.CreateHost(builder);
        host.Services.GetRequiredService<IHost>().Start();

        return host;
    }

    public HttpClient CreateHttpClient(IPAddress ipAddress, bool includeAntiforgery = true)
    {
        HttpClient httpClient = includeAntiforgery
            ? CreateDefaultClient(
                new Uri("https://api.tests"), new AntiforgeryDelegatingHandler(), new CookieContainerHandler())
            : CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.tests") });
        httpClient.DefaultRequestHeaders.Host = "api.tests";
        httpClient.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress.ToString());
        httpClient.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        return httpClient;
    }
}
