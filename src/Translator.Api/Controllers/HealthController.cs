using Microsoft.AspNetCore.Mvc;
using System;

namespace Translator.Api.Controllers
{
    /// <summary>
    /// Controlador para verificar el estado de salud de la API.
    /// Utilizado para monitoreo y comprobaciones de disponibilidad.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Devuelve el estado actual del servicio.
        /// </summary>
        /// <returns>Estado de salud del servicio con marca de tiempo</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow
            });
        }
    }
} 