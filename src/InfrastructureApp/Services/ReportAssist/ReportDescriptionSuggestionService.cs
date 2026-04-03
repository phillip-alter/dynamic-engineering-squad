//implementation of the IReportDescriptionSuggestionService

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace InfrastructureApp.Services.ReportAssist
{
    public sealed class ReportDescriptionSuggestionService : IReportDescriptionSuggestionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly Lazy<List<string>> _suggestions;

        public ReportDescriptionSuggestionService(IWebHostEnvironment env)
        {
            _env = env;

            /**do not load the JSON file immediately
            load it only the first time suggestions are actually needed
            then keep the data in memory for reuse**/
            _suggestions = new Lazy<List<string>>(LoadSuggestions);
        }

        //this method returns suggestions when typing in description box
        public Task<IReadOnlyList<string>> GetSuggestionsAsync(string input, CancellationToken ct = default)
        {
            //If the user hasn’t typed anything meaningful, return an empty list.
            if (string.IsNullOrWhiteSpace(input))
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            //This removes extra spaces at the ends.
            string normalizedInput = input.Trim();

            //find the last token - If the user types: There is a brok the last token becomes: brok
            //This helps match suggestions using what the user is currently typing.
            string lastToken = normalizedInput
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? normalizedInput;

            /** This says:
            return suggestions where either:
            the whole input appears in the suggestion, or
            the last typed word appears in the suggestion
            limit the result to 5 to prevent dropdown from getting too long**/
            var matches = _suggestions.Value
                .Where(s =>
                    s.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                    s.Contains(lastToken, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(matches);
        }

        //loads the list of autocomplete suggestions
        private List<string> LoadSuggestions()
        {
            string filePath = Path.Combine(_env.ContentRootPath, "Data", "Moderation", "descriptionSuggestions.json");

            if (!File.Exists(filePath))
            {
                return new List<string>();
            }

            string json = File.ReadAllText(filePath);

            var items = JsonSerializer.Deserialize<List<string>>(json);

            return items ?? new List<string>();
        }
    }
}