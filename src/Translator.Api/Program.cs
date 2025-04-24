using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Translator.Application.Services;
using Translator.Core.Interfaces;
using Translator.Infrastructure.Cache;
using Translator.Infrastructure.OpenAI;

// Cargar variables de entorno desde el archivo .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); // Agregar soporte para formatos XML

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agregar caché en memoria
builder.Services.AddMemoryCache();

// Agregar HttpClient para OpenAI
builder.Services.AddHttpClient();

// Configurar opciones
builder.Services.Configure<TranslationOptions>(options => 
{
    // Obtener TTL de caché desde variables de entorno
    if (TimeSpan.TryParse(Environment.GetEnvironmentVariable("TRANSLATION_CACHE_TTL"), out var cacheTTL))
    {
        options.CacheTTL = cacheTTL;
    }
    else
    {
        // Usar valor de configuración o predeterminado
        options.CacheTTL = TimeSpan.Parse(builder.Configuration["Translation:CacheTTL"] ?? "24:00:00");
    }
});

builder.Services.Configure<OpenAIOptions>(options => 
{
    // Priorizar variables de entorno
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                     builder.Configuration["OpenAI:ApiKey"];
    
    options.ModelName = Environment.GetEnvironmentVariable("OPENAI_MODEL_NAME") ?? 
                       builder.Configuration["OpenAI:ModelName"] ?? 
                       "gpt-3.5-turbo";
});

// Registrar servicios de la aplicación
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<ICacheProvider, MemoryCacheProvider>();
builder.Services.AddScoped<IAITranslationClient, OpenAITranslationClient>();

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Manejador global de excepciones
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var errorResponse = new
        {
            StatusCode = context.Response.StatusCode,
            Message = exception?.Message ?? "Ha ocurrido un error inesperado",
            Path = context.Request.Path
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(jsonResponse);
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
