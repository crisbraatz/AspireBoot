# GitHub Copilot entry point

Read and follow [AGENTS.md](../AGENTS.md) at the repository root. Resolve its skill paths from the repository root and open the applicable `.agents/skills/<skill-name>/SKILL.md` files before implementation or review.

When runtime context identifies GitHub Copilot, use that identity for entry-point selection. All assistants share the same architecture, test, security, performance, and verification instructions. Keep future engineering-rule updates in `AGENTS.md` and the shared skills. If your current client cannot read a referenced file, state that limitation and request the relevant content before relying on it.
