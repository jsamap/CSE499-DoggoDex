using DoggoDex.Web.Api;
using DoggoDex.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Profile data source: mocks by default, the real API once DOG-23 / DOG-24 are merged
// (set Api:UseMocks=false and Api:BaseUrl). Both emit the same DoggoDex.Shared DTOs.
if (builder.Configuration.GetValue("Api:UseMocks", true))
{
    builder.Services.AddSingleton<IProfileApi, MockProfileApi>();
}
else
{
    builder.Services.AddHttpClient<IProfileApi, HttpProfileApi>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5099/"));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();