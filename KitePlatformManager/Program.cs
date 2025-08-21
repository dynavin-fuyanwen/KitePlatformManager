using System.Security.Cryptography.X509Certificates;
using KitePlatformManager.Configuration;
using KitePlatformManager.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KitePlatformOptions>(builder.Configuration.GetSection("KitePlatform"));

builder.Services.AddHttpClient("Kite", (sp, c) =>
{
    var options = sp.GetRequiredService<IOptions<KitePlatformOptions>>().Value;
    c.BaseAddress = new Uri($"{options.BaseUrl}:{options.Port}/");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var options = sp.GetRequiredService<IOptions<KitePlatformOptions>>().Value;
    var cert = new X509Certificate2(options.CertificatePath, options.CertificatePassword,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
    return new HttpClientHandler
    {
        ClientCertificates = { cert },
        SslProtocols = System.Security.Authentication.SslProtocols.Tls12
    };
});

builder.Services.AddScoped<KiteClient>();

var app = builder.Build();

app.MapGet("/demo/run", async (KiteClient kite) =>
{
    await foreach (var s in kite.GetSubscriptionsAsync(lifeCycle: "ACTIVE"))
        Console.WriteLine($"{s.GetProperty(\"icc\").GetString()} - {s.GetProperty(\"lifeCycleStatus\").GetString()}");

    await kite.ModifyLifecycleAsync(icc: "8934072100251262559", targetStatus: "ACTIVE");

    var watcherId = await kite.SendSmsAsync("8934072100251262559", "hello from .NET");
    Console.WriteLine($"SMS watcherId: {watcherId}");

    var groups = await kite.ListCommercialGroupsAsync();
    Console.WriteLine($"Groups count: {groups.Count}");

    return Results.Ok("Done");
});

app.Run();
