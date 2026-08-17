# IIS DEPLOYMENT GUIDE - IT Compliance Portal

Step-by-step for deploying to IIS, written for someone coming
from the old .NET Framework 4.8 portals. Big differences from
the old days:

1. **No "No Managed Code" isn't a typo** - ASP.NET Core apps run
   their own runtime; IIS is just a reverse proxy in front.
2. **Web.config only starts the app** - it does NOT hold app
   settings or connection strings anymore. Configuration comes
   from environment variables on the server.
3. You deploy by **copying a publish folder**, not by building
   on the server or using msdeploy (though msdeploy also works).

---

## STEP 1 - Prepare the server (one time)

### 1.1 Install the .NET 10 Hosting Bundle

Download the **.NET 10 Hosting Bundle** (x64) from
https://dotnet.microsoft.com/download (the "hosting bundle"
link, NOT the plain runtime). It installs:
- the .NET runtime
- the ASP.NET Core Module for IIS (ANCM)

Reboot (or `iisreset`) after installing.

### 1.2 Enable IIS features

Server Manager → Add Roles and Features → Web Server (IIS):
- Web Server → Common HTTP Features (all)
- Web Server → Application Development → **WebSocket Protocol**
  (optional), CGI NOT needed
- Web Server → Security → Basic / Windows / Request Filtering
  as your policy requires
- Management Tools → IIS Management Console

### 1.3 Network reachability (test before going further)

The server must reach:
- `10.100.1.17` (SQL Server, port 1433) - for the database
- `DC-AD-01.orient-power.com` (port 389/636) - for AD logins

Test: `Test-NetConnection 10.100.1.17 -Port 1433`

---

## STEP 2 - Prepare the database (one time)

Do NOT use `sa`. Create a dedicated login in SSMS:

```sql
CREATE LOGIN itcompliance_app WITH PASSWORD = '<strong password>';
USE ITcomplianceDB;
CREATE USER itcompliance_app FOR LOGIN itcompliance_app;
ALTER ROLE db_datareader ADD MEMBER itcompliance_app;
ALTER ROLE db_datawriter ADD MEMBER itcompliance_app;
ALTER ROLE db_ddladmin   ADD MEMBER itcompliance_app;  -- EF migrations
```

---

## STEP 3 - Publish from your dev machine

In the project folder:

```
cd D:\IT-Complia\ITCompliance.API
dotnet publish -c Release -o publish
```

This produces a self-contained-in-folder deployment in
`publish\` (the app DLL, the web.config that starts it,
appsettings, Views, wwwroot). Zip it and copy to the server,
e.g. `C:\inetpub\ITCompliance`.

---

## STEP 4 - Create the site in IIS

1. IIS Manager → **Application Pools** → Add Application Pool:
   - Name: `ITCompliancePool`
   - .NET CLR Version: **No Managed Code** (this is correct!)
2. Right-click **Sites** → Add Website:
   - Name: `IT Compliance Portal`
   - Physical path: `C:\inetpub\ITCompliance`
   - Application pool: `ITCompliancePool`
   - Binding: port 80 first (HTTPS in step 7)
3. Give the app pool identity read access to the folder (it
   has it by default under inetpub).

---

## STEP 5 - Configure the app (environment variables)

**App Pool level** (recommended): IIS Manager → Application
Pools → `ITCompliancePool` → Advanced Settings →
General → Application Pool Identity → ... open
`Configuration Editor` → section `system.applicationHost/applicationPools`,
or simpler - set them at the **Site level**:

IIS Manager → select the site → **Configuration Editor** →
section: `aspNetCore` → `environmentVariables` → add:

| Name                                        | Value                                                        |
|---------------------------------------------|--------------------------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`                    | `Production`                                                 |
| `ConnectionStrings__DefaultConnection`      | `Server=10.100.1.17;Database=ITcomplianceDB;User ID=itcompliance_app;Password=<strong password>;TrustServerCertificate=True;` |

IMPORTANT:
- Double underscore `__` in the name - that is correct.
- **`ASPNETCORE_ENVIRONMENT` must be `Production`.** The dev
  Power Login page only exists when this is `Development` -
  verify after deploy that `https://yoursite/PowerLogin`
  returns 404.
- No password in any file - it lives only in this env var.

---

## STEP 6 - First start + smoke test

```
iisreset
```

Browse to `http://yourserver/`:
- Login page renders -> runtime + ANCM OK
- Log in with an AD account -> DB + AD both reachable
- `http://yourserver/PowerLogin` -> **404** (environment guard works)

If you get errors, enable logs (STEP 8).

---

## STEP 7 - HTTPS (required before real use)

Login cookies must not travel as plaintext:

1. Obtain a cert (internal CA or purchased) for the site's hostname.
2. IIS Manager → site → Bindings → Add → https, port 443,
   select the certificate.
3. The app already redirects HTTP → HTTPS (`UseHttpsRedirection`).
4. Consider HSTS once HTTPS is proven (`app.UseHsts()` - add to
   `Program.cs` production branch; currently pending, see
   PROJECT_STATUS.md).

---

## STEP 8 - Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| 500.19 | ANCM not installed / web.config unreadable | Install Hosting Bundle, `iisreset` |
| 500.30 / 500.31 | App failed to start | Env var typo (check `ConnectionStrings__` name), wrong password, missing runtime |
| 500.35 | .NET runtime version mismatch | Wrong hosting bundle version |
| Login fails, "Employee not found" | DB reachable but HR view empty for that user | Check the account exists in `OES_vuEmployeeDetails` |
| Login fails instantly for everyone | AD unreachable from server | Test port 389 to `DC-AD-01`, check firewall |

Enable detailed startup logs when stuck: in `web.config` set
`stdoutLogEnabled="true"`, create a `logs\` folder in the site
root, reproduce, read `logs\*.log`, then turn it back off.

---

## STEP 9 - Updating later

From dev machine:

```
dotnet publish -c Release -o publish
```

Copy the new files over the site folder (IIS locks the DLL, so
either stop the app pool first, or copy when no one is using it),
then restart the app pool. No `iisreset` needed for app updates.

If you add EF Core migrations later, they apply automatically on
the next deploy only if you call `Database.Migrate()` at startup
(currently NOT configured - run `dotnet ef database update`
manually during maintenance, or ask to add auto-migration).
