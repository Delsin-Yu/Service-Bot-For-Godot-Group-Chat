using System.Diagnostics.CodeAnalysis;
using ServiceBotForGodotGroupChat.Services.GodotDocs;

namespace ServiceBotForGodotGroupChat;

public class CommandHandler
{
    private readonly Dictionary<string, IServiceProvider> _services;
    private readonly string[] _serviceHeaders;
    
    public CommandHandler()
    {
        IServiceProvider[] services =
        [
            new GodotDocsServiceProvider()
        ];
        _services = services.ToDictionary(x => x.ServiceHeader, x => x);
        _serviceHeaders = _services.Keys.ToArray();
    }

    public void MatchCommand(
        string userMessage,
        [NotNullWhen(true)] out string? serviceKey,
        out ReadOnlyMemory<char> command
    )
    {
        var userMessageMemory = userMessage.AsMemory();
        var userMessageSpan = userMessageMemory.Span.Trim();
        command = null;
        serviceKey = null;
        foreach (var serviceHeader in _serviceHeaders.AsSpan())
        {
            if (!userMessageSpan.StartsWith(serviceHeader)) continue;
            serviceKey = serviceHeader;
            command = userMessageMemory[(serviceHeader.Length + 1)..];
            command = command.Trim();
            return;
        }

        command = null;
    }

    public Task<ReadOnlyMemory<char>> ProcessCommand(string serviceKey, ReadOnlyMemory<char> command) => 
        !_services.TryGetValue(serviceKey, out var service) ? 
            Task.FromResult<ReadOnlyMemory<char>>(null) : 
            service.ProcessServiceAsync(command);
}