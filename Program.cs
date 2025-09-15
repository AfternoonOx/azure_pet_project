using SmartFeedbackCollector.Models.Configuration;
using SmartFeedbackCollector.Repositories;
using SmartFeedbackCollector.Repositories.Interfaces;
using SmartFeedbackCollector.Services;
using SmartFeedbackCollector.Services.Interfaces;

/// <summary>
/// Główny plik aplikacji, odpowiedzialny za konfigurację i uruchomienie serwera webowego.
/// Definiuje, jakie serwisy są używane, jak aplikacja jest skonfigurowana
/// oraz jak obsługiwane są żądania HTTP.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// --- Konfiguracja serwisów (Dependency Injection) ---

// Dodaje podstawowe serwisy wymagane przez architekturę MVC (Model-View-Controller).
builder.Services.AddControllersWithViews();

// Dodaje wbudowany mechanizm cachowania w pamięci, używany m.in. do przechowywania
// wyników analizy, aby unikać ponownych zapytań do API Azure dla tych samych treści.
builder.Services.AddMemoryCache();

/*
 * Konfiguracja opcji aplikacji z pliku appsettings.json.
 */
builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection("AzureStorage"));
builder.Services.Configure<CognitiveServicesOptions>(
    builder.Configuration.GetSection("CognitiveServices"));
builder.Services.Configure<ContentSafetyOptions>(
    builder.Configuration.GetSection("ContentSafety"));

// Rejestracja serwisów i repozytoriów.

builder.Services.AddSingleton<IFeedbackRepository, CosmosDbFeedbackRepository>();
builder.Services.AddSingleton<ISentimentService, AzureSentimentService>();
builder.Services.AddSingleton<IContentSafetyService, AzureContentSafetyService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// --- Konfiguracja potoku przetwarzania żądań HTTP (Middleware Pipeline) ---

// W środowisku innym niż deweloperskie, konfiguruje globalną obsługę błędów
// i włącza HSTS (HTTP Strict Transport Security) dla większego bezpieczeństwa.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Przekierowuje wszystkie żądania HTTP na HTTPS.
app.UseHttpsRedirection();
// Umożliwia serwowanie plików statycznych z folderu wwwroot (np. CSS, JavaScript, obrazy).
app.UseStaticFiles();

// Włącza mechanizm routingu, który mapuje adresy URL na odpowiednie akcje w kontrolerach.
app.UseRouting();

// Definiuje domyślny wzorzec routingu dla aplikacji MVC.
// Domyślnie, adres URL "/" zostanie obsłużony przez akcję Index w kontrolerze Home.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
