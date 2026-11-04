---
name: backend-security-performance
description: Review or improve AspireBoot authorization, vulnerabilities, transactional messaging, dependency resilience, telemetry, and database performance during audits or sensitive backend changes.
---

# Backend security and performance

## Trace the affected flow

Read the endpoint, route group, filters, Core handler, repository/integration, options, and tests. For proxy/authentication changes also read `ApiDependencyInjection`, both Identity registration files, `ApiOptions`, and the Caddyfiles. Apply checks relevant to the request.

## Access and request integrity

- Preserve secure HTTP-only `__Host-` cookies, `SameSite=Strict`, Redis session tickets, security-stamp validation, and revocation behavior.
- Preserve antiforgery validation and `X-CSRF-TOKEN` for state-changing requests. Check login/logout transitions because tokens belong to the current principal.
- Resolve the actor from authenticated claims. Enforce ownership, administrator policies, inactive-user visibility, and session access on the server. Distinguish Identity IDs from domain IDs.
- Keep Identity password, token, lockout, unique-email, and production confirmed-email policies. Preserve recovery-response privacy and avoid email enumeration.
- Preserve route authentication, explicit anonymous exceptions, rate limits, body limits, multipart validation, and bounded pagination. Avatar validation checks actual image signatures and a 4 MiB limit.
- Keep explicit `AllowedHosts`, frontend origins, and trusted proxy networks. Retain forwarded-header trust boundaries, production HSTS, response security headers, and private health routing.

## State and asynchronous consistency

Reuse EF parameterized queries and repository policies. Keep state changes and outbox events in one unit of work. Review transaction/flush ordering, early failure, retries, and concurrency conflicts. Reuse outbox publishing for persisted mutations.

Read `RabbitDependencyInjection` for durable inbox/outbox, publisher confirmations, cooldown retries, and error handling. Event identifiers and routing must remain compatible with persisted messages. Make consumer database effects retry-safe and assess duplicate external SMTP/storage effects for the affected feature.

Review cache invalidation, TTLs, session indexes, and avatar cleanup ordering when modifying state. Preserve bounded cleanup and minimum-age protection for objects that may still be attached to a user.

## Resilience and telemetry

Reuse `Infrastructure/Observability`, including `DependencyTrace`, `DependencyLogs`, `DependencyFailures`, and `DependencyUnavailableException`. Follow existing success/failure/cancellation mapping and low-cardinality tags. Exclude passwords, cookies, account tokens, connection strings, email bodies, and signed URLs from logs/traces.

Propagate cancellation and preserve meaningful dependency-unavailable responses. Retry operations according to their side effects. ServiceDefaults disables generic HTTP retries for unsafe methods. Preserve health checks and OTLP behavior when changing startup or clients.

## Performance and production configuration

- Use no-tracking reads, deterministic indexed ordering, projections, bounded page sizes, and batched role lookups. Inspect queries/indexes before adding abstractions or schema changes.
- Avoid blocking waits, per-request connection creation, N+1 access, unbounded uploads, and uncontrolled consumer concurrency. Reuse registered client lifetimes.
- Keep secrets outside tracked JSON. Production requires a Data Protection certificate and SMTP STARTTLS. Automatic RustFS bucket creation is restricted to Development. API and Worker share the persisted Data Protection key ring.
- For dependency audits or changes, inspect central package versions and use CI's NuGet restore audit and transitive vulnerability listing. Preserve compatibility and report actual findings.

Use `backend-testing` for relevant boundary regressions. For audits, report affected paths, concrete impact, and focused fixes while honoring read-only scope. Separate measured performance results from hypotheses and state unexecuted load/production checks.
