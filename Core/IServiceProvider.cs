namespace ServiceBotForGodotGroupChat;

public interface IServiceProvider
{
    string ServiceHeader { get; }
    Task<ReadOnlyMemory<char>> ProcessServiceAsync(ReadOnlyMemory<char> command);
}