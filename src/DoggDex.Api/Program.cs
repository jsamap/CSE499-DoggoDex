using DoggoDex.Api.Infrastructure;

// ... after var builder = WebApplication.CreateBuilder(args);
var rawConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
var connectionString = DatabaseUrl.ToNpgsql(rawConnection);
// builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddHealthChecks();

// ... after var app = builder.Build();
app.MapHealthChecks("/health");
