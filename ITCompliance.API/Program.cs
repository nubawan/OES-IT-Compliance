using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ITCompliance.API.Data;
using ITCompliance.API.Models;
using ITCompliance.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// The connection string is NOT in source code.
//   Development -> dotnet user-secrets
//   Production  -> ConnectionStrings__DefaultConnection env var
// =====================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found. " +
        "In Development run: dotnet user-secrets set " +
        "\"ConnectionStrings:DefaultConnection\" \"<your connection string>\". " +
        "In Production set the ConnectionStrings__DefaultConnection " +
        "environment variable.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<ActiveDirectoryService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Bootstrap the first Admin(s) from config so Role Management is
// reachable without a chicken-and-egg manual DB edit. Idempotent -
// safe to leave the config key populated across every deploy.
// Never lets a slow/unreachable DB stop the app from starting - same
// graceful-degradation treatment as every other DB/AD access in this
// app (see ActiveDirectoryService, AccountController).
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var bootstrapAdminIds = app.Configuration
        .GetSection("Bootstrap:AdminEmployeeIds")
        .Get<string[]>() ?? Array.Empty<string>();

    foreach (var empId in bootstrapAdminIds
        .Where(id => !string.IsNullOrWhiteSpace(id)))
    {
        var alreadyAdmin = db.RoleAssignments.Any(r =>
            r.EmployeeId == empId &&
            r.Role == RoleNames.Admin &&
            r.IsActive);

        if (!alreadyAdmin)
        {
            db.RoleAssignments.Add(new RoleAssignment
            {
                EmployeeId = empId,
                Role = RoleNames.Admin,
                DepartmentCode = null,
                CreatedByEmpId = "SYSTEM"
            });
        }
    }

    db.SaveChanges();
}
catch (Exception ex)
{
    app.Logger.LogWarning(
        ex,
        "Could not apply Bootstrap:AdminEmployeeIds at startup - " +
        "database unreachable or slow. The app will still start; " +
        "this will retry on the next restart.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.MapControllers();

app.Run();