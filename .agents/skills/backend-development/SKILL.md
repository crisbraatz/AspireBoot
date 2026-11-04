---
name: backend-development
description: Extend AspireBoot backend slices with Minimal API endpoints, CQRS handlers, repositories, EF Core persistence, and Wolverine consumers.
---

# Backend development

## Read the complete slice

Read the closest feature across Api, Core, Infrastructure, Worker, and Tests. Use `Identity/Users/UpdateUser` for mutations and `Identity/Users/ListUsers` for queries. Read `CoreDependencyInjection.cs`, `ApiEndpointMapper.cs`, and the relevant repository before implementing.

Preserve capability-oriented paths and matching namespaces across layers. Api owns HTTP contracts. Core owns application behavior, policies, entities, and integration interfaces. Infrastructure implements those interfaces. Worker owns background consumers. ServiceDefaults owns shared telemetry and health behavior. Core already has an ASP.NET Core framework reference for Identity-related abstractions.

## Implement the use case

1. Define the command/query and result beside the Core use case using existing `ICommand`, `IQuery`, and handler interfaces. Use stable dotted `Failure` codes, appropriate failure kinds, and matching field names through `Result`/`Result<T>`.
2. Add input validation through `ICommandValidator` or `IQueryValidator`. Register the concrete handler, validators, and `ValidatorCommandHandler`/`ValidatorQueryHandler` decorator in `CoreDependencyInjection` as adjacent validated operations do. The endpoint must resolve the decorated interface.
3. Implement persistence behind Core interfaces. Preserve inactive-record filtering, no-tracking read paths, bounded pagination, deterministic ordering, and cancellation. Add EF mappings beside their resource and generate a migration plus snapshot update for requested schema changes.
4. Put Api request/response records beside the endpoint. Follow existing mapping methods such as `ToCommandAsync`. Resolve the authenticated actor through `GetIdentityUserId`. Distinguish Identity string IDs from domain user GUIDs.
5. Add a static `<Action>Endpoint`, public `Map<Action>`, and private `HandleAsync`. Match adjacent binding attributes, result projections, `ToProblemHttpResult`, status metadata, content types, request limits, rate policy, and `WithName` conventions. Register it in `ApiEndpointMapper` and preserve group filters and authorization.

## Events and consumers

Use Core `IEvent`/`AbstractEvent` and `EventTypeAttribute` conventions. Event identifiers are persistent contracts. Reuse `IEventRepository` and the DbContext outbox. Preserve the transaction containing both state changes and outgoing events.

Read `RabbitDependencyInjection` and `RabbitOptions`. Each concrete event needs `EventRoutes` configuration. Add exchanges, queues, and bindings as needed in API and Worker configuration and describe the required production settings.

Conventional Wolverine discovery is disabled. Explicitly register consumers in `WorkerDependencyInjection` following existing discovery calls. Use `[WolverineHandler]`, `ConsumeAsync`, and `[Transactional(typeof(AspireBootDbContext))]` where database consumer work requires it. Account for retries and external side effects. Reuse email preparations and `SendRequestedEmailCommand` for email workflows.

## Code conventions

- Use four-space C# indentation, file-scoped namespaces, Allman blocks, explicit local types, PascalCase types/methods/properties, camelCase parameters, and descriptive dependency names. Project/configuration files use two spaces.
- Match primary constructors and nearby record/class forms. Private fields use `_camelCase`, private/internal static fields use `s_camelCase`, and constants use PascalCase. Follow `.editorconfig` when nearby files differ.
- Keep usings outside namespaces with configured System-first ordering. Match adjacent member order, typically public entry/mapping methods followed by private implementation helpers.
- Pass cancellation tokens through async work and use `ConfigureAwait(false)` in production backend code. Preserve parameter defaults, argument order, and meaningful named arguments.
- Reuse shared primitives, options validation, and integration helpers. Keep expected domain failures distinct from dependency outages and unexpected exceptions.

Read `backend-testing` for associated tests and `backend-security-performance` for sensitive changes. Build and test through normal entry points and report actual outcomes and unavailable prerequisites.
