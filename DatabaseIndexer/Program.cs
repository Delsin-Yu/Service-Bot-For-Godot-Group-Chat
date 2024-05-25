using Qdrant.Client;
using Qdrant.Client.Grpc;

internal partial class Program
{
    public static async Task Main()
    {
        var texts =  ParseXmlBlocks().ToArray();
  
        var client = new QdrantClient("localhost");
        
        const string collectionName = "godot_docs";
        
        var collections = await client.ListCollectionsAsync();
        if (collections.Contains(collectionName))
        {
            await client.DeleteCollectionAsync(collectionName);
        }
        
        await client.CreateCollectionAsync(
            collectionName,
            new VectorParams
            {
                Size = 1024,
                Distance = Distance.Cosine
            });
        
        var bridge = new BGEM3Bridge();
        
        Console.WriteLine($"Total Text Blocks: {texts.Length}");
        
        var points = new PointStruct[texts.Length];
        
        for (var index = 0; index < texts.Length; index++)
        {
            var text = texts[index];
            Console.WriteLine($"Indexing({index + 1}/{texts.Length}) {text.Title}");
            var context = text.GetContext();
            var embeddings = await bridge.Embedding(context);
            var pointStruct = new PointStruct()
            {
                Id = (ulong)index,
                Vectors = embeddings.ToArray(),
                Payload =
                {
                    ["Document"] = context.Trim(),
                    ["Url"] = text.GetUrl(),
                }
            };
            points[index] = pointStruct;
        }
        
        await client.UpsertAsync(collectionName, points);
    }
}