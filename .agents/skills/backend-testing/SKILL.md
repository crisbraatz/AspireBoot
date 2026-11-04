---
name: backend-testing
description: Maintain AspireBoot xUnit and NSubstitute unit tests and WebApplicationFactory/Testcontainers integration tests, including isolation, messaging, and coverage.
---

# Backend testing

## Test layout and style

Place tests under `AspireBoot.Tests/Unit/<Layer>/<Feature>` or `Integration/<Layer>/<Feature>` and mirror production slices. Use `<Subject>Tests`, `[Fact]`/`[Theory]`, and descriptive `Should...` method names. Follow four-space C# indentation, explicit types, readable arrange/act/assert grouping, and neighboring assertion conventions.

Use xUnit v3 and NSubstitute. Read a nearby handler test and endpoint integration test. Test observable results and meaningful side effects, covering changed behavior and concrete regression risks.

## Unit tests

- Instantiate Core handlers with interface substitutes. Assert success/results or failure kind/code/field and relevant repository, cache, and event calls.
- Cover validator boundaries, including pagination overflow, missing input, and upload type/size where applicable.
- Verify invalid or denied mutations leave persistence and outgoing events untouched. Preserve dependency failures and cancellation behavior.
- Use `TestContext.Current.CancellationToken` in new async tests where appropriate. The test project suppresses CA2007 so ordinary test awaits can omit `ConfigureAwait(false)`.
- Use a controllable `TimeProvider` where already supported. Avoid timing-sensitive sleeps and test-only production hooks when existing abstractions suffice.

## Container integration tests

Read `Integration/TestFixture.cs`, `TestBase.cs`, `TestCollectionFixture.cs`, `ApiWebApplicationFactory.cs`, and `AntiforgeryDelegatingHandler.cs` before changing fixtures.

The fixture starts real PostgreSQL, RabbitMQ, Redis, and RustFS containers and runs API and Worker hosts. SMTP `IEmailSender` is substituted. Reuse the shared resources, factory override hook, authenticated-client patterns, and IP generator. Keep requested image upgrades aligned with AppHost tags.

Derive ordinary integration tests from `TestBase`. It joins the nonparallel `Integration` collection and resets state before client creation. Reset waits for outgoing and pending inbox messages, clears recorded email calls and Redis, and uses Respawn for application tables while retaining migrations and Data Protection keys. Preserve that order to prevent background work from contaminating the next test.

The default client adds CSRF tokens automatically. Negative CSRF tests must select the factory's appropriate client configuration so the helper cannot mask rejection. Preserve secure-host and forwarded-header conventions from existing endpoint specs.

Select relevant scenarios for the change, including response contracts, invalid input, unauthenticated/wrong-role/wrong-owner access, inactive records, mutation plus audit/outbox effects, dependency failures, rate limits, session revocation, and upload bounds. Use bounded polling for asynchronous outcomes as adjacent tests do. Keep real dependencies for integration assertions and preserve collection isolation.

## Verification

Run from the root with Docker available.

```sh
dotnet test AspireBoot.Tests/AspireBoot.Tests.csproj -c Debug --filter FullyQualifiedName~YourFeature
dotnet test AspireBoot.Tests/AspireBoot.Tests.csproj -c Debug
```

For unit-only execution use `--filter 'FullyQualifiedName~AspireBoot.Tests.Unit'`. CI uses Coverlet with a 90% total line threshold over configured included code. Preserve test-project exclusions and the workflow gate. Report exactly which checks ran and whether Docker-dependent tests remain unverified.
