using System;
using System.Threading.Tasks;

namespace Translator.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el proveedor de caché.
    /// Define operaciones asíncronas para almacenar y recuperar datos de la caché.
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// Obtiene un valor de la caché de forma asíncrona.
        /// </summary>
        /// <typeparam name="T">Tipo del valor a recuperar</typeparam>
        /// <param name="key">Clave única para identificar el valor en la caché</param>
        /// <returns>El valor almacenado o null si no existe</returns>
        Task<T> GetAsync<T>(string key);

        /// <summary>
        /// Guarda un valor en la caché de forma asíncrona.
        /// </summary>
        /// <typeparam name="T">Tipo del valor a almacenar</typeparam>
        /// <param name="key">Clave única para identificar el valor en la caché</param>
        /// <param name="value">Valor a almacenar</param>
        /// <param name="expiry">Tiempo de expiración opcional (TTL)</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    }
} 