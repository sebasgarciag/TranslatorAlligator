using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Translator.Core.Interfaces;
using Translator.Core.Models;

namespace Translator.Api.Controllers
{
    /// <summary>
    /// Controlador para gestionar solicitudes de traducción.
    /// Acepta y procesa solicitudes de traducción en formatos JSON y XML.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json", "application/xml")]
    [Consumes("application/json", "application/xml")]
    public class TranslateController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<TranslateController> _logger;

        /// <summary>
        /// Constructor para el controlador de traducción.
        /// </summary>
        /// <param name="translationService">Servicio de traducción</param>
        /// <param name="logger">Registrador para mensajes de log</param>
        public TranslateController(
            ITranslationService translationService,
            ILogger<TranslateController> logger)
        {
            _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Traduce texto al idioma de destino especificado.
        /// </summary>
        /// <param name="request">Solicitud de traducción con uno o más elementos a traducir</param>
        /// <returns>Respuesta con los textos traducidos</returns>
        [HttpPost]
        [ProducesResponseType(typeof(TranslationResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
        {
            try
            {
                if (request == null || request.Items == null || request.Items.Count == 0)
                {
                    return BadRequest("La solicitud debe contener al menos un elemento para traducir");
                }

                _logger.LogInformation("Solicitud de traducción recibida con {ItemCount} elementos", request.Items.Count);

                var startTime = DateTime.UtcNow;
                var results = await _translationService.TranslateAsync(request.Items);
                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation("Traducción completada en {Duration} ms", duration.TotalMilliseconds);

                var response = new TranslationResponse
                {
                    Results = results
                };

                // Devuelve en el mismo formato que la solicitud (JSON o XML)
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar solicitud de traducción");
                return StatusCode(500, new ProblemDetails
                {
                    Status = 500,
                    Title = "Error Interno del Servidor",
                    Detail = "Ocurrió un error al procesar la solicitud de traducción."
                });
            }
        }
    }
} 