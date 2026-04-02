using JikeCLI.Models;
using JikeCLI.Serialization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JikeCLI.Infrastructure;

public sealed class JikeApiClient(HttpClient httpClient)
{
    public const string DefaultBaseUrl = "https://test5011.jikefw.com/";
    private const string Platform = "0";
    private const string UserAgent = "JikeCLI/1.0";

    public async Task<LoginResponse> LoginAsync(
        string tenantId,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Check/AccountToken")
        {
            Content = CreateJsonContent(new LoginRequest
            {
                UserName = username,
                PassWord = password
            }, JikeJsonSerializerContext.Default.LoginRequest)
        };

        request.Headers.TryAddWithoutValidation("Tenantid", tenantId);
        request.Headers.TryAddWithoutValidation("platform", Platform);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateRequestException("登录失败", response.StatusCode, body);
        }

        var result = JsonSerializer.Deserialize(body, JikeJsonSerializerContext.Default.LoginResponse);
        if (result is null)
        {
            throw new JikeCliException("登录接口返回为空。");
        }

        if (string.IsNullOrWhiteSpace(result.AccessToken))
        {
            throw new JikeCliException("登录成功，但未返回 access token。");
        }

        return result;
    }

    public async Task<ApiResponse<string?>> AddOrderWorkAsync(
        string tokenType,
        string token,
        AddOrderWorkRequest payload,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/OrderWorks/AddOrderWork")
        {
            Content = CreateJsonContent(payload, JikeJsonSerializerContext.Default.AddOrderWorkRequest)
        };

        request.Headers.TryAddWithoutValidation("platform", Platform);
        request.Headers.Authorization = new AuthenticationHeaderValue(tokenType, token);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateRequestException("创建工单失败", response.StatusCode, body);
        }

        var result = JsonSerializer.Deserialize(body, JikeJsonSerializerContext.Default.ApiResponseString);
        if (result is null)
        {
            throw new JikeCliException("工单接口返回为空。");
        }

        if (result.Code != 200)
        {
            throw new JikeCliException(string.IsNullOrWhiteSpace(result.Message)
                ? $"创建工单失败，接口返回 code={result.Code}。"
                : $"创建工单失败: {result.Message}");
        }

        return result;
    }

    private static StringContent CreateJsonContent<T>(T payload, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(payload, typeInfo);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static JikeCliException CreateRequestException(string prefix, System.Net.HttpStatusCode statusCode, string body)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var error = JsonSerializer.Deserialize(body, JikeJsonSerializerContext.Default.ApiErrorResponse);
                if (error is not null && !string.IsNullOrWhiteSpace(error.Message))
                {
                    return new JikeCliException($"{prefix}: {error.Message}");
                }
            }
            catch (JsonException)
            {
            }

            return new JikeCliException($"{prefix}: HTTP {(int)statusCode} {statusCode}，响应: {body}");
        }

        return new JikeCliException($"{prefix}: HTTP {(int)statusCode} {statusCode}。");
    }
}
