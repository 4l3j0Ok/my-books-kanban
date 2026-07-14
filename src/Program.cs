using Microsoft.EntityFrameworkCore;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Application.Services;
using MyBooksKanban.Application.State;
using MyBooksKanban.Components;
using MyBooksKanban.Infrastructure.Data;

// Por defecto, .NET trata ASPNETCORE_ENVIRONMENT vacío como "Production".
// Para desarrollo local (dotnet run / dotnet watch) queremos "Development"
// para que se sirvan los Static Web Assets (blazor.web.js, CSS scope, etc.)
// sin necesidad de publicar. En Docker el compose fija Production explícitamente.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
 && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=my-books-kanban.db";

builder.Services.AddDbContextFactory<LibraryDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddSingleton<ICoverStorageService, CoverStorageService>();
builder.Services.AddScoped<KanbanState>();

builder.Services.AddLogging();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

// Aplica migraciones y datos seed al arrancar.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LibraryDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    try
    {
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "No se pudo inicializar la base de datos. Continuando para que el endpoint /health responda.");
    }
}

app.Run();
