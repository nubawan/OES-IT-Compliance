// ============================================================
//  MODEL DIAGRAM GENERATOR
//
//  Generates a dbml-style view of BOTH EF Core contexts,
//  without needing the database or any VS extension:
//
//     AppModel.dgml      - the app's code-first model (AppDbContext)
//     Database.dgml      - the REAL database as scaffolded
//                          (ITcomplianceDBContext, from EF Core
//                          Power Tools reverse engineer)
//     ModelDiagram.html  - both, as entity cards in the browser
//
//  Run from the project root:
//     dotnet run --project tools/ModelDiagram
// ============================================================

using System.Text;
using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// ------------------------------------------------------------
// Collect metadata for each context
// ------------------------------------------------------------

var appModel = Collect(
    new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;") // never opened
            .Options),
    "App Model (AppDbContext)",
    "The code-first model the application uses. Migrations are generated from this.");

var dbModel = Collect(
    new ITcomplianceDBContext(
        new DbContextOptionsBuilder<ITcomplianceDBContext>()
            .UseSqlServer("Server=unused;Database=unused;") // never opened
            .Options),
    "Real Database (ITcomplianceDBContext)",
    "Scaffolded from the live ITcomplianceDB - this is what actually exists in SQL Server.");

List<EntityInfo> Collect(DbContext ctx, string title, string subtitle)
{
    var list = new List<EntityInfo>();

    foreach (var et in ctx.Model.GetEntityTypes())
    {
        var info = new EntityInfo
        {
            ClrName = et.ClrType.Name,
            StoreName = et.GetTableName() ?? et.GetViewName() ?? et.ClrType.Name,
            IsView = et.GetViewName() != null
        };

        foreach (var prop in et.GetProperties())
        {
            info.Columns.Add(new ColumnInfo
            {
                Name = prop.Name,
                ClrType = prop.ClrType.Name,
                IsPrimaryKey = prop.IsPrimaryKey(),
                IsNullable = prop.IsColumnNullable()
            });
        }

        foreach (var fk in et.GetForeignKeys())
        {
            info.ForeignKeys.Add(
                $"{et.ClrType.Name} -> " +
                $"{fk.PrincipalEntityType.ClrType.Name} " +
                $"({string.Join(", ", fk.Properties.Select(p => p.Name))})");
        }

        list.Add(info);
    }

    list.Sort((a, b) =>
        string.Compare(a.StoreName, b.StoreName, StringComparison.OrdinalIgnoreCase));

    Console.WriteLine($"{title}: {list.Count} entities");
    return list;
}

// ------------------------------------------------------------
// Write DGML per context
// ------------------------------------------------------------

void WriteDgml(List<EntityInfo> entities, string file)
{
    var dgml = new StringBuilder();

    dgml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    dgml.AppendLine("<DirectedGraph xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\">");
    dgml.AppendLine("  <Nodes>");

    foreach (var e in entities)
    {
        var tooltip = string.Join(
            "&#10;",
            e.Columns.Select(c =>
                (c.IsPrimaryKey ? "[PK] " : "") +
                c.Name + " : " + c.ClrType +
                (c.IsNullable ? " ?" : "")));

        dgml.AppendLine(
            $"    <Node Id=\"{e.ClrName}\" " +
            $"Label=\"{e.StoreName}{(e.IsView ? " (view)" : "")}\" " +
            $"ToolTip=\"{tooltip}\" />");
    }

    dgml.AppendLine("  </Nodes>");
    dgml.AppendLine("  <Links>");

    foreach (var e in entities)
    {
        foreach (var fk in e.ForeignKeys)
        {
            var target = fk.Split(" -> ")[1].Split(' ')[0];
            dgml.AppendLine(
                $"    <Link Source=\"{e.ClrName}\" Target=\"{target}\" Label=\"FK\" />");
        }
    }

    dgml.AppendLine("  </Links>");
    dgml.AppendLine("</DirectedGraph>");

    File.WriteAllText(file, dgml.ToString());
    Console.WriteLine($"{file} -> {Path.GetFullPath(file)}");
}

// ------------------------------------------------------------
// Write ITcomplianceDB.dbml (LINQ to SQL designer file)
// for the tools/DbViewer Framework 4.8 companion project
// + ModelDiagram.html for the browser view
// ------------------------------------------------------------

WriteDbml(dbModel, Path.Combine("tools", "DbViewer", "ITcomplianceDB.dbml"));

void WriteDbml(List<EntityInfo> entities, string file)
{
    static string SysType(string clrName) => clrName switch
    {
        "Int32" => "System.Int32",
        "Int64" => "System.Int64",
        "Int16" => "System.Int16",
        "String" => "System.String",
        "Boolean" => "System.Boolean",
        "DateTime" => "System.DateTime",
        "Decimal" => "System.Decimal",
        "Double" => "System.Double",
        "Single" => "System.Single",
        "Guid" => "System.Guid",
        "Byte[]" => "System.Byte[]",
        _ => "System." + clrName
    };

    var dbml = new StringBuilder();

    dbml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    dbml.AppendLine("<Database Name=\"ITcomplianceDB\" " +
        "Class=\"ITcomplianceDBDataContext\" " +
        "xmlns=\"http://schemas.microsoft.com/linqtosql/dbml/2007\">");

    foreach (var e in entities)
    {
        // views are modeled as tables in dbml
        dbml.AppendLine(
            $"  <Table Name=\"dbo.{e.StoreName}\" Member=\"{e.ClrName}\">");
        dbml.AppendLine($"    <Type Name=\"{e.ClrName}\">");

        foreach (var c in e.Columns)
        {
            var attrs = new List<string>
            {
                $"Name=\"{c.Name}\"",
                $"Type=\"{SysType(c.ClrType)}\""
            };

            if (c.IsPrimaryKey)
            {
                attrs.Add("IsPrimaryKey=\"true\"");
                attrs.Add("CanBeNull=\"false\"");
            }
            else if (c.IsNullable)
            {
                attrs.Add("CanBeNull=\"true\"");
            }
            else
            {
                attrs.Add("CanBeNull=\"false\"");
            }

            dbml.AppendLine(
                "      <Column " + string.Join(" ", attrs) + " />");
        }

        dbml.AppendLine("    </Type>");
        dbml.AppendLine("  </Table>");
    }

    dbml.AppendLine("</Database>");

    Directory.CreateDirectory(
        Path.GetDirectoryName(Path.GetFullPath(file))!);

    File.WriteAllText(file, dbml.ToString());
    Console.WriteLine($"{file} -> {Path.GetFullPath(file)}");
}

// ------------------------------------------------------------
// Write combined HTML
// ------------------------------------------------------------

var html = new StringBuilder();

html.AppendLine("<!DOCTYPE html>");
html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\" />");
html.AppendLine("<title>DB Model - IT Compliance Portal</title>");
html.AppendLine("<style>");
html.AppendLine("body{font-family:'Segoe UI',Arial,sans-serif;background:#f4f5f7;margin:0;padding:32px;color:#111827;}");
html.AppendLine("h1{font-size:22px;margin:0 0 4px 0;} .sub{color:#6b7280;font-size:13.5px;margin-bottom:28px;}");
html.AppendLine("h2.sec{font-size:17px;margin:34px 0 6px 0;padding-bottom:8px;border-bottom:2px solid #C8102E;}");
html.AppendLine(".secsub{color:#6b7280;font-size:13px;margin-bottom:16px;}");
html.AppendLine(".grid{display:flex;flex-wrap:wrap;gap:22px;align-items:flex-start;}");
html.AppendLine(".entity{background:#fff;border:1px solid #e5e7eb;border-radius:14px;box-shadow:0 1px 2px rgba(16,24,40,.06),0 6px 20px rgba(16,24,40,.06);min-width:270px;max-width:360px;overflow:hidden;}");
html.AppendLine(".entity h3{font-size:15px;margin:0;padding:14px 18px;border-bottom:2px solid #C8102E;background:#fafafa;display:flex;justify-content:space-between;gap:10px;}");
html.AppendLine(".tag{font-size:10.5px;font-weight:700;padding:3px 9px;border-radius:999px;text-transform:uppercase;letter-spacing:.05em;}");
html.AppendLine(".tag-table{background:#dbeafe;color:#1d4ed8;} .tag-view{background:#fef3c7;color:#92400e;}");
html.AppendLine("table{width:100%;border-collapse:collapse;font-size:13px;}");
html.AppendLine("td{padding:7px 18px;border-bottom:1px solid #f3f4f6;vertical-align:top;}");
html.AppendLine("td:first-child{font-weight:600;white-space:nowrap;} td:last-child{color:#6b7280;text-align:right;}");
html.AppendLine(".pk{color:#C8102E;font-weight:800;}");
html.AppendLine(".fks{padding:10px 18px;font-size:12px;color:#6b7280;}");
html.AppendLine(".foot{margin-top:30px;font-size:12px;color:#9ca3af;}");
html.AppendLine("</style></head><body>");

html.AppendLine("<h1>IT Compliance Portal - Database Model</h1>");
html.AppendLine($"<div class=\"sub\">Generated {DateTime.Now:dd MMM yyyy HH:mm} &middot; regenerate with: dotnet run --project tools/ModelDiagram</div>");

void WriteSection(string title, string subtitle, List<EntityInfo> entities)
{
    html.AppendLine($"<h2 class=\"sec\">{title}</h2>");
    html.AppendLine($"<div class=\"secsub\">{subtitle} &middot; {entities.Count} entities</div>");
    html.AppendLine("<div class=\"grid\">");

    foreach (var e in entities)
    {
        html.AppendLine("<div class=\"entity\">");
        html.AppendLine($"<h3>{e.StoreName}<span class=\"tag {(e.IsView ? "tag-view" : "tag-table")}\">{(e.IsView ? "view" : "table")}</span></h3>");
        html.AppendLine("<table>");

        foreach (var c in e.Columns)
        {
            var name = c.IsPrimaryKey
                ? $"<span class=\"pk\" title=\"Primary Key\">&#128273; {c.Name}</span>"
                : c.Name;

            html.AppendLine($"<tr><td>{name}</td><td>{c.ClrType}{(c.IsNullable ? "?" : "")}</td></tr>");
        }

        html.AppendLine("</table>");

        if (e.ForeignKeys.Any())
        {
            html.AppendLine("<div class=\"fks\">FK: " + string.Join("<br/>FK: ", e.ForeignKeys) + "</div>");
        }

        html.AppendLine("</div>");
    }

    html.AppendLine("</div>");
}

WriteSection(
    "Application Model (code-first)",
    "What the app uses - AppDbContext. Migrations are generated from this.",
    appModel);

WriteSection(
    "Real Database (scaffolded from SQL Server)",
    "What actually exists in ITcomplianceDB on 10.100.1.17.",
    dbModel);

html.AppendLine("<div class=\"foot\">IT Department - Internal Portal</div>");
html.AppendLine("</body></html>");

File.WriteAllText("ModelDiagram.html", html.ToString());
Console.WriteLine($"ModelDiagram.html -> {Path.GetFullPath("ModelDiagram.html")}");

class EntityInfo
{
    public string ClrName { get; set; } = "";
    public string StoreName { get; set; } = "";
    public bool IsView { get; set; }
    public List<ColumnInfo> Columns { get; } = new();
    public List<string> ForeignKeys { get; } = new();
}

class ColumnInfo
{
    public string Name { get; set; } = "";
    public string ClrType { get; set; } = "";
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
}
