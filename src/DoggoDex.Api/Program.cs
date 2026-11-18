using DoggoDex.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var rawConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
var connectionString = DatabaseUrl.ToNpgsql(rawConnection);

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddHealthChecks();
builder.Services.AddControllers();

var app = builder.Build();

// --- AUTOMATIC MIGRATION & SEEDING ON STARTUP ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Automatically apply any pending EF Core migrations
    await dbContext.Database.MigrateAsync();
    // Seed initial test data
    await DbSeeder.SeedAsync(dbContext);
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();