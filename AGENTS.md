# AI Coding & Operational Rules

These rules must be strictly followed by all AI agents operating within this repository to ensure infrastructure synchronization, codebase consistency, and prevent unintended deployments or commits.

## 1. Explicit Commit Approval & Separation
- **NEVER execute `git commit` or `git push` without explicit, prior approval from the user.** 
- **NEVER prompt or ask the user to commit or push.** Do not ask "shall I commit?", "are you ready for me to commit?", or "should I push?". Simply make changes locally, prepare them, and wait for the user to initiate.
- **Do NOT combine commit and push.** When the user tells you to "commit", only execute `git commit`. Do NOT run `git push` unless the user explicitly instructs you to "push".

## 2. Git-First Workflow & Server Synchronization
- **ALL code, infrastructure, and configuration changes must be made in the local repository first.** Never modify files directly on the server without tracking them in the local repository first.
- **Server Synchronization:** After making changes in the local repository and pushing them to GitHub, you must connect to the server (e.g., `ssh minipc`) and run `git pull` in the corresponding application directory to deploy. The local repository and the server must always be exactly in sync.
- **Gitignored Files:** Files in `.gitignore` (such as `.env.secret`) are not tracked by Git and must be manually copied or synchronized to the server (e.g., via secure copy or piped SSH transfer) to ensure secrets remain in sync.

## 3. STRICT: Use Wrapper Scripts (Make/StackPilot), NEVER Raw Docker Compose
- **NEVER run raw `docker compose` commands (e.g., `docker compose up -d`) on the server.** Raw commands bypass the injection of secret environment variables, which will instantly break critical infrastructure.
- **ALWAYS use the official wrapper scripts.** Use `make` (e.g., `make up`, `make down`, `make deploy`, `make status`) for the platform/homelab stack, and `stackpilot-deploy` tools (`deploy.sh`, etc.) for StackPilot deployments. These scripts properly source all required secret dependencies.

## 4. Codebase Architecture & Conventions (C# / Blazor)
- **Global Usings Only:** All `using` and `inject` statements MUST go into `_Imports.razor` (for Blazor components) or `GlobalUsings.cs` (for C# files). Never put them at the top of individual files.
- **Strict File Separation:** Always use separate files for code-behind and styles. A Blazor component `Component.razor` MUST have its C# logic in `Component.razor.cs` and its scoped CSS in `Component.razor.css`. Never use `@code { }` blocks or `<style>` tags inside the `.razor` file.
- **Database Migrations:** When generating new EF Core migrations, always use the provided PowerShell scripts in the `scripts/` directory (e.g., `scripts/Add-Migration.ps1` or `scripts/New-Migration.ps1`) instead of running `dotnet ef` manually. This ensures that migrations for both SQL Server and PostgreSQL are correctly generated in parallel.
- **Localization First:** Every time we change something (adding new text, labels, or UI elements), the localization should follow. All user-facing text MUST be added to the resource files (e.g., `AicvResources.resx`) and localized using `IStringLocalizer`.

## 5. Proactive Documentation
- **Proactive Documentation:** After EVERY feature change or refactor, proactively check and update the documentation (`README.md`, `docs/`, etc.) if necessary.
