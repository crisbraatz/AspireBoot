# AspireBoot agent instructions

## Shared entry point

Apply these instructions to work in this repository. Keep documentation, code identifiers, comments, and new skill content in English. Read the relevant existing feature before changing it.

Identify the active assistant from explicit runtime or client context when available. Codex normally loads this file directly. Claude Code enters through `CLAUDE.md`, Gemini CLI through `GEMINI.md`, and GitHub Copilot through `.github/copilot-instructions.md`. If the client identity is unavailable, continue with these shared instructions. Never infer a model from code style or a user's wording. Identity affects the entry point only. All assistants use the same skills below.

## Skill routing

Read the applicable `SKILL.md` files before implementation. Resolve paths relative to the repository root. Load additional skills when the task crosses their boundaries.

| Work | Shared skill |
| --- | --- |
| Backend features, endpoints, CQRS, persistence, consumers | [.agents/skills/backend-development/SKILL.md](.agents/skills/backend-development/SKILL.md) |
| Backend unit or integration tests | [.agents/skills/backend-testing/SKILL.md](.agents/skills/backend-testing/SKILL.md) |
| Backend security/performance review, sensitive changes, dependency failures, telemetry | [.agents/skills/backend-security-performance/SKILL.md](.agents/skills/backend-security-performance/SKILL.md) |
| Angular features, routes, contracts, UI, localization | [.agents/skills/frontend-development/SKILL.md](.agents/skills/frontend-development/SKILL.md) |
| Angular tests and coverage | [.agents/skills/frontend-testing/SKILL.md](.agents/skills/frontend-testing/SKILL.md) |
| Browser security/performance review, authentication, uploads, bundle work | [.agents/skills/frontend-security-performance/SKILL.md](.agents/skills/frontend-security-performance/SKILL.md) |

For a feature touching both API and UI, read both development skills. For tests, add the corresponding testing skill. For authentication, authorization, secrets, uploads, proxy trust, messaging durability, or performance changes, add the corresponding security/performance skill. Apply review guidance proportionally to the requested change.

## Repository contract

- Preserve feature-oriented paths such as `Identity/Users/UpdateUser` and the current project dependencies. Keep HTTP contracts in Api, application behavior and interfaces in Core, adapters in Infrastructure, and consumers in Worker.
- Use existing Result/Failure types, CQRS interfaces and validation decorators, repository interfaces, shared UI, and dependency telemetry helpers. Check existing callers before changing a contract.
- Treat `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, frontend lint/format configuration, and the package lockfile as authoritative. Add .NET versions centrally. Keep npm dependency changes and lockfile changes together.
- Prefer the smallest complete change. Reuse established behavior and avoid introducing a second framework or shared abstraction without a concrete need.
- Keep secrets, private keys, personal data, session cookies, recovery tokens, and signed URLs out of committed artifacts and logs. Preserve server-side access checks, CSRF, rate limits, request bounds, and outbox transactions when extending a feature.
- Use the user-authorized scope for external actions and deployments. Treat repository examples as development configuration.

## Commands and verification

Use standard shell commands directly with the project's installed tools.

From the repository root, backend checks are `dotnet build AspireBoot.slnx -c Debug` and `dotnet test AspireBoot.Tests/AspireBoot.Tests.csproj -c Debug`. Integration tests require Docker. From `AspireBoot.Angular`, checks are `npm run lint`, `npm run build`, and `npm run test:coverage`.

Run checks relevant to the change and report their actual outcomes. Explain any missing prerequisites or unexecuted checks. Keep existing coverage gates and analyzer rules. Review the final diff for unrelated changes.
