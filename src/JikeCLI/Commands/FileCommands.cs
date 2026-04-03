using ConsoleAppFramework;
using JikeCLI.Infrastructure;

namespace JikeCLI.Commands;

public sealed class FileCommands(JikeApiClient apiClient, JikeConfigStore configStore)
{
    /// <summary>
    /// 上传本地图片或附件到 Jike 文件服务，默认使用当前登录保存的 token。
    /// </summary>
    /// <param name="path">-p, 要上传的本地文件路径。</param>
    [Command("upload")]
    public async Task Upload(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new JikeCliException("--path 不能为空。");
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new JikeCliException($"文件不存在: {fullPath}");
        }

        var config = configStore.Load();
        if (string.IsNullOrWhiteSpace(config.Token))
        {
            throw new JikeCliException("未找到登录 token，请先执行 jike login。");
        }

        var response = await apiClient.UploadFileAsync(
            config.TokenType ?? "Bearer",
            config.Token,
            fullPath,
            cancellationToken);

        Console.WriteLine("文件上传成功。");
        Console.WriteLine($"文件路径: {fullPath}");

        if (response.Data is null || response.Data.Count == 0)
        {
            Console.WriteLine("接口未返回附件 ID。");
            return;
        }

        foreach (var file in response.Data)
        {
            if (string.IsNullOrWhiteSpace(file.Id))
            {
                continue;
            }

            Console.WriteLine($"文件 ID: {file.Id}");
        }
    }
}
