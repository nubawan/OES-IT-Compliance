# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

IT Compliance Portal (Orient Power) - an ASP.NET Core MVC app (.NET 10) that manages internet-access-request approvals through a fixed workflow chain: **Employee -> IT Officer -> HOD -> Security Head -> Boss**. Any stage can reject, ending the request. Auth is LDAP bind against on-prem Active Directory; user identity/role data comes from an HR SQL view the app does not own.

No git repo has been initialized yet in this working directory.

## Solution structure

- `ITCompliance.API.slnx` - the solution file (open this in VS, not a `.csproj`, to see both projects below).
- `ITCompliance.API/` - the actual web app (target framework `net10.0`).
- `ITCompliance.API/tools/DbViewer/` - a separate **.NET Framework 4.8** companion project holding `ITcomplianceDB.dbml`, purely so the classic LINQ-to-SQL drag-drop designer can render the schema (that designer doesn't exist for .NET 10/modern projects). The main API project does not reference it. Regenerate the base `.dbml` after schema changes with `dotnet run --project ITCompliance.API/tools/ModelDiagram`.
- `ITCompliance.API/tools/ModelDiagram/` - standalone generator that emits `ModelDiagram.html` / the DbViewer `.dbml`. Both `tools/*` folders are explicitly excluded from the main `.csproj` (`<Compile Remove="tools\**" />` etc.) so they don't build as part of the web app.

## Common commands

Run all commands from `ITCompliance.API/` (the project folder), e.g. `cd "ITCompliance.API"`.

```bash
# build
dotnet build

# run (dev profile, picks up appsettings.Development.json + user-secrets)
dotnet run
# or explicitly:
dotnet run --launch-profile https   # https://localhost:7294

# EF Core migrations (dotnet-ef must be installed: dotnet tool install --global dotnet-ef)
dotnet ef migrations add <Name>
dotnet ef database update

# publish for IIS deployment
dotnet publish -c Release -o publish

# check for vulnerable packages
dotnet list package --vulnerable

# regenerate the DbViewer .dbml / ModelDiagram.html after a schema change
dotnet run --project tools/ModelDiagram
```

There is no test project in the solution yet (`dotnet test` has nothing to run).

### Local secrets (required to run)

The connection string is **not** in any checked-in file. In Development it comes from user-secrets (`UserSecretsId` is set in the `.csproj`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string>"
```

In Production it comes from the `ConnectionStrings__DefaultConnection` environment variable (double underscore). `Program.cs` throws on startup with an explanatory message if neither is set. The AD/LDAP server defaults to `DC-AD-01.orient-power.com` and is overridable via `Ldap:Server` config / `Ldap__Server` env var.

## Architecture

### Request workflow and status model

`Models/RequestStatus.cs` defines the canonical status strings and is the single source of truth for the pipeline - always use these constants rather than string literals:

```
Pending -> "IT Officer Approved" -> "HOD Approved" -> "Security Head Approved" -> "Boss Approved"
```

Each approver stage has its own rejection status (`Rejected`, `Rejected by HOD`, `Rejected by Security Head`, `Rejected by Boss`); `RequestStatus.IsRejected` checks the `"Rejected"` prefix rather than an exact match against all four. Each of the four approver controllers (`ITOfficerController`, `HODController`, `SecurityHeadController`, `BossController`) follows the same shape: a `Dashboard` GET that filters `InternetAccessRequests` to the status this role acts on, KPI counts (grouped, not N+1) via `RequestStatus.Kpi` records, plus `Approve`/`Reject` POST actions that move the row to the next status and stamp a `<Role>Remarks` column. When adding a new approval stage or changing the chain, all four controllers, `RequestStatus`, and the relevant `Views/<Role>/Dashboard.cshtml` need to stay in sync.

### Data layer - two DbContexts, only one is wired up

- `Data/AppDbContext.cs` is the context actually registered in `Program.cs` and used everywhere. It owns `Employees` and `InternetAccessRequests` (code-first, migrated via EF migrations in `Migrations/`), and maps two tables/views the app does **not** own, read-only, via Fluent API instead of migrations:
  - `OESEmployees` -> HR view `OES_vuEmployeeDetails` (`.ToView(...)`) - the source of truth for employee identity/department, looked up by email after AD auth succeeds.
  - `HODDetails` -> `tbl_HODdetails` (`.ToTable(...)`).
  Never generate a migration that touches these two - they belong to another system.
- `Models/ITcomplianceDBContext.cs` + the `tbl_Employee`/`OES_vuEmployeeDetail` models are leftover EF Core Power Tools scaffold output (reverse-engineered reference only, `efpt.config.json` drives that tool). It is not registered in DI and not used at runtime.
- Legacy `Models/Employee.cs` (with `PasswordHash`) is a pre-AD local-auth relic, listed for removal once a real roles DB lands (see PROJECT_STATUS.md).

### Auth flow

`Services/ActiveDirectoryService.cs` binds to AD over LDAP (plaintext port 389 - flagged in PROJECT_STATUS.md as needing to move to LDAPS 636). It accepts an email or bare AD username, and on email input retries the local-part as a `sAMAccountName` since some domains have UPNs that don't match emails. AD sub-error codes (embedded in LDAP error 49's `data NNN` string, e.g. `532` = password expired, `775` = locked out) are parsed into a friendly `AdAuthStatus` enum so `AccountController.Login` can show specific messages instead of a blanket "invalid credentials".

After a successful AD bind, `AccountController` looks the identity up in `OESEmployees` (the HR view) to get identity/department data and builds the cookie principal from that record - AD success alone does not grant access if the person isn't in the HR view. Currently every login gets `ClaimTypes.Role = "Employee"` unconditionally; there is no roles table yet, so `[Authorize(Roles=...)]` is commented out on every approver controller (see "Known gaps" below).

`/PowerLogin` is a dev-only bypass (`AccountController.PowerLogin`, returns 404 unless `ASPNETCORE_ENVIRONMENT=Development`) that signs in as any employee picked from an HR-view-backed dropdown using a hardcoded password (`PowerLoginPassword` constant in `AccountController.cs`). When deploying, verify `ASPNETCORE_ENVIRONMENT` is `Production` so this route 404s.

### Views / shared UI

- `Views/Shared/_Layout.cshtml` is the shared shell (sidebar, topbar, TempData alert rendering, double-submit-guard script). Login and PowerLogin opt out via `Layout = null` and keep a standalone card design.
- `Views/Shared/_SidebarNav.cshtml` (config-driven item list) + `wwwroot/css/sidenav.css` render the nav; `wwwroot/css/theme.css` holds design tokens/components (brand color `#C8102E`) shared across all dashboards; `wwwroot/css/style.css` is scoped to the login pages only.
- `Views/Shared/_IconSprite.cshtml` is a self-hosted inline SVG sprite (referenced via `<use href="#i-...">`) - intentionally no CDN/icon-font dependency, since the app runs intranet-only.
- `Views/Shared/_KpiCard.cshtml` / `_EmptyState.cshtml` are shared partials used by every dashboard for the KPI row and "no requests" state.

## Known gaps (see PROJECT_STATUS.md for the full, prioritized list)

`PROJECT_STATUS.md` in `ITCompliance.API/` is the authoritative, actively-maintained checklist of what's implemented vs. skipped - check it before assuming a feature is missing or before re-flagging something already listed there. Highlights relevant to day-to-day changes:

- **Authorization is fully disabled.** All `[Authorize(Roles=...)]` attributes on the five role controllers are commented out pending a roles DB design; anyone can currently reach any dashboard. Don't "fix" this piecemeal without checking PROJECT_STATUS.md first - it's an intentional, tracked gap blocked on a DB design decision.
- LDAP runs on cleartext port 389, not LDAPS.
- Controllers generally return raw `ex.Message`/inner exceptions to the client instead of using a global exception handler.
- `CORS` policy in `Program.cs` is `AllowAnyOrigin()` even though the app is same-origin.
- No approval audit trail (no approver identity/timestamp captured beyond `UpdatedAt`).
- `DateTime.Now` is used throughout instead of UTC.
- No tests exist in the solution.

## Deployment

Full IIS deployment steps (hosting bundle, app pool config, env vars, HTTPS, troubleshooting table) live in `ITCompliance.API/DEPLOY-IIS.md` - consult it rather than re-deriving IIS/ASP.NET Core Module setup from scratch. Key points: config is via IIS `environmentVariables` (site/app-pool level), not `web.config` (which only bootstraps ANCM); `ASPNETCORE_ENVIRONMENT=Production` is required in prod both for correct behavior and to close off `/PowerLogin`.
