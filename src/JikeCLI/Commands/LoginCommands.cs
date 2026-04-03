using ConsoleAppFramework;
using JikeCLI.Infrastructure;

namespace JikeCLI.Commands;

public sealed class LoginCommands(JikeApiClient apiClient, JikeConfigStore configStore)
{
    /// <summary>
    /// 登录 Jike 系统并保存访问令牌，执行时会交互式提示输入密码。
    /// </summary>
    /// <param name="username">登录账号，例如工号或用户名。</param>
    /// <param name="tenant">租户标识，用于确定登录的目标租户。</param>
    /// <param name="password">登录密码；不传时会交互式提示输入。</param>
    [Command("login")]
    public async Task Login(
        string username,
        string tenant,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new JikeCliException("--username 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(tenant))
        {
            throw new JikeCliException("--tenant 不能为空。");
        }

        password ??= PasswordPrompt.ReadPassword("密码: ");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new JikeCliException("密码不能为空。");
        }

        var loginResponse = await apiClient.LoginAsync(tenant, username, password, cancellationToken);
        var config = configStore.Load();

        config.Username = username;
        config.Tenant = tenant;
        config.Token = loginResponse.AccessToken;
        config.TokenType = string.IsNullOrWhiteSpace(loginResponse.TokenType) ? "Bearer" : loginResponse.TokenType;
        config.RefreshToken = loginResponse.RefreshToken;
        config.ExpiresIn = loginResponse.ExpiresIn;
        config.BaseUrl = JikeApiClient.DefaultBaseUrl.TrimEnd('/');
        config.UpdatedAt = DateTimeOffset.Now;

        configStore.Save(config);

        Console.WriteLine("登录成功。");
        Console.WriteLine($"Access Token: {loginResponse.AccessToken}");
        Console.WriteLine($"Token 已保存到: {configStore.ConfigFilePath}");
    }
}
