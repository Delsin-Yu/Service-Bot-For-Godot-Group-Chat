using System.Text.Json;
using System.Text.Json.Serialization;

public partial class BGEM3Bridge
{
    private readonly HttpClient _httpClient = new();
    private const string LocalUrl = "http://127.0.0.1:8888/embedding";

    private readonly Range[] _buffer = new Range[1024];
    private readonly float[] _vectors = new float[1024];

    private record struct SendModel(string Sentence);

    [JsonSerializable(typeof(SendModel)), JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    private partial class SendModelSerializerContext : JsonSerializerContext;
    
    public async Task<ReadOnlyMemory<float>> Embedding(string text)
    {
        var response = await _httpClient.PostAsync(LocalUrl,
            new StringContent(JsonSerializer.Serialize(
                    new SendModel(text),
                    SendModelSerializerContext.Default.SendModel
                )
            )
        );

        var resultString = (await response.Content.ReadAsStringAsync())
            .AsMemory(1..^1);
        
        resultString
            .Span
            .Split(_buffer.AsSpan(), ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < _buffer.Length; index++)
        {
            var range = _buffer[index];
            var segment = resultString[range];
            _vectors[index] = float.Parse(segment.Span);
        }

        return _vectors;
    }
}