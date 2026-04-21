using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using SmartCutScheduler.Web;
using SmartCutScheduler.Web.Services;
using SmartCutScheduler.Web.State;
using MudBlazor.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Named HttpClient for the Python AI service
var aiServiceBaseUrl = builder.Configuration["AiServiceBaseUrl"] ?? "http://localhost:8000";
builder.Services.AddHttpClient("AiService", client => client.BaseAddress = new Uri(aiServiceBaseUrl));

// Add MudBlazor services
builder.Services.AddMudServices();

// Add local storage
builder.Services.AddBlazoredLocalStorage();

// Add services
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IBarberService, BarberService>();
builder.Services.AddScoped<IBarberServiceService, BarberServiceService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<NotificationService>();

// Add authorization
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
