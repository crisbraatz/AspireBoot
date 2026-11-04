---
name: frontend-development
description: Extend AspireBoot Angular features with standalone OnPush components, signals, RxJS, typed API services, runtime configuration, Tailwind, and shared localization.
---

# Frontend development

## Read the existing feature

Read `src/app/app.config.ts`, affected routes, the nearest page/service/contracts, and specs. Use `identity/users/list-users` for route-driven lists and `identity/users/update-user` for forms/uploads. Confirm the backend endpoint and records before defining browser contracts.

Keep `src/app/<capability>/<resource>/<action>` folders. Put request/response interfaces beside the use case, resource API services at the resource level, and reused general primitives in existing `shared` folders. Preserve kebab-case files, PascalCase classes/interfaces, `.page.ts`, `.page.html`, and `.spec.ts` conventions.

## Feature implementation

1. Define typed contracts matching API field names, nullability, pagination, and ID semantics. Distinguish Identity string IDs from domain user IDs. Check statuses and ProblemDetails.
2. Extend the existing injectable resource API service using `HttpClient` and `AppConfiguration.value.apiUrl`. Encode path IDs and use query parameters for paging. Use `FormData` for uploads and let the browser select its multipart content type.
3. Use standalone components with explicit imports, `ChangeDetectionStrategy.OnPush`, `inject`, and signals for view state. Use protected template members and private implementation state. Follow the adjacent form and validation-helper pattern.
4. Compose route/request behavior with RxJS. Follow `switchMap`, `catchError`, `finalize`, and `takeUntilDestroyed` patterns for stale-read cancellation, visible errors, and cleanup. Prevent duplicate writes using the existing submission-state pattern.
5. Add lazy `loadComponent`/`loadChildren` routes and existing authentication/profile/guest guards. Preserve pagination query parameters and the safe return-URL helper.
6. Reuse typed `Localization` messages, shared loading/error/notice components, pagination, icons, and avatars. Add visible-message keys in every supported language in `shared/localization/messages.ts`. Keep documentation in English.

## Code and UI shape

Use two-space indentation, single-quoted strings, strict types, readonly dependencies, descriptive camelCase members, and adjacent import/member ordering. Follow Angular `.editorconfig`, ESLint, and Prettier's 100-column setting.

Use existing Tailwind utilities and `src/styles.css` tokens. Preserve appearance preferences, responsive layout, template control-flow conventions, associated labels, keyboard access, meaningful button types, and accessible error feedback.

## Configuration and authentication

`AppConfiguration` loads `public/appsettings.json` before session initialization and requires a same-origin API. Keep API URLs runtime-configurable. Use the registered interceptor so cookies and CSRF headers stay inside the matching API boundary.

Reuse `CurrentSession` and `CurrentUser` and preserve refresh/expiration behavior after account changes. Client guards support navigation. Protected operations also require the backend's access checks.

Read `frontend-testing` for associated specs and `frontend-security-performance` for authentication, uploads, configuration, or performance changes. Run lint, production build, and relevant tests. Verify changed API contracts through the backend as well.
