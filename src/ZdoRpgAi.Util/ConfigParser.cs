using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ZdoRpgAi.Util;

public static class ConfigParser {
    public static T ParseYamlFile<T>(string path, JsonTypeInfo<T> jsonTypeInfo) {
        var fullPath = Path.GetFullPath(path);
        string yaml;
        try {
            yaml = File.ReadAllText(fullPath);
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Failed to read config: {fullPath} ({ex.Message})");
            Environment.Exit(1);
            return default!;
        }

        try {
            var jsonNode = YamlToJsonTransformer.Parse(yaml);
            var json = jsonNode.ToJsonString();
            var config = JsonSerializer.Deserialize(json, jsonTypeInfo);
            if (config == null) {
                Console.Error.WriteLine($"Failed to parse config: {fullPath}");
                Environment.Exit(1);
            }
            return config;
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Invalid config: {fullPath} ({ex.Message})");
            Environment.Exit(1);
            return default!;
        }
    }
}
