using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TourMap.AdminWeb.Services;

public interface IAITranslationService
{
    Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "vi");
    Task<string> GenerateTtsAudioAsync(string text, string languageCode, string wwwRootPath);
}

public class AITranslationService : IAITranslationService
{
    private readonly HttpClient _httpClient;

    public AITranslationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "vi")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Use Google Translate free API
        string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
        
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            // The response is a nested JSON array: [[["TranslatedText","SourceText",null,null,1]],null,"vi",...]
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var sentences = root[0];
                string result = "";
                foreach (var sentence in sentences.EnumerateArray())
                {
                    if (sentence.ValueKind == JsonValueKind.Array && sentence.GetArrayLength() > 0)
                    {
                        var segment = sentence[0].GetString();
                        if (segment != null) result += segment;
                    }
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Translation Error: {ex.Message}");
        }

        return string.Empty;
    }

    public async Task<string> GenerateTtsAudioAsync(string text, string languageCode, string wwwRootPath)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        
        // Truncate text if needed (Google TTS allows up to 200 chars per request usually, but tw-ob often accepts more)
        if (text.Length > 200) text = text.Substring(0, 200);

        string url = $"https://translate.googleapis.com/translate_tts?ie=UTF-8&client=tw-ob&tl={languageCode}&q={Uri.EscapeDataString(text)}";
        
        try
        {
            var responseBytes = await _httpClient.GetByteArrayAsync(url);
            
            string audioFolder = Path.Combine(wwwRootPath, "uploads", "audio", "ai");
            Directory.CreateDirectory(audioFolder);
            
            string fileName = $"tts_{languageCode}_{Guid.NewGuid().ToString().Substring(0,8)}.mp3";
            string filePath = Path.Combine(audioFolder, fileName);
            
            await File.WriteAllBytesAsync(filePath, responseBytes);
            return $"/uploads/audio/ai/{fileName}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TTS Error: {ex.Message}");
        }

        return string.Empty;
    }
}
