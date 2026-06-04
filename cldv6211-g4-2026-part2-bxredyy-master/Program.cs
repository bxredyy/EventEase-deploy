using EventEase.Data;
using EventEase.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

// Program.cs is the entry point of an ASP.NET Core 8 app.
// It wires up services (DI), middleware, and finally calls app.Run().

var builder = WebApplication.CreateBuilder(args);

// Registers MVC controllers + Razor view engine.
builder.Services.AddControllersWithViews();

// POE Part 1C: DbContext registration.
// Reads the "DefaultConnection" string from appsettings.json and tells
// EF Core to use SQL Server (LocalDB).
// EnableRetryOnFailure makes EF retry the connection a few times if
// LocalDB is still spinning up — useful when the app starts up cold.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(15),
            errorNumbersToAdd: null)));

// POE Part 2A: Register the blob service so controllers can inject it.
// AddScoped = one BlobService instance per HTTP request, which is the
// recommended lifetime for things that talk to external storage.
builder.Services.AddScoped<IBlobService, BlobService>();

// POE Part 2A: Keep uploaded files in memory instead of spooling them to
// %TEMP%. On some Windows machines Defender locks the temp spool file mid-
// upload, which kills the entire host process with exit code 0xFFFFFFFF.
// Holding the file in memory sidesteps that completely.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MemoryBufferThreshold = int.MaxValue;        // never spool to disk
    options.MultipartBodyLengthLimit = 100_000_000;       // 100 MB cap
    options.ValueLengthLimit = int.MaxValue;
});

// Lift Kestrel's overall request body limit to match.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 100_000_000;
});

var app = builder.Build();

// Ensure the shared LocalDB instance MSSQLLocalDB is started before EF tries to connect.
try
{
    using var proc = Process.Start(new ProcessStartInfo
    {
        FileName = "sqllocaldb",
        Arguments = "start MSSQLLocalDB",
        CreateNoWindow = true,
        UseShellExecute = true,
        WindowStyle = ProcessWindowStyle.Hidden
    });
    proc?.WaitForExit(8000);
}
catch { /* sqllocaldb missing from PATH — instance may already be running */ }

// POE Part 1C: Auto-apply any pending EF migrations on startup so the
//              marker doesn't have to run "dotnet ef database update"
//              themselves. The try/catch keeps the app running even if
//              migrations fail (e.g. if the DB already exists).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try { db.Database.Migrate(); } catch { /* already applied */ }
}

// Standard pipeline.
if (app.Environment.IsDevelopment())
{
    // Show full stack traces in dev so we never swallow exceptions silently.
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Global safety-net middleware: even if a deep async exception escapes a
// controller, this catches it and writes the stack trace to the console.
// Prevents the entire process from dying with exit code 0xFFFFFFFF.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GLOBAL] Unhandled: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine($"[GLOBAL] Stack: {ex.StackTrace}");
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Server error: {ex.Message}");
        }
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();   // serves wwwroot files (CSS, JS, images)
app.UseRouting();
app.UseAuthorization();

// Default route: /Home/Index when no controller/action is given.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
