using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace Suspension.AI;

/// <summary>
/// Represents a Google Gemini AI model.
/// </summary>
/// <param name="key">An API key for the Gemini API.</param>
/// <param name="variant">The <see cref="ModelVariant"/> of the <see cref="Model"/>.</param>
public partial class Model(string key, ModelVariant variant)
{
    /// <summary>
    /// Gets the API key used by the <see cref="Model"/>.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the <see cref="ModelVariant"/> of the <see cref="Model"/>.
    /// </summary>
    public ModelVariant Variant { get; } = variant;

    /// <summary>
    /// Attempts to make a request to the specified <see cref="Variant"/>.
    /// </summary>
    /// <param name="request">The <see cref="AIRequest"/> to present to the <see cref="Variant"/>.</param>
    /// <returns>If no errors occur (e.g., timeout, rate limit), an <see cref="AIResponse"/> from the <see cref="Variant"/>. Otherwise, <see langword="null"/>.</returns>
    public async Task<AIResponse> TryMakeRequest(AIRequest request)
    {
        HttpClient client = new()
        {
            BaseAddress = new("https://generativelanguage.googleapis.com"),
            Timeout = TimeSpan.FromMinutes(1)
        };

        HttpResponseMessage message;
        try
        {
            message = await client.SendAsync(
            new(HttpMethod.Post, $"/v1beta/models/{Variant.GetModelString()}:generateContent")
            {
                Content = new StringContent(
                JsonSerializer.Serialize(new Request(request)),
                Encoding.UTF8,
                "application/json"),
                Headers =
                {
                    { "x-goog-api-key", Key }
                }
            });
        }
        catch
        {
            return null;
        }

        if (!message.IsSuccessStatusCode || await message.Content.ReadAsStringAsync() is not string responseJson)
            return null;

        var response = JsonSerializer.Deserialize<Response>(responseJson);
        var metadata = response.UsageMetadata;

        return new(
            [.. response.Candidates.Select(i =>
                new AI.Candidate(
                    string.Join('\n', i.Content.Parts.Select(i => i.Text)),
                    i.FinishReason))],
            response.ResponseId,
            new(
                metadata.PromptTokenCount,
                metadata.CandidatesTokenCount,
                metadata.TotalTokenCount,
                [.. metadata.PromptTokensDetails.Select(i =>
                    new AI.PromptTokensDetails(i.Modality, i.TokenCount))]));
    }
}

/// <summary>
/// Defines the variants available to be used by the <see cref="Model.Variant"/> property.
/// </summary>
public enum ModelVariant
{
    #region Release

    Gemini25Pro,

    Gemini15Pro,
    Gemini15Pro2,
    Gemini15ProLatest,

    Gemini25Flash,

    Gemini2Flash,
    Gemini2Flash1,
    Gemini2FlashLite,
    Gemini2FlashLite1,

    Gemini15Flash,
    Gemini15FlashLatest,
    Gemini15Flash2,
    Gemini15Flash8B,
    Gemini15Flash8BLatest,
    Gemini15Flash8B1,

    GeminiProVision,
    Gemini1ProVisionLatest,

    Gemma31BIT,
    Gemma34BIT,
    Gemma312BIT,
    Gemma327BIT,
    Gemma3E4BIT,
    Gemma3E2BIT,

    Aqa,

    Embedding1,
    TextEmbedding4,
    EmbeddingGecko,

    #endregion

    #region Preview

    Gemini25ProPreview0325,
    Gemini25ProPreview0506,
    Gemini25ProPreview0605,

    Gemini25FlashPreview0417,
    Gemini25FlashPreview0520,

    Gemini25FlashLitePreview0617,
    Gemini2FlashLitePreview,
    Gemini2FlashLitePreview0205,

    Gemini25ProPreviewTTS,
    Gemini25FlashPreviewTTS,

    Gemini25FlashPreview0417Thinking,

    Gemini2FlashPreviewImageGeneration,

    #endregion

    #region Experimental

    Gemini2ProExp,
    Gemini2ProExp0205,

    Gemini2FlashGemini25Exp,

    Gemini2FlashThinkingExp,
    Gemini2FlashThinkingExp0121,
    Gemini2FlashThinkingExp1219,

    Gemini2FlashExpImageGeneration,

    LearnLM2FlashExperimental,

    GeminiEmbeddingExp,
    GeminiEmbeddingExp0307,

    GeminiExp1206

    #endregion
}
