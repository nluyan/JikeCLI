using ConsoleAppFramework;
using JikeCLI.Infrastructure;

namespace JikeCLI.Commands;

public sealed class LoginCommands(JikeApiClient apiClient, JikeConfigStore configStore)
{
    [Command("login")]
    public async Task Login(
        string username,
        string tenant,
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

        var password = PasswordPrompt.ReadPassword("Password: ");
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
        Console.WriteLine($"Token 已保存到: {configStore.ConfigFilePath}");
    }
}
