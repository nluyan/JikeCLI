using ConsoleAppFramework;
using JikeCLI.Infrastructure;
using JikeCLI.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace JikeCLI.Commands;

public sealed class OrderCommands(JikeApiClient apiClient, JikeConfigStore configStore)
{
    private const string RequiredTimeFormat = "yyyy-MM-dd HH:mm:ss";

    [Command("add")]
    public async Task Add(
        string phone,
        [Range(0, 1, ErrorMessage = "--urgency 只支持 0(普通) 或 1(紧急)。")] int urgency,
        string content,
        string? requiredTime = null,
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
        var request = new AddOrderWorkRequest
        {
            StartUserPhone = phone,
            Source = "机器人",
            Urgency = urgency == 0 ? "普通" : "紧急",
            Desc = content,
            RequiredTime = dueTime.ToString(RequiredTimeFormat, CultureInfo.InvariantCulture)
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
            Console.WriteLine($"工单创建成功，工单ID: {response.Data}");
        }

        Console.WriteLine($"要求时间: {request.RequiredTime}");
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
}
