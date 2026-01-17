using SmartFeedbackCollector.Models.Exceptions;
using SmartFeedbackCollector.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SmartFeedbackCollector.Middleware
{
    public class ConfigurationErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public ConfigurationErrorMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ConfigurationException ex)
            {
                await HandleConfigurationException(context, ex);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Could not initialize") || ex.Message.Contains("Failed to initialize"))
            {
                var configEx = new ConfigurationException(
                    "Couldn't load configuration. Please check your credentials if they are valid.",
                    new List<string>()
                );
                await HandleConfigurationException(context, configEx);
            }
        }

        private async Task HandleConfigurationException(HttpContext context, ConfigurationException ex)
        {
            Console.WriteLine($"Configuration error caught by middleware: {ex.Message}");
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html";

            var viewModel = new ConfigurationErrorViewModel
            {
                ErrorMessage = ex.UserFriendlyMessage,
                MissingConfigurations = ex.MissingConfigurations,
                IsDevelopment = _env.IsDevelopment(),
                TriedKeyVault = ex.TriedKeyVault,
                TriedAppSettings = ex.TriedAppSettings
            };

            await context.Response.WriteAsync(GenerateErrorHtml(viewModel));
        }

        private string GenerateErrorHtml(ConfigurationErrorViewModel model)
        {
            var technicalDetails = "";
            if (model.IsDevelopment && model.MissingConfigurations.Count > 0)
            {
                var configList = string.Join("", model.MissingConfigurations.Select(c => $"<li><code>{c}</code></li>"));
                var source = model.TriedKeyVault ? "Azure Key Vault → appsettings.json" : "appsettings.json";
                
                technicalDetails = $@"
                    <div class='mt-4'>
                        <h5 class='text-muted mb-3'>Szczegóły techniczne:</h5>
                        <div class='alert alert-warning'>
                            <strong>Brakujące konfiguracje:</strong>
                            <ul class='mt-2 mb-0'>{configList}</ul>
                        </div>
                        <p class='small text-muted'>
                            <i class='bi bi-info-circle me-1'></i>
                            Próbowano pobrać z: {source}
                        </p>
                    </div>";
            }

            var keyVaultCheck = model.TriedKeyVault ? "<li class='mb-2'><i class='bi bi-check-circle text-primary me-2'></i>Sprawdź połączenie z Azure Key Vault</li>" : "";

            return $@"
<!DOCTYPE html>
<html lang='pl'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Błąd Konfiguracji - Inteligentny System Zbierania Opinii</title>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css' rel='stylesheet'>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; }}
        .container {{ margin-top: 3rem; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='row justify-content-center'>
            <div class='col-md-8'>
                <div class='card shadow-lg border-danger'>
                    <div class='card-header bg-danger text-white'>
                        <h3 class='mb-0'>
                            <i class='bi bi-exclamation-triangle-fill me-2'></i>
                            Błąd Konfiguracji
                        </h3>
                    </div>
                    <div class='card-body p-5'>
                        <div class='alert alert-danger' role='alert'>
                            <h4 class='alert-heading'>
                                <i class='bi bi-shield-x me-2'></i>
                                Nie można załadować konfiguracji aplikacji
                            </h4>
                            <hr>
                            <p class='mb-0'>{model.ErrorMessage}</p>
                        </div>

                        <div class='mt-4'>
                            <h5 class='text-muted mb-3'>Co możesz zrobić:</h5>
                            <ul class='list-unstyled'>
                                <li class='mb-2'>
                                    <i class='bi bi-check-circle text-primary me-2'></i>
                                    Sprawdź czy Twoje dane uwierzytelniające są poprawne
                                </li>
                                <li class='mb-2'>
                                    <i class='bi bi-check-circle text-primary me-2'></i>
                                    Upewnij się, że plik appsettings.json zawiera wszystkie wymagane klucze
                                </li>
                                {keyVaultCheck}
                                <li class='mb-2'>
                                    <i class='bi bi-check-circle text-primary me-2'></i>
                                    Skontaktuj się z administratorem systemu
                                </li>
                            </ul>
                        </div>

                        {technicalDetails}

                        <div class='mt-4 text-center'>
                            <a href='/' class='btn btn-primary'>
                                <i class='bi bi-house-door me-2'></i>
                                Powrót do strony głównej
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}
