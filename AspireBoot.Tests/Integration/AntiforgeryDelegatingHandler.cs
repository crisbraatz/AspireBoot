using System.Text.Json;

namespace AspireBoot.Tests.Integration;

public class AntiforgeryDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Get ||
            request.Method == HttpMethod.Head ||
            request.Method == HttpMethod.Options)
            return await base.SendAsync(request, cancellationToken);

        using HttpRequestMessage csrf = new(HttpMethod.Get, new Uri(request.RequestUri!, "/api/auth/csrf"));

        foreach (string header in new[] { "Host", "X-Forwarded-For", "X-Forwarded-Proto" })
            if (request.Headers.TryGetValues(header, out IEnumerable<string>? values))
                csrf.Headers.TryAddWithoutValidation(header, values);

        using HttpResponseMessage response = await base.SendAsync(csrf, cancellationToken);

        response.EnsureSuccessStatusCode();

        using JsonDocument token = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token.RootElement.GetProperty("token").GetString());

        return await base.SendAsync(request, cancellationToken);
    }
}
