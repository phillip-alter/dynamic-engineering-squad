/**This action gives you a URL like this: /api/reportassist/suggestions?q=broken
If the input is empty, it returns: []
If the input has text, it asks the service for matches and returns them as JSON. **/

using System;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ReportAssist;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/reportassist")]
    public class ReportAssistApiController : ControllerBase
    {
        private readonly IReportDescriptionSuggestionService _suggestionService;

        public ReportAssistApiController(IReportDescriptionSuggestionService suggestionService)
        {
            _suggestionService = suggestionService;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Ok(Array.Empty<string>());
            }

            var suggestions = await _suggestionService.GetSuggestionsAsync(q, ct);
            return Ok(suggestions);
        }
    }
}