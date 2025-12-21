using System.Net.Http;
using Traceability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sample.WebApi.Net8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApiController> _logger;

        public ApiController(IHttpClientFactory httpClientFactory, ILogger<ApiController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            // O correlation-id está automaticamente disponível no contexto
            var correlationId = CorrelationContext.Current;
            
            _logger.LogInformation("Iniciando chamada externa com CorrelationId: {CorrelationId}", correlationId);

            // O HttpClient criado via factory automaticamente inclui o correlation-id no header
            var httpClient = _httpClientFactory.CreateClient("ExternalApi");
            
            try
            {
                var response = await httpClient.GetAsync("posts/1");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Chamada externa concluída com sucesso. CorrelationId: {CorrelationId}", correlationId);
                
                return Ok(new
                {
                    CorrelationId = correlationId,
                    Message = "Chamada externa realizada com sucesso",
                    ExternalResponse = content
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar chamada externa. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, new
                {
                    CorrelationId = correlationId,
                    Error = ex.Message
                });
            }
        }

        [HttpGet("correlation-id")]
        public IActionResult GetCorrelationId()
        {
            var correlationId = CorrelationContext.Current;
            
            return Ok(new
            {
                CorrelationId = correlationId,
                Message = "Correlation ID obtido do contexto atual"
            });
        }
    }
}

