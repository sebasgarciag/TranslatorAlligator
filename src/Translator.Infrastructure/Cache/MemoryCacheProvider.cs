using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Translator.Core.Interfaces;

namespace Translator.Infrastructure.Cache
{
    /// <summary>
    /// Implementación del proveedor de caché que utiliza IMemoryCache de ASP.NET Core.
    /// Almacena valores en la memoria del proceso de la aplicación.
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheProvider> _logger;

        /// <summary>
        /// Constructor para el proveedor de caché en memoria.
        /// </summary>
        /// <param name="cache">Instancia de IMemoryCache</param>
        /// <param name="logger">Registrador para mensajes de log</param>
        public MemoryCacheProvider(IMemoryCache cache, ILogger<MemoryCacheProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene un valor de la caché de forma asíncrona.
        /// </summary>
        /// <typeparam name="T">Tipo del valor a recuperar</typeparam>
        /// <param name="key">Clave única para identificar el valor en la caché</param>
        /// <returns>El valor almacenado o default(T) si no existe</returns>
        public Task<T> GetAsync<T>(string key)
        {
            try
            {
                if (_cache.TryGetValue(key, out T value))
                {
                    return Task.FromResult(value);
                }
                
                return Task.FromResult<T>(default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar valor de caché para clave: {Key}", key);
                return Task.FromResult<T>(default);
            }
        }

        /// <summary>
        /// Guarda un valor en la caché de forma asíncrona.
        /// </summary>
        /// <typeparam name="T">Tipo del valor a almacenar</typeparam>
        /// <param name="key">Clave única para identificar el valor en la caché</param>
        /// <param name="value">Valor a almacenar</param>
        /// <param name="expiry">Tiempo de expiración opcional (TTL)</param>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromHours(24)
                };

                _cache.Set(key, value, options);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al establecer valor en caché para clave: {Key}", key);
                return Task.CompletedTask;
            }
        }
    }
} 