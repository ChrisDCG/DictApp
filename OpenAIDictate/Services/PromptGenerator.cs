using System.Text;
using System.Text.Json;
using OpenAIDictate.Models;

namespace OpenAIDictate.Services;

/// <summary>
/// Generates optimized prompts for Whisper/gpt-4o-transcribe using GPT-4o-mini
/// Based on OpenAI Cookbook: https://cookbook.openai.com/examples/whisper_prompting_guide
/// </summary>
public class PromptGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly AppConfig _config;
    private readonly IAppLogger _logger;

    // Cache for generated prompts to avoid redundant API calls
    // Limited to prevent unbounded memory growth
    private const int MaxCacheSize = 100;
    private static readonly Dictionary<string, string> _promptCache = new();
    private static readonly object _cacheLock = new();

    public PromptGenerator(string apiKey, AppConfig config, IAppLogger? logger = null)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? new SerilogLogger();

        _httpClient = OpenAIHttpClientFactory.Create(TimeSpan.FromSeconds(30));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    /// <summary>
    /// Generates a fictitious prompt from an instruction using GPT-4o-mini
    /// OpenAI Cookbook pattern: Use GPT to create realistic example transcripts
    /// </summary>
    public async Task<string> GenerateFictitiousPromptAsync(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return string.Empty;
        }

        // Check cache first
        lock (_cacheLock)
        {
            if (_promptCache.TryGetValue(instruction, out string? cachedPrompt))
            {
                _logger.LogInfo("Using cached GPT-generated prompt");
                return cachedPrompt;
            }
        }

        try
        {
            _logger.LogInfo("Generating fictitious prompt for: {Instruction}", instruction);

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Generate one long paragraph of fictional conversation or professional text " +
                                  "that demonstrates the desired style, terminology, and formatting. " +
                                  "The text should be realistic, natural, and include the specified characteristics. " +
                                  "Do not include meta-commentary, just generate the example text."
                    },
                    new
                    {
                        role = "user",
                        content = instruction
                    }
                },
                temperature = 0.7, // Slightly creative for natural examples
                max_tokens = 500
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to generate prompt: HTTP {StatusCode}", (int)response.StatusCode);
                return string.Empty;
            }

            using JsonDocument doc = JsonDocument.Parse(responseBody);
            string? generatedPrompt = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(generatedPrompt))
            {
                return string.Empty;
            }

            // Trim to ~224 tokens (Whisper's limit for prompt consideration)
            // Rough estimate: 224 tokens ≈ 1000 characters
            if (generatedPrompt.Length > 1000)
            {
                generatedPrompt = generatedPrompt.Substring(generatedPrompt.Length - 1000);
            }

            // Cache the result (with size limit to prevent unbounded growth)
            lock (_cacheLock)
            {
                // If cache is full, clear it to prevent unbounded growth
                // Simple strategy: clear all when limit reached (alternative: LRU eviction)
                if (_promptCache.Count >= MaxCacheSize)
                {
                    _promptCache.Clear();
                    _logger.LogInfo("Prompt cache cleared (limit of {MaxCacheSize} reached)", MaxCacheSize);
                }

                _promptCache[instruction] = generatedPrompt;
            }

            _logger.LogInfo("Generated prompt ({Length} chars)", generatedPrompt.Length);
            return generatedPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating prompt: {Message}", ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Builds an optimized prompt combining multiple strategies from OpenAI Cookbook
    /// </summary>
    public async Task<string> BuildOptimizedPromptAsync()
    {
        var promptParts = new List<string>();

        // 1. Custom Glossary (if specified)
        if (!string.IsNullOrWhiteSpace(_config.Glossary))
        {
            // OpenAI Best Practice: Use natural sentences instead of lists
            // "Aimee and Shawn ate whisky at a BBQ" > "Glossary: Aimee, Shawn, BBQ"
            string glossaryPrompt = BuildGlossarySentence(_config.Glossary);
            promptParts.Add(glossaryPrompt);
        }

        // 2. GPT-Generated Contextual Example (based on language)
        string contextInstruction = _config.Language switch
        {
            "de" => "Generate a professional German business text with legal and formal terminology, " +
                    "including proper punctuation, capitalization, and paragraph symbols (§). " +
                    "Include references to legal codes like BGB, court decisions, and formal business language.",
            "en" => "Generate a professional English business text with legal and formal terminology, " +
                    "including proper punctuation, capitalization, and formal business language.",
            _ => "Generate a professional text with proper punctuation and formal language."
        };

        string gptGeneratedContext = await GenerateFictitiousPromptAsync(contextInstruction);
        if (!string.IsNullOrWhiteSpace(gptGeneratedContext))
        {
            promptParts.Add(gptGeneratedContext);
        }

        // 3. Combine all parts
        string fullPrompt = string.Join(" ", promptParts);

        // 4. Ensure we stay within Whisper's 224-token limit (last tokens are used)
        if (fullPrompt.Length > 1000)
        {
            fullPrompt = fullPrompt.Substring(fullPrompt.Length - 1000);
        }

        return fullPrompt;
    }

    /// <summary>
    /// Converts glossary list into natural sentences (OpenAI best practice)
    /// </summary>
    private string BuildGlossarySentence(string glossary)
    {
        // Split by common delimiters
        var terms = glossary.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim())
                           .Where(t => !string.IsNullOrWhiteSpace(t))
                           .ToList();

        if (terms.Count == 0)
        {
            return string.Empty;
        }

        if (terms.Count == 1)
        {
            return $"Wichtiger Begriff: {terms[0]}.";
        }

        if (terms.Count == 2)
        {
            return $"{terms[0]} und {terms[1]}.";
        }

        // For multiple terms, create a natural sentence
        string lastTerm = terms.Last();
        string otherTerms = string.Join(", ", terms.Take(terms.Count - 1));

        return _config.Language == "de"
            ? $"Fachbegriffe: {otherTerms} und {lastTerm}."
            : $"Technical terms: {otherTerms} and {lastTerm}.";
    }

    /// <summary>
    /// Clears the prompt cache (useful after config changes)
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _promptCache.Clear();
            // Note: Static method, can't use instance logger - use static Logger for compatibility
            Logger.LogInfo("Prompt cache cleared");
        }
    }
}
