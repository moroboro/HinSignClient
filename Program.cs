// See https://aka.ms/new-console-template for more information

using HinQesSignDemo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true);

builder.Services.Configure<AppOptions>(builder.Configuration.GetRequiredSection(AppOptions.ConfigurationSectionName));

builder.WebHost.ConfigureKestrel(kestrelServerOptions =>
{
    var appOptions = builder.Configuration.GetSection(AppOptions.ConfigurationSectionName).Get<AppOptions>()!;
    kestrelServerOptions.Configure().LocalhostEndpoint(appOptions.AppPort);
});

builder.Services.AddHttpClient<ICertifactionClient, CertifactionClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;
    client.BaseAddress = options.Certifaction.ContainerHost;
});

var app = builder.Build();

var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;

Console.WriteLine($"Starting - {appOptions.AppName}");

app.MapPost("callback", Callback.Handle);

app.MapGet("batch", SignBatch.Handle);
app.MapGet("single", SignSingle.Handle);

app.Run();