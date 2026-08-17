# PROJECT STATUS - IT Compliance Portal

Last updated: 2026-08-14

This file tracks what has been **implemented** and what has been
**skipped / pending** during the audit and improvement work.
Use it as the checklist for the next sessions.

---

## 1. IMPLEMENTED

### Build & Folder Organization (done)

| Change | Files |
|---|---|
| Fixed compile errors (`HODDetail.Email` -> `HODEmail`) | `Controllers/AccountController.cs` |
| `models/` renamed to `Models/` (matches namespace) | `Models/` |
| Consolidated `ActiveDirectoryService` - kept the better `Services/` copy (LDAP v3 + specific `LdapException` catch), deleted the older root copy | `Services/ActiveDirectoryService.cs` |
| Removed `.csproj` hacks (`Compile Remove` / `None Include`) | `ITCompliance.API.csproj` |
| Deleted dead test script `ADController.cs` (console app with hardcoded `administrator@` username, caused CS7022 warning) | - |

Build status: **0 errors, 0 warnings.**

### UI Theme Overhaul (done)

- **New unified theme:** `wwwroot/css/theme.css` - design tokens
  (brand red #C8102E), topbar, cards, alerts, status badges for
  every workflow stage, detail grids, remarks chain, action bar,
  forms, empty states, responsive.
- **New shared layout:** `Views/Shared/_Layout.cshtml` +
  `Views/_ViewStart.cshtml` - centralizes html shell, sidebar,
  topbar (title/subtitle/user chip with initials/logout),
  TempData success+error alerts (now shown on ALL pages),
  footer, and global double-submit protection for every form.
- **All 8 views rewritten on the theme** (Employee Dashboard +
  Request form, ITOfficer / HOD / SecurityHead / Boss
  dashboards, Login, Power Login). Consistent structure, status
  badges colored per stage, prior-stage remarks shown to later
  approvers, single shared remarks field with Approve/Reject
  buttons (HTML5 `formaction`).
- **Fixed:** stray markdown ``` fences rendering as text on the
  Employee Dashboard; broken status-badge ternary; inconsistent
  per-view alert handling.
- Login + Power Login pages opt out of the layout
  (`Layout = null`) and keep the standalone card design with a
  shared footer tagline.
- **Deleted superseded files:** `employee-dashboard.css`,
  `form.css`, and dead prototype files (`boss.css`, `hod.css`,
  `it-officer.css`, `security-head.css`, `login.css`,
  `components.css`, entire `wwwroot/js/` folder - all
  unreferenced). CSS is now exactly: `theme.css` (app),
  `sidenav.css` (sidebar), `style.css` (login pages).
- Verified: build 0/0, login page + assets serve 200, auth
  redirect works, PowerLogin reaches its DB query (fails only
  when SQL at 10.100.1.17 is unreachable from the machine).

### Dashboard Revamp - Modules 1-4 (done; Module 0 roles SKIPPED for now)

- **Session timeout:** was effectively unlimited (default 14-day
  ticket). Now 30 minutes sliding inactivity; cookie still dies
  when the browser closes.
- **M1 Theme/icons:** purple tokens replaced with slate/teal/cyan
  status palette; inline SVG icon sprite
  (`Views/Shared/_IconSprite.cshtml`, self-hosted - no CDN,
  intranet-safe); all emoji replaced via `<use href="#i-...">`.
- **M2 Shell:** sidebar is config-driven (item list with icon +
  section, roles-ready for Module 0), collapses to icons-only
  with state persisted in localStorage; header user chip and a
  notifications bell are native `<details>` dropdowns (bell is a
  shell - no notification data yet).
- **M3 Employee dashboard:** KPI row (Pending / In progress /
  Approved / Rejected, computed from the already-loaded list -
  no extra queries); "Create New Request" is now a native
  `<dialog>` modal posting to the existing endpoint.
- **M4 Approver dashboards:** shared `_KpiCard` + `_EmptyState`
  partials; each dashboard has an Awaiting/Approved-by-me/
  Rejected-by-me KPI row (one grouped query, no N+1);
  controllers use `RequestStatus` constants and no longer leak
  exception messages.
- New shared code: `Models/RequestStatus.cs` (status constants +
  `Kpi` record), `Views/Shared/_IconSprite/_KpiCard/_EmptyState`.
- **M5 QA:** no emoji left in Views, no purple hex/vars left in
  CSS, build 0/0, public pages verified. Role-matrix test is
  deferred with Module 0 (roles).

### Login Diagnostics + Fast PowerLogin (done)

- `ActiveDirectoryService` rewritten with **edge-case detection**
  (AD sub-error codes parsed from LDAP error 49 "data 5xx"):
  password expired (532) / must change (773) / locked out (775) /
  disabled or expired (533/701/531) / user not found (525) -
  each gets its own friendly login message instead of the old
  blanket "invalid email or password". All attempts are logged
  with the raw server error for IT diagnosis.
- Distinguishes **unreachable DC** from **bad credentials**;
  5s timeout; accepts email OR plain AD username (email local
  part retried as sAMAccountName - the old portal used
  usernames, not emails). Server configurable via `Ldap:Server`
  (env: `Ldap__Server`).
- HR lookup: if AD login succeeds but the email is not in the
  HR view, the message now says exactly that ("AD login worked,
  but your email is not set up in the HR system yet - ask HR
  to update it"). Username logins also match HR emails by
  local part. Dead `checkhod` query removed (returns with the
  roles DB work).
- `/PowerLogin` renders **instantly**; the employee dropdown
  loads in the background from `/PowerLogin/Employees` (JSON,
  dev-guarded). User-secrets connection string now has
  `Connect Timeout=8;ConnectRetryCount=0` (fail fast).

### DbViewer visible in the solution (done)

- `D:\IT-Complia\ITCompliance.API.slnx` now includes the
  `tools\DbViewer` Framework 4.8 project - open the SLNX (not
  the csproj) in VS and the `ITcomplianceDB.dbml` appears in
  Solution Explorer under DbViewer. Double-click opens the
  classic drag-drop designer; add tables via Server Explorer
  (connect to 10.100.1.17) and drag them onto the surface.
  Regenerate the base file anytime:
  `dotnet run --project tools/ModelDiagram`.

- **Removed:** `AppModel.dgml`, `Database.dgml` (dgml output
  removed from the generator too), `scaffold-db.cmd`,
  `ITCompliance.API.http`, empty `DTOs/` + `Interfaces/`
  folders. Kept: `efpt.config.json` (EF Core Power Tools
  settings), scaffolded reference entities (used by the dbml
  generator), `ModelDiagram.html`.
- **CSS cleanup:** `style.css` rewritten - removed dead
  `#role` / `.role-group` / `#message` rules from the old
  role-dropdown prototype; `power-select` styles consolidated
  into style.css (no longer duplicated in sidenav.css);
  `_Layout` alerts now use a proper `.alert-bar` class instead
  of inline styles.
- **UI re-audit:** every class used in views verified against
  theme.css/sidenav.css/style.css - all defined; all status
  badge slugs covered; PowerLogin + Login verified live in a
  browser (structure + rendering).
- **IIS deployment:** `web.config` added (ANCM hosting only -
  no app settings) + full step-by-step guide in
  **`DEPLOY-IIS.md`** (server prep, SQL login, publish, app
  pool, env vars incl. `ASPNETCORE_ENVIRONMENT=Production`,
  HTTPS, troubleshooting, update procedure).

### Packages & Tooling (done)

- Installed **dotnet-ef 10.0.11** (global CLI tool) - required for
  `dotnet ef migrations ...` and `dotnet ef dbcontext scaffold ...`.
- **Fixed hidden vulnerability:** `<NoWarn>NU1903</NoWarn>` in the
  .csproj was suppressing a High-severity advisory for transitive
  `Microsoft.OpenApi 2.0.0` (GHSA-v5pm-xwqc-g5wc). Pinned
  `Microsoft.OpenApi 2.12.0` explicitly and removed the suppression.
  `dotnet list package --vulnerable` now reports clean.
- Still to install manually: **EF Core Power Tools** VS extension
  (Visual Studio > Extensions > Manage Extensions) - the .dbml-style
  reverse-engineering wizard; it is a VSIX, not a NuGet package.
- Needs no package (built into ASP.NET Core): cookie auth, rate
  limiting, user-secrets, HTTPS/HSTS middleware.
- Optional later (only if switching to Windows/AD integrated auth):
  NuGet `Microsoft.AspNetCore.Authentication.Negotiate`.

### Sidebar Navigation (done)

- New shared partial: `Views/Shared/_SidebarNav.cshtml`
- New stylesheet: `wwwroot/css/sidenav.css` (brand red `#C8102E` accent)
- Included in ALL views:
  - `Views/Employee/Dashboard.cshtml`
  - `Views/Employee/Request.cshtml`
  - `Views/ITOfficer/Dashboard.cshtml`
  - `Views/HOD/Dashboard.cshtml`
  - `Views/SecurityHead/Dashboard.cshtml`
  - `Views/Boss/Dashboard.cshtml`
- Nav items: My Dashboard, New Internet Request, IT Officer,
  HOD, Security Head, Boss, Power Login, Logout.
- Current page is highlighted automatically.
- Sidebar hides below 900px width.

### Power Login / Superadmin (done - DEV ONLY)

- Route: **`/PowerLogin`** (sidebar link; no link on the login
  page anymore).
- Employee **dropdown fetched from the HR database**
  (`OES_vuEmployeeDetails` via `OESEmployees`).
- **Hardcoded developer password**: `Power@2026`
  (const `PowerLoginPassword` in `Controllers/AccountController.cs` -
  change it there to rotate).
- No role selector - signs the selected employee in exactly like
  a normal login (Employee role; real role resolution arrives
  with the roles DB) and lands on the Employee Dashboard.
- **Guardrail:** returns 404 unless the environment is
  Development. `ASPNETCORE_ENVIRONMENT` must NOT be
  `Development` on the production server.
- Login page shows no footer text, no AD-credentials hint, and
  no link to Power Login (removed on request).

---

## 2. SKIPPED / PENDING (priority order)

### CRITICAL - security

1. **Authorization is disabled everywhere.**
   All five `[Authorize(Roles = ...)]` attributes are still
   commented out (`Employee`, `ITOfficer`, `HOD`, `SecurityHead`,
   `Boss` controllers). Anyone - even without logging in - can
   open every dashboard and approve/reject requests.
   **Blocked on: roles DB design (see below).**

2. **Roles DB (you are designing this).**
   Once the tables exist: resolve the role at login (roles table /
   `tbl_HODdetails` / AD groups), add it as `ClaimTypes.Role`,
   then un-comment the `[Authorize]` attributes.
   Power Login already sets the role claim, so it keeps working.

3. **Connection string - REMOVED from all source code (done).**
   `Program.cs` now reads configuration
   (`GetConnectionString("DefaultConnection")` with a clear
   error if missing). Both appsettings files carry no secrets.
   The real string lives in **user-secrets** on the dev machine
   (`dotnet user-secrets list`). Production must set the
   `ConnectionStrings__DefaultConnection` environment variable.
   `scaffold-db.cmd` no longer contains a password either.
   **Still to do (DBA side):** rotate the exposed `abcd@1234`
   password and replace the `sa` login with a dedicated
   least-privilege app login.

4. **LDAP on port 389 (cleartext).**
   `Services/ActiveDirectoryService.cs` - switch to LDAPS 636
   (needs the AD certificate trusted on the app server).

### HIGH - security

5. **Exception details leaked to users** - every controller catch
   block returns `ex.Message` + inner exception. Replace with a
   global exception handler (`app.UseExceptionHandler` /
   `app.UseStatusCodePages`).

6. **No rate limiting on login** - brute-force avenue against AD.

7. **CORS `AllowAnyOrigin()`** in `Program.cs` - scope to the real
   frontend origin or remove (app is same-origin anyway).

8. **No approval audit trail** - no approver identity/timestamp
   recorded. For a compliance system this is required eventually.

### MEDIUM - code quality

9. `AuthController` (`/api/Auth/login`) is the old API login path -
   signs in ANY valid AD account with no role and no HR check.
   Decide: delete it, or align it with the MVC login. The
   `wwwroot/js/*` files (login.js etc.) are unreferenced leftovers
   from the static-HTML prototype - deletable.
10. Status handled as inconsistent magic strings ("Rejected",
    "Rejected by HOD"...). Introduce a `RequestStatus` enum/constants.
11. `InternetAccessRequest.DepartmentCode` is `[Required]` but
    never assigned (always empty).
12. `DateTime.Now` used everywhere - prefer UTC.
13. Legacy `Employee` model + `PasswordHash` column (old local-auth
    design) - drop when roles DB lands.
14. No version control - initialize git and add a `.gitignore`
    (bin/obj/.vs) AFTER scrubbing passwords.
15. No tests.

---

## 3. HOW-TO NOTES (old -> new mapping)

| .NET Framework 4.8 | This project |
|---|---|
| `Web.config` connectionStrings | `appsettings.json` + `builder.Configuration.GetConnectionString(...)` |
| `.dbml` drag-drop designer | EF Core Power Tools (VS extension) or `dotnet ef dbcontext scaffold` |
| `DataContext` / LINQ to SQL | `AppDbContext` / LINQ over `DbSet` (same syntax) |
| Windows Integrated Auth (IIS) | Current: forms login + LDAP bind. Option: `AddAuthentication(IISDefaults.AuthenticationScheme)` or the Negotiate package |
| App-owned tables | Code-first: entity in `Models/` -> `dotnet ef migrations add X` -> `dotnet ef database update` |
| Tables you don't own (HR view, HOD table) | Mapped manually with `.ToView()` / `.ToTable()` in `AppDbContext` - never scaffold over them |

### DB preview from the terminal (no VS extension needed)

- **DBML designer support (classic drag-drop view):**
  `tools/DbViewer/` is a .NET Framework 4.8 companion project
  containing `ITcomplianceDB.dbml` with all 5 real tables/views.
  Open `tools\DbViewer\DbViewer.csproj` in VS (File > Open >
  Project/Solution), then double-click the .dbml - the classic
  LINQ to SQL designer renders it. Save once if the surface
  starts empty; drag in more tables from Server Explorer
  (10.100.1.17). The main API project does NOT reference this.
  Why a separate project: the .dbml designer only works in
  Framework projects - it does not exist for .NET 10.
- Regenerate the .dbml after schema changes:
  `dotnet run --project tools/ModelDiagram` (also regenerates
  ModelDiagram.html / AppModel.dgml / Database.dgml).
- The reverse engineer already ran via EF Core Power Tools:
  `Models/ITcomplianceDBContext.cs` + scaffolded entities
  (reference only - hardcoded connection string was removed).
