using ConsoleAppFramework;
using JikeCLI.Infrastructure;
using JikeCLI.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace JikeCLI.Commands;

public sealed class OrderCommands(JikeApiClient apiClient, JikeConfigStore configStore)
{
    private const string RequiredTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// 创建一条工单；如果未指定要求时间，默认使用当前时间后 2 小时。
    /// </summary>
    /// <param name="phone">发起人的手机号。</param>
    /// <param name="urgency">紧急程度：0 表示普通，1 表示紧急。</param>
    /// <param name="content">工单内容或问题描述。</param>
    /// <param name="requiredTime">要求时间，格式为 yyyy-MM-dd HH:mm:ss；不填时默认当前时间后 2 小时。</param>
    /// <param name="files">附件 ID，多个 ID 使用英文逗号分隔，先通过 jike file upload 获取。</param>
    [Command("add")]
    public async Task Add(
        string phone,
        [Range(0, 1, ErrorMessage = "--urgency 只支持 0(普通) 或 1(紧急)。")] int urgency,
        string content,
        string? requiredTime = null,
        string? files = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new JikeCliException("--phone 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new JikeCliException("--content 不能为空。");
        }

        var config = configStore.Load();
        if (string.IsNullOrWhiteSpace(config.Token))
        {
            throw new JikeCliException("未找到登录 token，请先执行 jike login。");
        }

        var dueTime = ResolveRequiredTime(requiredTime);
        var fileIds = ParseFiles(files);
        var request = new AddOrderWorkRequest
        {
            StartUserPhone = phone,
            Source = "机器人",
            Urgency = urgency == 0 ? "普通" : "紧急",
            Desc = content,
            RequiredTime = dueTime.ToString(RequiredTimeFormat, CultureInfo.InvariantCulture),
            Files = fileIds
        };

        var response = await apiClient.AddOrderWorkAsync(
            config.TokenType ?? "Bearer",
            config.Token,
            request,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Data))
        {
            Console.WriteLine("工单创建成功。");
        }
        else
        {
            Console.WriteLine($"工单创建成功，工单 ID: {response.Data}");
        }

        Console.WriteLine($"要求时间: {request.RequiredTime}");

        if (fileIds is { Length: > 0 })
        {
            Console.WriteLine($"附件数量: {fileIds.Length}");
        }
    }

    private static DateTime ResolveRequiredTime(string? requiredTime)
    {
        if (string.IsNullOrWhiteSpace(requiredTime))
        {
            return DateTime.Now.AddHours(2);
        }

        if (DateTime.TryParseExact(
                requiredTime,
                RequiredTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        throw new JikeCliException($"--requiredTime 格式必须为 {RequiredTimeFormat}。");
    }

    private static string[]? ParseFiles(string? files)
    {
        if (string.IsNullOrWhiteSpace(files))
        {
            return null;
        }

        var fileIds = files
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (fileIds.Length == 0)
        {
            throw new JikeCliException("--files 至少要包含一个附件 ID。");
        }

        return fileIds;
    }
}
