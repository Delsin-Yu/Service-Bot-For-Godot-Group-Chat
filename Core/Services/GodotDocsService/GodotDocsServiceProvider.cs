using Qdrant.Client;

namespace ServiceBotForGodotGroupChat.Services.GodotDocs;

public class GodotDocsServiceProvider : IServiceProvider
{
    private readonly BGEM3Bridge _bridge = new();
    private readonly QdrantClient _client = new("localhost");
    private const string CollectionName = "godot_docs";

    public string ServiceHeader => "Doc";
    
    public async Task<ReadOnlyMemory<char>> ProcessServiceAsync(ReadOnlyMemory<char> command)
    {
        var embedding = await _bridge.Embedding(command.ToString());
        var matched = await _client.SearchAsync(CollectionName, embedding);
        if (matched.Count == 0) return "暂未找到符合的结果。".AsMemory();

        var payload = matched[0].Payload;


        var document = payload["Document"].StringValue;
        var url = payload["Url"].StringValue;
        
        var message = $"""
                       {document.ReplaceLineEndings("\n").Split("\n")[0]}
                       {url}
                       """;

        return message.AsMemory();
    }
}