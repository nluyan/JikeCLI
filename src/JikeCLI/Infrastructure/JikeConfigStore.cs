using JikeCLI.Models;
using JikeCLI.Serialization;
using System.Text.Json;

namespace JikeCLI.Infrastructure;

public sealed class JikeConfigStore
{
    public string ConfigFilePath => Path.Combine(GetHomeDirectory(), ".jike", "config.json");

    public JikeConfig Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefaultConfig();
            }

            return JsonSerializer.Deserialize(json, JikeJsonSerializerContext.Default.JikeConfig) ?? CreateDefaultConfig();
        }
        catch (JsonException ex)
        {
            throw new JikeCliException($"配置文件格式错误: {ConfigFilePath}。{ex.Message}");
        }
        catch (IOException ex)
        {
            throw new JikeCliException($"读取配置文件失败: {ex.Message}");
        }
    }

    public void Save(JikeConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
            var json = JsonSerializer.Serialize(config, JikeJsonSerializerContext.Default.JikeConfig);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (IOException ex)
        {
            throw new JikeCliException($"写入配置文件失败: {ex.Message}");
        }
    }

    private static string GetHomeDirectory()
    {
        var overrideHome = Environment.GetEnvironmentVariable("JIKE_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(overrideHome))
        {
            return overrideHome;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static JikeConfig CreateDefaultConfig()
    {
        return new JikeConfig
        {
            BaseUrl = JikeApiClient.DefaultBaseUrl.TrimEnd('/')
        };
    }
}
