using DoggoDex.Api.Auth;
using DoggoDex.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var rawConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
var connectionString = DatabaseUrl.ToNpgsql(rawConnection);

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddDoggoDexAuth(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddControllers();

// --- ADD SWAGGER SERVICES ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- AUTOMATIC MIGRATION & SEEDING ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}

// --- ENABLE SWAGGER MIDDLEWARE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DoggoDex API V1");
        options.RoutePrefix = "swagger";
    });
}

app.MapHealthChecks("/health");
app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
