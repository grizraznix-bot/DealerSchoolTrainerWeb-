using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DealerSchoolTrainerWeb;
using DealerSchoolTrainerWeb.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Menu -> current game "go back to your start screen" signal
builder.Services.AddScoped<GameResetService>();

await builder.Build().RunAsync();
