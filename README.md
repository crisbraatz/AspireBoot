# AspireBoot

## About

Aspire Boot is a fully functional .NET Aspire template designed to help you jump straight into business logic.

It comes pre-configured with everything you need to build scalable, observable and modern applications.

Included out of the box:

- Web API and Worker built with C# / .NET 9.0
    - JWT authentication and authorization with refresh tokens
    - Integration, unit and mutation tests using XUnit and Stryker
- Responsive Angular 20 frontend
- Postgres 17 database
- Rabbit 4 broker with Management UI
- Redis 7 cache with Redis Commander

## Prerequisites

### Backend:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Podman Desktop](https://podman.io)

### Frontend:

- [Node.js 22](https://nodejs.org/en/download)

## Running the application locally

Trust HTTPS certificates by executing the command `dotnet dev-certs https --trust` (one-time setup).

Set up the frontend by executing the commands (one-time setup):

```bash
mkdir ssl
mkcert -key-file ssl/key.pem -cert-file ssl/cert.pem localhost 127.0.0.1 ::1;
npm install
```

In the .NET IDE, run/debug the `AspireBoot.AppHost` project using the `https` profile.

The Aspire dashboard will open at `https://localhost:5000`, showing all resources:

- Frontend `https://localhost:4200`
- Scalar API documentation `https://localhost:5100/scalar`
- Rabbit Management UI `http://localhost:15673`
- Redis Commander `http://localhost:6380`

### Migrating the database

Install EF Core CLI by executing the command `dotnet tool install -g dotnet-ef` (one-time setup).

Run migrations by executing the commands:

```bash
export ASPNETCORE_ENVIRONMENT=Migration
dotnet ef -p AspireBoot.Infrastructure -s AspireBoot.Api migrations add MIGRATION_NAME
```

> Note: The application auto-applies migrations in development mode.

## Projects breakdown

### AspireBoot.AppHost

The central hub of any .NET Aspire application.

The `AppHost.cs` file defines all resources and their configurations.

Keep `appsettings.json` and `appsettings.Development.json` files in sync across projects when modifying resources.

### AspireBoot.ServiceDefaults

Injects default behaviors like observability, health checks and service discovery.

Ensure new APIs or Workers reference this project to benefit from Aspire's features.

### AspireBoot.Api

Includes a ready-to-use `AuthController` with endpoints:

- SignIn, SignOut and SignUp
- RefreshToken with Redis cache
- Test endpoint to ping the worker (can be removed)

### AspireBoot.Worker

Handles background jobs triggered by the API.

Ideal for tasks like sending emails, processing reports, etc.

### AspireBoot.Angular

Responsive frontend.

Access it at `https://localhost:4200` by executing the command `ng serve`.
