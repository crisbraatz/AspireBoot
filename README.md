# AspireBoot

AspireBoot is a starter template for building modern .NET Aspire applications without spending time on initial setup.

It gives you a working full-stack foundation so you can start with business logic sooner.

## What You Get

- .NET 10 Web API
- .NET 10 Worker Service
- Angular 21 frontend
- PostgreSQL 18
- RabbitMQ 4 with Management UI
- Redis 8 with Redis Commander
- Caddy 2 for HTTPS and reverse proxy
- JWT authentication with refresh tokens
- Unit and integration tests with xUnit
- OpenSpec-ready structure

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24](https://nodejs.org/en/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Podman Desktop](https://podman.io)
- [`mkcert`](https://github.com/FiloSottile/mkcert)

## Run Locally

1. Trust the local HTTPS certificate:

   ```bash
   dotnet dev-certs https --trust
   ```

2. Install frontend dependencies:

   ```bash
   cd AspireBoot.Angular
   npm install
   ```

3. Create local certificates for the app domains:

   ```bash
   cd ../AspireBoot.AppHost
   mkcert -key-file localhost-key.pem -cert-file localhost-cert.pem \
     app.localhost api.localhost rabbit.localhost redis.localhost
   ```

4. Run or debug the `AspireBoot.AppHost` project with the `https` profile from your .NET IDE.

## Local URLs

When the app starts, the Aspire dashboard opens at:

- `https://localhost:5000`

You can then access:

- Frontend: `https://app.localhost:1443`
- API docs (Scalar): `https://api.localhost:1443/scalar/`
- RabbitMQ Management UI: `https://rabbit.localhost:1443`
- Redis Commander: `https://redis.localhost:1443`

## Database Migrations

Install the EF Core CLI once:

```bash
dotnet tool install -g dotnet-ef
```

Create a migration with:

```bash
export ASPNETCORE_ENVIRONMENT=Migration
dotnet ef -p AspireBoot.Infrastructure -s AspireBoot.ApiService migrations add MIGRATION_NAME
```

In development, migrations are applied automatically when the app starts.

## API Endpoints Included

- `POST /api/sessions` signs in
- `DELETE /api/sessions` signs out
- `POST /api/sessions/refresh` refreshes the access token using the refresh-token cookie
- `POST /api/users` signs up
- `GET /api/users` lists users

## Project Structure

### AspireBoot.AppHost

The entry point for the distributed app.

It defines infrastructure, service wiring, ports, and startup order.

### AspireBoot.ServiceDefaults

Shared Aspire defaults such as observability, health checks, and service discovery.

### AspireBoot.ApiService

The main HTTP API with authentication and user endpoints.

### AspireBoot.WorkerService

Background processing for asynchronous or long-running tasks.

### AspireBoot.Infrastructure

Shared infrastructure code for PostgreSQL, RabbitMQ, and Redis.

### AspireBoot.Angular

The frontend application.

For frontend-only development, you can also run:

```bash
cd AspireBoot.Angular
ng serve
```

Then open:

- `http://localhost:4200`
