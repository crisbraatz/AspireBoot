# AspireBoot

## About

This is an Aspire solution that includes:

- A Web API built with C# / .NET 9.0
- An Angular 19 application
- A Postgres 17 database
- A Redis 7 cache
- A Rabbit 4 broker

## Dependencies to execute the application locally

Backend:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Podman Desktop](https://podman.io)

Frontend:

- [Node.js 22](https://www.nodejs.tech/pt-br/download)

## How to

### Execute the application locally?

Execute the command `dotnet dev-certs https --trust` (one time only).

In the .NET IDE, play/debug `AspireBoot.AppHost: https` profile.

It will automatically open Aspire dashboard at `https://localhost:17180` which shows all resources:

- The frontend can be accessed at `https://localhost:4200`
- The API documentation can be accessed at `https://localhost:5001`

### Migrate the database?

Execute the command `dotnet tool install -g dotnet-ef` (one time only).

Then, in the repository's root directory, execute the commands:

```bash
export ASPNETCORE_ENVIRONMENT=Migration
dotnet ef -p AspireBoot.Infrastructure -s AspireBoot.Api migrations add MIGRATION_NAME
```
