# AspireBoot

## About

Aspire Boot gives you a fully functional .NET Aspire template so you can go straight to business code.

It includes out of the box:

- A Web API built with C# / .NET 9.0
  - JWT authentication and authorization with refresh token
  - Integration / unit / mutation tests with XUnit and Stryker frameworks
- A Worker built with C# / .NET 9.0
- A responsive Angular 20 application
- A Postgres 17 database
- A Redis 7 cache + Commander
- A Rabbit 4 broker + Management

## Dependencies to execute the application locally

### Backend:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Podman Desktop](https://podman.io)

### Frontend:

- [Node.js 22](https://www.nodejs.tech/pt-br/download)

## How to

### Execute the application locally?

Execute the command `dotnet dev-certs https --trust` (one time only).

In the .NET IDE, play/debug `AspireBoot.AppHost: https` profile.

It will automatically open Aspire dashboard at `https://localhost:5000` which shows all resources:

- The frontend can be accessed at `https://localhost:4200`
- The Scalar API documentation can be accessed at `https://localhost:5001/scalar`
- The Rabbit Management can be accessed at `http://localhost:15673`
- The Redis Commander can be accessed at `http://localhost:6380`

### Migrate the database?

Execute the command `dotnet tool install -g dotnet-ef` (one time only).

Then, in the repository's root directory, execute the commands:

```bash
export ASPNETCORE_ENVIRONMENT=Migration
dotnet ef -p AspireBoot.Infrastructure -s AspireBoot.Api migrations add MIGRATION_NAME
```

The application is auto migrated in development environment.

## Key projects

### AspireBoot.AppHost

The heart of a .NET Aspire application resides in this project.

The `AppHost.cs` file defines all resources and its details.

If a resource is added/edited/removed, make sure the `appsettings.json` and `appsettings.Development.json` of all projects are adjusted (if needed).

### AspireBoot.ServiceDefaults

This project is responsible to inject everything that is default for a service, like observability and health checks.

If a new API or Worker is created, make sure it references this project and inject its methods so Aspire can do its magic.

### AspireBoot.Api

This project ships out of the box an `AuthController` with five endpoints:

- RefreshToken with Redis cache
- SignIn
- SignOut
- SignUp
- Test (feel free to remove it)
  - An endpoint for authenticated users to ping the worker project

### AspireBoot.Worker

This project enables consumption of messages sent by the API so it processes in the background.

Great for sending emails, process reports or any kind of background job.

Feel free to remove it.

### AspireBoot.Angular

This project is where the frontend resides.

In the project root directory, execute the command:

```bash
ng serve
```

The frontend can be accessed at `https://localhost:4200`.
