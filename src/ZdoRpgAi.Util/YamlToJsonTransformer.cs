using System.Globalization;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace ZdoRpgAi.Util;

public static class YamlToJsonTransformer {
    public static JsonNode Parse(string yaml) {
        using var reader = new StringReader(yaml);
        var stream = new YamlStream();
        stream.Load(reader);

        if (stream.Documents.Count == 0)
            return new JsonObject();

        return ConvertNode(stream.Documents[0].RootNode) ?? new JsonObject();
    }

    private static JsonNode? ConvertNode(YamlNode node) => node switch {
        YamlMappingNode mapping => ConvertMapping(mapping),
        YamlSequenceNode sequence => ConvertSequence(sequence),
        YamlScalarNode scalar => ConvertScalar(scalar),
        _ => null
    };

    private static JsonObject ConvertMapping(YamlMappingNode mapping) {
        var obj = new JsonObject();
        foreach (var (keyNode, valueNode) in mapping.Children) {
            var key = ((YamlScalarNode)keyNode).Value!;
            obj[key] = ConvertNode(valueNode);
        }
        return obj;
    }

    private static JsonArray ConvertSequence(YamlSequenceNode sequence) {
        var arr = new JsonArray();
        foreach (var item in sequence.Children)
            arr.Add(ConvertNode(item));
        return arr;
    }

    private static JsonNode? ConvertScalar(YamlScalarNode scalar) {
        var value = scalar.Value;
        if (value is null) return null;

        // Quoted strings stay as strings
        if (scalar.Style is YamlDotNet.Core.ScalarStyle.SingleQuoted
            or YamlDotNet.Core.ScalarStyle.DoubleQuoted)
            return JsonValue.Create(value);

        if (value is "true" or "True" or "TRUE") return JsonValue.Create(true);
        if (value is "false" or "False" or "FALSE") return JsonValue.Create(false);
        if (value is "null" or "~" or "") return null;

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            return JsonValue.Create(l);
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return JsonValue.Create(d);

        return JsonValue.Create(value);
    }
}
