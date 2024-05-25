using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;

internal partial class Program
{
    private const string XmlPath = @"godot.docs-xml\xml";
    private static readonly char[] SliceChars = { '\\', '/' };

    private readonly record struct StructuralText(
            string Title,
            string Content,
            string ModuleName,
            string SubModuleName,
            string SectionName,
            string SubSectionName
        )
    {
        public string GetUrl() =>
            $"https://docs.godotengine.org/zh-cn/4.x/{ModuleName}/{SubModuleName}/{SectionName}.html#{SubSectionName}";

        public string GetContext() => $"""
                                       ### {Title}

                                       {Content}
                                       """;
    }

    private static ConcurrentBag<StructuralText> ParseXmlBlocks()
    {
        var parsedDocumentElements = new ConcurrentBag<StructuralText>();

        var xmlFiles = Directory.GetFiles(XmlPath, "*.xml", SearchOption.AllDirectories);
        
        Parallel.ForEach(xmlFiles, xmlFilePath => ParseXml(xmlFilePath, parsedDocumentElements));
        
        return parsedDocumentElements;
    }

    private static void ParseXml(string xmlFilePath, ConcurrentBag<StructuralText> structuralTexts)
    {
        if (string.Equals(Path.GetFileNameWithoutExtension(xmlFilePath), "index", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        var xDocument = XDocument.Load(xmlFilePath);

        var relativePath = Path.GetRelativePath(XmlPath, xmlFilePath);

        var tokens = relativePath.Split(
            SliceChars,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );

        var moduleName = tokens[0];
        var submoduleName = tokens[1];
        var sectionName = Path.GetFileNameWithoutExtension(relativePath);

        var sections = xDocument
            .Descendants("document")
            .Elements("section");

        foreach (var section in sections)
        {
            ParseNode(
                section,
                structuralTexts,
                string.Empty,
                moduleName,
                submoduleName,
                sectionName
            );
        }
    }

    private static void ParseNode(
        XElement sectionNode,
        ConcurrentBag<StructuralText> structuralTexts,
        string inheritedTitle,
        string moduleName,
        string submoduleName,
        string sectionName
    )
    {
        var contentBuilder = new StringBuilder();
        var subSectionName = sectionNode.Attribute("ids")?.Value ?? "";
        foreach (var titleNode in sectionNode.Elements("title"))
        {
            var title = string.IsNullOrEmpty(inheritedTitle)
                ? titleNode.Value
                : $"{inheritedTitle} / {titleNode.Value}";

            var nodesAfterTitle =
                titleNode
                    .NodesAfterSelf()
                    .OfType<XElement>()
                    .ToArray();

            foreach (var nodeAfterTitle in nodesAfterTitle
                         .TakeWhile(x => x.Name != "title")
                         .Where(x => x.Name != "section"))
            {
                switch (nodeAfterTitle.Name.LocalName)
                {
                    case "enumerated_list":
                        foreach (var listItem in nodeAfterTitle.Elements("list_item"))
                            AppendClean(listItem.Value, contentBuilder);
                        break;
                    case "note":
                        ParseNote(nodeAfterTitle, contentBuilder);
                        break;
                    default:
                        AppendClean(nodeAfterTitle.Value, contentBuilder);
                        break;
                }
            }

            if (contentBuilder.Length != 0)
            {
                structuralTexts.Add(
                    new StructuralText(
                        title,
                        contentBuilder.ToString(),
                        moduleName,
                        submoduleName,
                        sectionName,
                        subSectionName
                    )
                );
                contentBuilder.Clear();
            }

            foreach (var sectionNodesAfterTitle in nodesAfterTitle.Where(x => x.Name == "section"))
            {
                ParseNode(
                    sectionNodesAfterTitle,
                    structuralTexts,
                    title,
                    moduleName,
                    submoduleName,
                    sectionName
                );
            }
        }
    }

    private static void ParseNote(XElement noteNode, StringBuilder stringBuilder)
    {
        foreach (var element in noteNode.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "line_block":
                    foreach (var line in element.Elements("line"))
                        AppendClean(line.Value, stringBuilder, "- {0}");
                    break;
                default:
                    AppendClean(element.Value, stringBuilder);
                    break;
            }
        }
    }

    private static void AppendClean(
        string value,
        StringBuilder stringBuilder,
        [StringSyntax("CompositeFormat")] string format = "{0}"
    )
    {
        var cleaned = string.Join(
                " ",
                value.ReplaceLineEndings("\n").Split(
                    "\n",
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
            )
            .TrimEnd();

        if (string.IsNullOrEmpty(cleaned)) return;

        stringBuilder.AppendLine(string.Format(format, cleaned));
    }
}