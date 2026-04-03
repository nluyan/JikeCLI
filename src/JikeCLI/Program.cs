using ConsoleAppFramework;
using JikeCLI.Commands;
using JikeCLI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var app = ConsoleApp.Create()
    .ConfigureServices(services =>
    {
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(JikeApiClient.DefaultBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        });
        services.AddSingleton<JikeApiClient>();
        services.AddSingleton<JikeConfigStore>();
    });

app.Add<LoginCommands>();
app.Add<FileCommands>("file");
app.Add<OrderCommands>("order");

await app.RunAsync(args);
