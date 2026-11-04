---
name: frontend-testing
description: Maintain AspireBoot Vitest and Angular TestBed specs for API contracts, standalone pages, guards, interceptors, signals, and frontend coverage.
---

# Frontend testing

## Runner and layout

Colocate `<subject>.spec.ts` files beside implementation. Use Angular's `@angular/build:unit-test` runner with Vitest/jsdom. Read neighboring specs and `angular.json`. Follow `describe`, `beforeEach`, `afterEach`, behavior-oriented `it('should ...')` names, two-space indentation, and single quotes.

Use TestBed for DI-dependent behavior and direct calls for pure policies, configuration/URL validation, and conversion helpers. Keep setup proportional to the changed behavior.

## HTTP contracts

Use `identity/users/users-api.spec.ts` as the reference. Register `provideHttpClient()` before `provideHttpClientTesting()`, inject `HttpTestingController`, and call `http.verify()` in `afterEach`.

Assert URLs, encoded IDs, verbs, query parameters, body shapes, and results. Inspect upload `FormData` and verify the browser supplies multipart content type. Flush relevant success, validation, authorization, and dependency failures. Keep doubles aligned with backend records.

API-service specs may stub `AppConfiguration`. Loader specs should exercise actual same-origin validation and malformed/rejected URLs so a permissive stub cannot hide a security boundary.

## Pages and state

- Import standalone components in TestBed and provide service doubles following existing specs. Test visible output, user actions, request payloads, and route changes.
- Follow nearby change-detection and stability patterns. Avoid arbitrary sleeps and unnecessary fake timers.
- Cover affected loading/error/empty/populated states, retries, pagination, malformed route parameters, cancelled/stale requests, and denied operations.
- For forms, check valid submission, boundary validation, duplicate-submission prevention, server error feedback, and recovery.
- For identity changes, cover session initialization, authentication transitions, expiry, and current-user refresh/invalidation.

## Browser boundaries

Use existing interceptor, antiforgery, return-URL, and guard specs. Verify credential/CSRF attachment only to matching API requests, fresh tokens for unsafe requests, and the invalid-credentials exception in 401 expiration behavior. Use backend tests for server access enforcement.

For avatars cover type/size boundaries, HEIC/HEIF conversion failures, temporary object-URL cleanup, and submitted output. Mock browser APIs narrowly and restore them after tests.

## Verification

Run inside `AspireBoot.Angular`.

```sh
npm run lint
npm run build
npm run test:coverage
```

Use `npm test -- --watch=false` for a nonwatch run. Preserve the 90% lines/statements/functions/branches thresholds and production bundle/style budgets. Report actual results and missing runtime prerequisites. Keep gates and feature coverage intact.
