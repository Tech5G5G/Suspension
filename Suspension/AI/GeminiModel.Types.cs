namespace Suspension.AI;

public partial class GeminiModel
{
    private class Request
    {
        public Request(AIPrompt[] prompts, string systemInstruction)
        {
            Contents = [.. prompts.Select(
                i => new Content { Parts = [new() { Text = i.Content }], Role = i.Role.ToString().ToLower() })];
            SystemInstruction = new() { Parts = [new() { Text = systemInstruction }] };
        }

        [JsonPropertyName("system_instruction")]
        public SystemContent SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public Content[] Contents { get; set; }
    }

    #region Content

    private class Content
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("parts")]
        public Part[] Parts { get; set; }
    }

    private class SystemContent
    {
        [JsonPropertyName("parts")]
        public Part[] Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    #endregion

    #region Response

    private class Response
    {
        [JsonPropertyName("candidates")]
        public Candidate[] Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata UsageMetadata { get; set; }

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; }

        [JsonPropertyName("responseId")]
        public string ResponseId { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    private class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }

        [JsonPropertyName("promptTokensDetails")]
        public PromptTokensDetails[] PromptTokensDetails { get; set; }
    }

    private class PromptTokensDetails
    {
        [JsonPropertyName("modality")]
        public string Modality { get; set; }

        [JsonPropertyName("tokenCount")]
        public int TokenCount { get; set; }
    }

    #endregion
}
