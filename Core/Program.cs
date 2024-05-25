using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using ServiceBotForGodotGroupChat;

var builder = WebApplication.CreateSlimBuilder(args);

var builderConfiguration = builder.Configuration;
var httpAddress = builderConfiguration.GetValue<string>("HttpAddress")!;
var httpPort = builderConfiguration.GetValue<ushort>("HttpPort");

var app = builder.Build();

var commandHandler = new CommandHandler();

var statusCode204 = Results.StatusCode(204);

app
    .MapGroup("onebot")
    .MapPost("post", async (HttpRequest request) =>
    {
        var jsonElement = (await JsonDocument.ParseAsync(request.Body)).RootElement;
        
        if (!jsonElement.TryGetProperty("self_id", out var selfId)) return statusCode204;
        var selfIdString = selfId.GetInt64().ToString();
        if (!jsonElement.TryGetProperty("post_type", out var postType)) return statusCode204;
        if (postType.GetString() != "message") return statusCode204;
        if (!jsonElement.TryGetProperty("message_type", out var messageType)) return statusCode204;
        if (messageType.GetString() != "group") return statusCode204;
        if (!jsonElement.TryGetProperty("message", out var message)) return statusCode204;
        if(message.ValueKind is not JsonValueKind.Array) return statusCode204;

        ReadOnlyMemory<char> command = null;
        var mentioned = false;
        string? serviceKey = null;
        
        foreach (var arrayElement in message.EnumerateArray())
        {
            if(serviceKey is not null && mentioned) break;
            
            if(!arrayElement.TryGetProperty("type", out var type)) continue;

            switch (type.GetString())
            {
                case "text":
                    if(serviceKey is not null) continue;
                    
                    if (!arrayElement.TryGetProperty("data", out var data)) continue;
                    if (!data.TryGetProperty("text", out var text)) continue;
                    var userMessage = text.GetString();
                    if (userMessage is null) continue;
                    commandHandler.MatchCommand(userMessage, out serviceKey, out command);
                    break;
                case "at":
                    if(mentioned) continue;
                    
                    if (!arrayElement.TryGetProperty("data", out data)) continue;
                    if (!data.TryGetProperty("qq", out var qq)) continue;
                    var qqString = qq.GetString();
                    
                    mentioned = qqString == selfIdString;
                    break;
                default:
                    continue;
            }
        }
        
        if(serviceKey == null || !mentioned) return statusCode204;
        
        var result = await commandHandler.ProcessCommand(serviceKey, command);

        if(Unsafe.IsNullRef(ref result)) return statusCode204;
        
        var response = new JsonObject { { "reply", JsonValue.Create(result.ToString()) } };
        
        return Results.Content(response.ToJsonString(), "application/json");
    });


app.Run($"http://{httpAddress}:{httpPort}");