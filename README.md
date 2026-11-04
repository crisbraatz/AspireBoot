<p align="center">
  <img src="AspireBoot.Angular/public/assets/brand/logo.png" alt="AspireBoot" width="360">
</p>

# Bring your idea to life with a safer starting point

AspireBoot is a starting template for people building products with AI, including people with little programming experience. Describe your idea, work with your assistant, and grow it into an application you can run on your own virtual private server (VPS).

Getting a screen to work is only part of building a product. Accounts, passwords, private information, file uploads, and background work also need care. AspireBoot brings these foundations together so you can begin with established protections and spend more time shaping your idea. Security still depends on the features you add and how you configure and operate your server.

[Explore the AspireBoot project page](https://crisbraatz.github.io/en-US/projects/aspireboot/index.html).

## AI ready

**Built for Claude, Gemini, Codex, and GitHub Copilot.** Each assistant has a repository entry point that directs it to the same six development skills. Architecture, testing, security, and performance guidance stays in one shared location as you switch assistants.

| Assistant | Entry point |
| --- | --- |
| Codex | [AGENTS.md](AGENTS.md) |
| Claude Code | [CLAUDE.md](CLAUDE.md) |
| Gemini CLI | [GEMINI.md](GEMINI.md) |
| GitHub Copilot | [.github/copilot-instructions.md](.github/copilot-instructions.md) |

Open the repository root in your assistant and enable repository instructions in your client. The entry points route work to [.agents/skills](.agents/skills). Assistant identification uses the active client's context when available. All clients follow the same engineering rules.

## What you can build on

- Account registration, sign-in, email confirmation, password recovery, and email and password changes.
- Profile editing, avatar uploads, a user directory, and administrator-aware access rules.
- Active-session tracking and revocation, including signing out other devices.
- Background email delivery, user auditing, and scheduled cleanup of unused avatars.
- HTTPS routing, health checks, logs, traces, metrics, and automated quality checks.
- English and Portuguese interface messages, appearance preferences, and reusable interface elements.

## Technologies and versions

Versions below reflect the committed SDK, package lockfile, and AppHost container tags.

| Technology | Version | Role |
| --- | --- | --- |
| <img src="https://cdn.simpleicons.org/dotnet/512BD4" width="22" alt=".NET"> .NET | SDK **10.0.403**, target **net10.0**, ASP.NET Core packages **10.0.14** | API, application logic, background services |
| <img src="https://cdn.simpleicons.org/angular/DD0031" width="22" alt="Angular"> Angular | **22.2.4** | Browser application |
| <img src="https://cdn.simpleicons.org/tailwindcss/06B6D4" width="22" alt="Tailwind CSS"> Tailwind CSS | **4.3.4** | Styling |
| <img src="https://cdn.simpleicons.org/postgresql/4169E1" width="22" alt="PostgreSQL"> PostgreSQL | **18.6-alpine** | Application data, Identity, Data Protection keys, durable messages |
| <img src="https://cdn.simpleicons.org/rabbitmq/FF6600" width="22" alt="RabbitMQ"> RabbitMQ | **4.4.0-management-alpine** | Event transport |
| <img src="https://cdn.simpleicons.org/redis/FF4438" width="22" alt="Redis"> Redis | **8.10.3-alpine** | Caching, session tickets, email request limits |
| 🗄️ RustFS | **1.0.3** | S3-compatible object storage |
| <img src="https://cdn.simpleicons.org/caddy/1F88C0" width="22" alt="Caddy"> Caddy | **2.11.7** | HTTPS reverse proxy and production static-file hosting |

## Get ready to develop

Install the following on your development machine.

| Requirement | Purpose |
| --- | --- |
| Git | Clone the repository and track changes |
| .NET SDK **10.0.403** | Match `global.json`, which disables SDK roll-forward |
| Node.js **26.x** and npm **12.2.1** | Match the frontend CI runtime and declared package manager |
| Docker Desktop or a compatible Docker Engine | Run the local infrastructure and container-based integration tests |
| `mkcert` | Create a locally trusted HTTPS certificate |
| An editor and your preferred AI coding assistant | Work with the shared repository instructions |
| An SMTP service with credentials | Deliver registration, recovery, and account notification emails |

Install OpenSSL if you need to generate a production Data Protection certificate. Aspire is optional. The AppHost provides a convenient way to orchestrate the complete local environment, and you can also start the API, Worker, and frontend independently. For UI-only work, Node.js, npm, and the local HTTPS certificate are sufficient. The Aspire CLI is optional for the `dotnet run` workflow below.

### 1. Clone and install dependencies

Clone your copy of the repository, open its root, then run the following commands.

```sh
dotnet restore AspireBoot.slnx
cd AspireBoot.Angular
npm ci
cd ..
```

### 2. Create trusted local certificates

Run from the repository root. The generated files are ignored by Git.

```sh
dotnet dev-certs https --trust
mkcert -install
mkdir -p AspireBoot.AppHost/certs
mkcert -cert-file AspireBoot.AppHost/certs/localhost-cert.pem \
  -key-file AspireBoot.AppHost/certs/localhost-key.pem \
  localhost 127.0.0.1 ::1 api.localhost app.localhost aspire.localhost \
  rabbit.localhost redis.localhost rustfs-api.localhost rustfs-app.localhost
```

The .NET certificate serves the local .NET processes. The `mkcert` certificate is shared by Angular and Caddy. Keep the certificate and private-key filenames above because the existing launch settings reference them.

The development Caddyfile uses `host.docker.internal` to reach processes running on your machine. Docker Desktop provides it. With Linux Docker Engine, configure the equivalent host-gateway mapping for the Caddy container before launching. The local browser workflow uses HTTPS on port 443. Also keep ports 4200, 5000, 5010, 5020, 5100, 5432, 5672, 6379, 9000, 9001, and 15672 available for the configured processes and containers.

### 3. Configure email

The AppHost starts the database, messaging, cache, storage, and proxy containers. Supply an SMTP service separately. Development defaults point to `localhost:587` with placeholder credentials. Configure a local authenticated SMTP server or your provider using these environment variables before starting the AppHost.

```sh
export AspireBoot__Infrastructure__Smtp__Host='your-smtp-host'
export AspireBoot__Infrastructure__Smtp__Port='587'
export AspireBoot__Infrastructure__Smtp__Username='your-smtp-user'
export AspireBoot__Infrastructure__Smtp__Password='your-smtp-password'
export AspireBoot__Infrastructure__Smtp__From='noreply@your-domain.example'
export AspireBoot__Infrastructure__Smtp__StartTls='true'
```

Use the equivalent environment-variable syntax in PowerShell on Windows. Environment variables inherited by the API and Worker override their JSON settings. Store actual credentials outside version control. Production requires STARTTLS. Development permits sign-in before email confirmation, while production requires confirmed email.

### 4. Start the application with Aspire

Start Docker, then launch from the repository root.

```sh
dotnet run --project AspireBoot.AppHost/AspireBoot.AppHost.csproj
```

The AppHost starts services in dependency order and launches Angular using its `start` script. Development enables EF migrations, messaging provisioning, and object-storage bucket creation. Named volumes persist infrastructure data between runs.

| Local service | Address |
| --- | --- |
| Application | [https://app.localhost](https://app.localhost) |
| API | [https://api.localhost/api](https://api.localhost/api) |
| API reference, Development only | [https://api.localhost/scalar](https://api.localhost/scalar) |
| Aspire dashboard | [https://aspire.localhost](https://aspire.localhost) |
| RabbitMQ management | [https://rabbit.localhost](https://rabbit.localhost) |
| Redis Commander | [https://redis.localhost](https://redis.localhost) |
| RustFS console | [https://rustfs-app.localhost](https://rustfs-app.localhost) |
| RustFS public endpoint | [https://rustfs-api.localhost](https://rustfs-api.localhost) |

Use the dashboard login link or token printed by Aspire when authentication is requested. Development infrastructure credentials are `aspireboot` where configured in the AppHost. The browser calls `/api` through the application origin so secure cookies and CSRF protection work together.

### Run the services independently

You can use your editor or separate terminals to launch each application. Supply PostgreSQL, RabbitMQ, Redis, RustFS, and SMTP independently, and match their addresses and credentials to each service's development settings. Start the API first so development migrations and bucket creation complete before the Worker starts consuming events.

Run the API from the repository root in one terminal.

```sh
dotnet run --project AspireBoot.Api/AspireBoot.Api.csproj --launch-profile https
```

Run the Worker in another terminal. Its standalone development configuration needs the Redis connection and the credentials for your RabbitMQ instance. For infrastructure using the template's local defaults, export these values in that terminal before launch.

```sh
export ConnectionStrings__Redis='localhost:6379,abortConnect=false'
export ConnectionStrings__Rabbit='amqp://aspireboot:aspireboot@localhost:5672'
dotnet run --project AspireBoot.Worker/AspireBoot.Worker.csproj --launch-profile https
```

The API listens on `https://localhost:5100` and the Worker exposes health checks on `https://localhost:5200`. Apply the SMTP settings from the email setup to both processes. Use environment variables to override other connection strings and integration options when your local services have different settings.

Start the frontend in another terminal.

```sh
cd AspireBoot.Angular
npm start
```

For full browser-to-API functionality, run Caddy separately with the development Caddyfile, its certificate mounts, and access to the configured infrastructure network. Open `https://app.localhost` and keep the browser API URL at `https://app.localhost/api`. This preserves the application's same-origin cookie and CSRF flow while the three applications run independently of Aspire.

### Run only the frontend

For layout, styling, and public-screen work, you can launch Angular on its own. Create the HTTPS certificate using the earlier certificate step, install the frontend dependencies, and temporarily set `AspireBoot.Angular/public/appsettings.json` to the following value.

```json
{
  "apiUrl": "https://localhost:4200/api"
}
```

```sh
cd AspireBoot.Angular
npm ci
npm start
```

Open [https://localhost:4200](https://localhost:4200). Public pages can render while the API is unavailable. Account actions and authenticated pages require the backend. To work on authenticated screens in isolation, use the existing TestBed specs and service doubles described in the frontend testing skill. Restore the API URL to `https://app.localhost/api` when returning to the complete Caddy-based environment.

### Working with your AI assistant

Describe the feature and its intended access rules. For example, ask your assistant to add a feature using the existing slice architecture, cover its behavior with tests, and review its authorization and dependency failures. Repository instructions select the relevant skills automatically when the client supports instruction loading. You can also ask the assistant to read a specific `SKILL.md` explicitly.

| Shared skill | Responsibility |
| --- | --- |
| [backend-development](.agents/skills/backend-development/SKILL.md) | Endpoints, CQRS, persistence, and Worker consumers |
| [backend-testing](.agents/skills/backend-testing/SKILL.md) | Unit tests and real-container integration tests |
| [backend-security-performance](.agents/skills/backend-security-performance/SKILL.md) | Authorization, vulnerabilities, resilience, telemetry, and data access |
| [frontend-development](.agents/skills/frontend-development/SKILL.md) | Angular features, contracts, styling, and localization |
| [frontend-testing](.agents/skills/frontend-testing/SKILL.md) | Vitest, Angular TestBed, HTTP contracts, and UI behavior |
| [frontend-security-performance](.agents/skills/frontend-security-performance/SKILL.md) | Browser security, session boundaries, rendering, uploads, and bundles |

## Architecture

AspireBoot combines **Vertical Slice Architecture**, **Screaming Architecture**, and **Clean Architecture principles**. Folders describe capabilities such as `Identity/Users/UpdateUser`. Related use cases remain easy to locate across the API, Core, Infrastructure, Worker, and tests. Project boundaries separate HTTP handling, application behavior, integration implementations, and background processing.

**CQRS** separates commands that change state from queries that read it. The project supplies its own `ICommandHandler`, `IQueryHandler`, validation decorators, and `Result`/`Failure` abstractions. Endpoint handlers translate HTTP input into commands or queries and map results back to responses. **Wolverine** handles durable asynchronous messaging and Worker consumers.

### Projects and responsibilities

| Project | Responsibility |
| --- | --- |
| [AspireBoot.AppHost](AspireBoot.AppHost/AspireBoot.AppHost.csproj) | Local orchestration, container versions, persistent volumes, service references, startup dependencies, and Angular launch |
| [AspireBoot.Api](AspireBoot.Api/AspireBoot.Api.csproj) | ASP.NET Core Minimal API endpoints, request/response contracts, cookie authentication, authorization, CSRF, rate limiting, error mapping, and development OpenAPI/Scalar |
| [AspireBoot.Core](AspireBoot.Core/AspireBoot.Core.csproj) | Entities, feature-specific commands, queries, handlers, validators, events, business policies, integration interfaces, and email preparation. It also uses the ASP.NET Core framework reference for Identity-related abstractions |
| [AspireBoot.Infrastructure](AspireBoot.Infrastructure/AspireBoot.Infrastructure.csproj) | EF Core and migrations, Identity stores, repositories, PostgreSQL-backed outbox, RabbitMQ, Redis, RustFS/S3, SMTP, and dependency telemetry |
| [AspireBoot.Worker](AspireBoot.Worker/AspireBoot.Worker.csproj) | Explicitly registered Wolverine consumers, asynchronous email delivery, user audit persistence, scheduled avatar cleanup, and health endpoints |
| [AspireBoot.ServiceDefaults](AspireBoot.ServiceDefaults/AspireBoot.ServiceDefaults.csproj) | Shared OpenTelemetry configuration, health checks, heartbeat metrics, HTTP resilience, and service discovery |
| [AspireBoot.Tests](AspireBoot.Tests/AspireBoot.Tests.csproj) | Backend unit and integration tests, API host fixtures, container lifecycle, isolation, and coverage configuration |
| [AspireBoot.Angular](AspireBoot.Angular) | Standalone Angular application with feature folders, lazy routes, signals, RxJS, Tailwind, runtime configuration, and colocated tests |

### Identity and session security

ASP.NET Core Identity manages passwords, roles, lockout, and account tokens. Passwords require at least 12 characters and four unique characters. Five failed sign-in attempts trigger a 15-minute lockout. Account tokens have a one-hour lifetime.

Authentication uses secure, HTTP-only, `SameSite=Strict` cookies with the `__Host-` prefix. Redis stores session tickets and supports session revocation. Requests that change state require an antiforgery token through `X-CSRF-TOKEN`. API filters, policies, and current-user resolution enforce access rules on the server. Data Protection keys are persisted in PostgreSQL and encrypted with a certificate outside Development.

### Persistence and the outbox pattern

Entity Framework Core uses PostgreSQL through Npgsql. Repositories implement Core interfaces and own persistence details. Account mutations and their outgoing events use the shared DbContext and Wolverine's transactional outbox so a committed change can retain its event for delivery even if the broker is temporarily unavailable.

Wolverine persists message state in the PostgreSQL `messaging` schema, publishes through RabbitMQ with durable outboxes, and consumes through durable inboxes. Routing uses explicit event identifiers, exchanges, queues, and bindings. Failed handling retries after 1, 5, and 30 seconds before moving to Wolverine's error queue. Consumers should account for retries and repeated external side effects.

### Add a database migration

Install the EF Core CLI once, matching the project's EF Core version.

```sh
dotnet tool install --global dotnet-ef --version 10.0.14
```

After changing an entity or its EF mapping, run the following command from the repository root and replace `AddYourFeature` with a descriptive migration name. Start the local infrastructure and provide the API's development configuration so its startup services can initialize.

```sh
dotnet ef migrations add AddYourFeature \
  --project AspireBoot.Infrastructure/AspireBoot.Infrastructure.csproj \
  --startup-project AspireBoot.Api/AspireBoot.Api.csproj \
  --output-dir Migrations \
  -- --environment Development
```

Review the generated migration and model snapshot, then commit them with the feature. Development applies pending migrations when the API starts. Production uses the controlled migration process described in the VPS section.

### Integrations and observability

- **Redis** provides expiring caches, session-ticket storage, and email-request cooldown and window limits.
- **RustFS** stores avatars using the S3 API. The server validates supported JPEG, PNG, and WebP signatures and enforces a 4 MiB limit. Signed URLs provide time-limited access. The browser can convert HEIC/HEIF images before upload.
- **SMTP** uses MailKit. Core prepares email content and the Worker sends it asynchronously. Email-related failures participate in the messaging error policies.
- **OpenTelemetry** captures ASP.NET Core, HTTP client, runtime, PostgreSQL, Wolverine, and custom dependency signals. Dependency wrappers record duration, failures, and cancellation using shared observability helpers.
- **Health endpoints** expose `/alive` for liveness and `/health` for readiness checks. Caddy blocks public access to both. Aspire visualizes local telemetry. Production can export logs, metrics, and traces to an OTLP collector through `OTEL_EXPORTER_OTLP_*` configuration.

### Frontend conventions

The Angular application uses standalone components, `OnPush` change detection, signals for view state, and RxJS for HTTP and route-driven behavior. Lazy routes keep feature loading separate. Shared services manage runtime configuration, the current user and session, localization, and appearance.

`public/appsettings.json` supplies the browser API URL at startup. The loader requires the API to share the application's origin. The authentication interceptor restricts credentials and antiforgery headers to matching API requests. Centralized problem handling, loading indicators, notices, and pagination support consistent feature screens. Tailwind CSS and shared styles provide the visual foundation.

## Tests and quality checks

Run backend commands from the repository root.

```sh
dotnet build AspireBoot.slnx -c Debug
dotnet test AspireBoot.Tests/AspireBoot.Tests.csproj -c Debug
```

Unit tests use **xUnit v3** and **NSubstitute**. Integration tests use **WebApplicationFactory**, **Testcontainers**, and real PostgreSQL, RabbitMQ, Redis, and RustFS containers. SMTP delivery is substituted in the integration fixture. **Respawn** resets application tables, Redis is cleared, and the fixture waits for durable messaging work before resetting state. Docker must be running for the complete suite.

Run frontend checks inside `AspireBoot.Angular`.

```sh
npm run lint
npm run build
npm run test:coverage
```

Frontend tests use **Vitest**, Angular **TestBed**, and **HttpTestingController**, with specs next to implementation files. CI requires 90% backend line coverage over the configured coverage scope and 90% frontend lines, statements, functions, and branches. Backend coverage excludes configured infrastructure and generated-code categories. Production Angular builds enforce initial-bundle and component-style budgets.

The [CI workflow](.github/workflows/ci.yml) restores, audits, builds, tests, and uploads backend coverage. It also installs, audits, lints, builds, and tests the frontend. C# analyzers and warnings-as-errors are enabled centrally. Preserve `.editorconfig`, Angular ESLint, strict TypeScript, and Prettier conventions when extending the template.

## Taking your product to a VPS

The repository includes a production [Caddyfile](AspireBoot.AppHost/Caddyfile). Plan the server packaging and deployment around its expected service names and paths.

1. Build and publish the API and Worker with .NET 10. Build Angular with `npm ci` and `npm run build`. Package the services and their runtime dependencies for your VPS. Add deployment manifests or a container composition for your hosting setup.
2. Point your application, API, and object-storage DNS names to the server. Set Caddy's `APP_HOST`, `API_HOST`, and `OBJECT_STORAGE_HOST`. It expects the API at `api:8080`, RustFS at `rustfs:9000`, and Angular's built browser files mounted at `/srv`. Persist Caddy's certificate data.
3. Run the API and Worker with the `Production` environment and supply their full configuration. Base `appsettings.json` files contain required empty values. Use environment variables or mounted secrets for connection strings and credentials. Set the browser's published `appsettings.json` API URL to `https://<your-app-host>/api`.
4. Configure API `AllowedHosts`, `AspireBoot__Api__Frontend__Origin`, and `AspireBoot__Api__ReverseProxy__IpNetwork` to match your real hosts and trusted proxy network. Keep the API and infrastructure on a private network. Expose the intended public Caddy ports, normally 80 and 443.
5. Supply production PostgreSQL, RabbitMQ, Redis, RustFS, and SMTP settings. Define the messaging topology, bindings, event routes, concurrency, retention, and rate limits using the existing option classes. Use separate production credentials. Provision the RustFS bucket before startup because automatic bucket creation is restricted to Development.
6. Mount a PKCS#12 Data Protection certificate at the configured `AspireBoot__Infrastructure__DataProtection__CertificatePath` and supply its password. API and Worker must share the application name, database key ring, and compatible certificate configuration. Configure SMTP STARTTLS and a verified sender so users can confirm their email and recover access.
7. Apply the EF migrations and provision Wolverine's database schema and RabbitMQ topology as a controlled deployment step. `ApplyMigrationsOnStartup` and messaging `AutoProvision` default to false in base configuration. Choose a migration/provisioning process explicitly before starting production services.
8. Configure an OTLP destination, private health monitoring, persistent volumes, database and object-storage backups, and recovery procedures. Verify sign-up, email confirmation, login, account recovery, uploads, and background delivery through the public HTTPS domain.

The development Caddyfile, local credentials, development certificate, and infrastructure management URLs serve the local workflow. Use the production Caddyfile and your server's actual configuration for the VPS.

## License

AspireBoot is available under the [MIT License](LICENSE).
