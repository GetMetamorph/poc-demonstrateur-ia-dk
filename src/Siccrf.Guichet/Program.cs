using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Features.Conversation;
using Siccrf.Guichet.Features.Demandes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SessionVerrous>();
builder.Services.Configure<AlbertOptions>(builder.Configuration.GetSection("Albert"));
builder.Services.AddHttpClient("Albert", client => client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
    .RemoveAllLoggers();
builder.Services.AddScoped<AlbertApiClient>();
builder.Services.AddScoped<AlbertCatalogue>();
builder.Services.AddSingleton<ChatPropositionLecteur>();
builder.Services.AddScoped<IChatService, AlbertChatService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Siccrf.Demo";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(2);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CompteDemo>();
builder.Services.AddScoped<BrouillonSession>();
builder.Services.AddSingleton<FicheRegles>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/erreur");

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; style-src 'self'; script-src 'self'; " +
        "img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.Use(async (context, next) =>
{
    var cookie = context.Request.Cookies["Siccrf.Demo"] ?? context.TraceIdentifier;
    var verrou = context.RequestServices.GetRequiredService<SessionVerrous>().Pour(cookie);
    await verrou.WaitAsync(context.RequestAborted);
    try
    {
        // Charger après acquisition, et committer avant libération, pour éviter les états périmés.
        await context.Session.LoadAsync(context.RequestAborted);
        context.Response.Headers.CacheControl = "no-store";
        await next();
        await context.Session.CommitAsync(context.RequestAborted);
    }
    finally
    {
        verrou.Release();
    }
});
app.MapControllers();
app.Run();

public partial class Program;
