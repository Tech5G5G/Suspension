namespace Suspension.AI;

public static class GeminiExtensions
{
    /// <summary>
    /// Gets the <see cref="string"/> representation of a <see cref="ModelVariant"/> (e.g., <c>gemini-2.5-pro</c>).
    /// </summary>
    /// <param name="variant">The <see cref="ModelVariant"/> to convert.</param>
    public static string GetModelString(this ModelVariant variant) => variant switch
    {
        ModelVariant.Gemini25Pro => "gemini-2.5-pro",

        ModelVariant.Gemini15Pro => "gemini-1.5-pro",
        ModelVariant.Gemini15Pro2 => "gemini-1.5-pro-002",
        ModelVariant.Gemini15ProLatest => "gemini-1.5-pro-latest",

        ModelVariant.Gemini25Flash => "gemini-2.5-flash",

        ModelVariant.Gemini2Flash => "gemini-2.0-flash",
        ModelVariant.Gemini2Flash1 => "gemini-2.0-flash-001",
        ModelVariant.Gemini2FlashLite => "gemini-2.0-flash-lite",
        ModelVariant.Gemini2FlashLite1 => "gemini-2.0-flash-lite-001",

        ModelVariant.Gemini15Flash => "gemini-1.5-flash",
        ModelVariant.Gemini15FlashLatest => "gemini-1.5-flash-latest",
        ModelVariant.Gemini15Flash2 => "gemini-1.5-flash-002",
        ModelVariant.Gemini15Flash8B => "gemini-1.5-flash-8b",
        ModelVariant.Gemini15Flash8BLatest => "gemini-1.5-flash-8b-latest",
        ModelVariant.Gemini15Flash8B1 => "gemini-1.5-flash-8b-001",

        ModelVariant.GeminiProVision => "gemini-pro-vision",
        ModelVariant.Gemini1ProVisionLatest => "gemini-1.0-pro-vision-latest",

        ModelVariant.Gemma31BIT => "gemma-3-1b-it",
        ModelVariant.Gemma34BIT => "gemma-3-4b-it",
        ModelVariant.Gemma312BIT => "gemma-3-12b-it",
        ModelVariant.Gemma327BIT => "gemma-3-27b-it",
        ModelVariant.Gemma3E4BIT => "gemma-3n-e4b-it",
        ModelVariant.Gemma3E2BIT => "gemma-3n-e2b-it",

        ModelVariant.Aqa => "aqa",

        ModelVariant.Embedding1 => "embedding-001",
        ModelVariant.TextEmbedding4 => "text-embedding-004",
        ModelVariant.EmbeddingGecko => "embedding-gecko-001",

        ModelVariant.Gemini25ProPreview0325 => "gemini-2.5-pro-preview-03-25",
        ModelVariant.Gemini25ProPreview0506 => "gemini-2.5-pro-preview-05-06",
        ModelVariant.Gemini25ProPreview0605 => "gemini-2.5-pro-preview-06-05",

        ModelVariant.Gemini25FlashPreview0417 => "gemini-2.5-flash-preview-04-17",
        ModelVariant.Gemini25FlashPreview0520 => "gemini-2.5-flash-preview-05-20",

        ModelVariant.Gemini25FlashLitePreview0617 => "gemini-2.5-flash-lite-preview-06-17",
        ModelVariant.Gemini2FlashLitePreview => "gemini-2.0-flash-lite-preview",
        ModelVariant.Gemini2FlashLitePreview0205 => "gemini-2.0-flash-lite-preview-02-05",

        ModelVariant.Gemini25ProPreviewTTS => "gemini-2.5-pro-preview-tts",
        ModelVariant.Gemini25FlashPreviewTTS => "gemini-2.5-flash-preview-tts",

        ModelVariant.Gemini25FlashPreview0417Thinking => "gemini-2.5-flash-preview-04-17-thinking",

        ModelVariant.Gemini2FlashPreviewImageGeneration => "gemini-2.0-flash-preview-image-generation",

        ModelVariant.Gemini2ProExp => "gemini-2.0-pro-exp",
        ModelVariant.Gemini2ProExp0205 => "gemini-2.0-pro-exp-02-05",

        ModelVariant.Gemini2FlashGemini25Exp => "gemini-2.0-flash-Gemini25exp",

        ModelVariant.Gemini2FlashThinkingExp => "gemini-2.0-flash-thinking-exp",
        ModelVariant.Gemini2FlashThinkingExp0121 => "gemini-2.0-flash-thinking-exp-01-21",
        ModelVariant.Gemini2FlashThinkingExp1219 => "gemini-2.0-flash-thinking-exp-1219",

        ModelVariant.Gemini2FlashExpImageGeneration => "gemini-2.0-flash-exp-image-generation",

        ModelVariant.LearnLM2FlashExperimental => "learnlm-2.0-flash-experimental",

        ModelVariant.GeminiEmbeddingExp => "gemini-embedding-exp",
        ModelVariant.GeminiEmbeddingExp0307 => "gemini-embedding-exp-03-07",

        ModelVariant.GeminiExp1206 => "gemini-exp-1206",

        _ => null
    };
}
