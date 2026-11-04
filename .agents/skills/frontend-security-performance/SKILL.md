---
name: frontend-security-performance
description: Review or improve AspireBoot credential/CSRF boundaries, redirects, uploads, browser rendering, dependency vulnerabilities, and Angular runtime or bundle performance.
---

# Frontend security and performance

## Trace the browser flow

Read `app.config.ts`, `AppConfiguration`, authentication interceptor, antiforgery service, current-session/user services, and affected routes/pages/API services. For hosting, headers, or caching also read both Caddyfiles and backend endpoint configuration.

## Trust boundaries

- Preserve runtime same-origin API validation. Use existing parsed-URL helpers and check origin plus API path boundaries. Simple string-prefix matching can include another host/path.
- Keep cookies and CSRF headers restricted to matching API calls. Unsafe methods obtain a token for the current principal. The antiforgery service uses `HttpBackend` to avoid interceptor recursion. Preserve fresh-token behavior across login.
- Keep the invalid-credentials exception in 401 session expiry. Preserve session/user invalidation after authentication/account changes. Keep passwords, session secrets, and recovery tokens out of browser persistence.
- Use the existing return-URL validator for navigation. Avoid leaking account tokens through logs, analytics, or external redirects.
- Use Angular's normal bindings and sanitization. Review requested HTML rendering, direct DOM mutation, executable URLs, or sanitizer bypass at the actual input boundary. Preserve production CSP, Trusted Types, and external startup scripts.
- Keep backend authorization for protected operations. UI guards and role indicators must reflect server rules.

## Uploads and personal data

Reuse avatar validation, conversion, and form helpers. Preserve the 4 MiB limit, supported image output, and backend contract. Keep server signature validation paired with client checks.

When changing conversion, assess HEIC/HEIF decoding cost, image dimensions, failures, and temporary object-URL cleanup. Preserve multipart field names. Exclude personal data and signed URLs from telemetry/error messages.

## Runtime and delivery performance

- Preserve standalone lazy routes, OnPush, signals, and lifecycle-aware RxJS. Avoid repeated HTTP calls or expensive template computations and unmanaged subscriptions.
- Keep pagination bounded and use existing state services before introducing another store.
- Inspect production bundle output for dependency changes. Preserve feature-demand loading of expensive conversion work and the `angular.json` initial-bundle/style budgets.
- Preserve `appsettings.json` no-store loading and production response headers, hashed assets, and Caddy SPA fallback behavior.
- Handle failed/stale requests without permanently busy UI state. Preserve accessible loading and error feedback.

## Dependencies and telemetry

For audits or dependency changes, inspect the lockfile and run `npm audit --audit-level=high`. Review transitive effects before upgrades or overrides. Preserve compatibility with the declared npm version and CI runtime.

Backend telemetry already covers API/dependency activity. If browser telemetry is requested, define event scope, redaction, sampling, and low-cardinality fields. Preserve privacy in authentication and recovery flows.

Read `frontend-testing` for regression coverage. Run lint, production build, and relevant specs. Report measured performance/bundle differences separately from hypotheses and keep audit-only work within its read-only scope.
