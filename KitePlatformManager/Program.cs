using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Linq;
using KitePlatformManager.Configuration;
using KitePlatformManager.Services;
using KitePlatformManager.Models;
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

app.MapGet("/sim/detail/{icc}", async (string icc, KiteClient kite) =>
{
    var detail = await kite.GetSimDetailAsync(icc);
    if (detail.HasValue)
    {
        var model = new GenericModel(detail.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value));
        foreach (var kv in model.Fields)
        {
            WriteJsonElement(kv.Value, kv.Key);
        }
    }
    return Results.Ok();
});

app.MapGet("/sim/list/{pageIndex:int}/{pageSize:int}", async (int pageIndex, int pageSize, KiteClient kite) =>
{
    await foreach (var sim in kite.ListSimsAsync(pageIndex, pageSize))
    {
        var model = new GenericModel(sim.EnumerateObject().ToDictionary(p => p.Name, p => p.Value));
        foreach (var kv in model.Fields)
        {
            WriteJsonElement(kv.Value, kv.Key);
        }
    }
    return Results.Ok();
});

app.MapGet("/subscriptiongroup/list", async (KiteClient kite) =>
{
    var groups = await kite.ListCommercialGroupsAsync();
    foreach (var group in groups)
    {
        var model = new GenericModel(group.EnumerateObject().ToDictionary(p => p.Name, p => p.Value));
        foreach (var kv in model.Fields)
        {
            WriteJsonElement(kv.Value, kv.Key);
        }
    }
    return Results.Ok();
});

app.Run();

static void WriteJsonElement(JsonElement element, string prefix = "")
{
    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            foreach (var p in element.EnumerateObject())
            {
                var next = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}.{p.Name}";
                WriteJsonElement(p.Value, next);
            }
            break;
        case JsonValueKind.Array:
            int i = 0;
            foreach (var item in element.EnumerateArray())
            {
                var next = string.IsNullOrEmpty(prefix) ? $"[{i}]" : $"{prefix}[{i}]";
                WriteJsonElement(item, next);
                i++;
            }
            break;
        default:
            Console.WriteLine($"{prefix}: {element.ToString()}");
            break;
    }
}
