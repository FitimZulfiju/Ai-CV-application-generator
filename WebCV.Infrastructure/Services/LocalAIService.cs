using WebCV.Domain;
using WebCV.Application.Interfaces;
using WebCV.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace WebCV.Infrastructure.Services;

public class LocalAIService : IAIService
{
    private readonly ILogger<LocalAIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AIModel _model;
    private readonly string _ollamaEndpoint;

    public LocalAIService(AIModel model, ILogger<LocalAIService> logger, HttpClient httpClient, string? ollamaEndpoint = null)
    {
        _model = model;
        _logger = logger;
        _httpClient = httpClient;
        _ollamaEndpoint = ollamaEndpoint ?? "http://localhost:11434/api/generate";
    }

    public async Task<string> GenerateCoverLetterAsync(CandidateProfile profile, JobPosting job)
    {
        var systemPrompt = AISystemPrompts.CoverLetterSystemPrompt;
        var userPrompt = AIPromptBuilder.Build(profile, job);

        _logger.LogInformation("Generating Cover Letter using Local Model: {Model}", _model);

        return await CallOllamaAsync(userPrompt, systemPrompt);
    }

    public async Task<TailoredResumeResult> GenerateTailoredResumeAsync(CandidateProfile profile, JobPosting job)
    {
        var systemPrompt = AISystemPrompts.ResumeTailoringSystemPrompt;
        var userPrompt = AIPromptBuilder.Build(profile, job, isResume: true);

        _logger.LogInformation("Generating Tailored Resume using Local Model: {Model}", _model);

        var jsonResponse = await CallOllamaAsync(userPrompt, systemPrompt, json: true);

        try 
        {
            return AIResponseParser.ParseTailoredResume(jsonResponse, profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse local model response for resume.");
            // Fallback: Return basic profile if parsing fails
            return new TailoredResumeResult 
            { 
                Profile = profile,
                DetectedCompanyName = job.CompanyName,
                DetectedJobTitle = job.Title
            };
        }
    }

    private async Task<string> CallOllamaAsync(string prompt, string? system, bool json = false)
    {
        var request = new OllamaRequest
        {
            Model = _model.ToModelString(),
            Prompt = prompt,
            System = system,
            Stream = false,
            Format = json ? "json" : null,
            Options = new Dictionary<string, object>
            {
                { "temperature", 0.7 }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(_ollamaEndpoint, request);
            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            
            if (ollamaResponse == null || string.IsNullOrWhiteSpace(ollamaResponse.Response))
            {
                throw new InvalidOperationException("Empty response from Ollama.");
            }

            return ollamaResponse.Response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama at {Endpoint}. Ensure Ollama is running.", _ollamaEndpoint);
            return $"Error: Could not connect to local AI (Ollama). Please ensure Ollama is running at {_ollamaEndpoint}. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama.");
            return $"Error: Local generation failed. {ex.Message}";
        }
    }
}
