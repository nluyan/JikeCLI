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
        using var loginContent = CreateLoginContent(username, password);
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Check/AccountToken")
        {
            Content = loginContent
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

        var result = DeserializeLoginResponse(body);

        if (string.IsNullOrWhiteSpace(result.AccessToken))
        {
            var apiErrorMessage = GetApiErrorMessage(body);
            if (!string.IsNullOrWhiteSpace(apiErrorMessage))
            {
                throw new JikeCliException($"登录失败: {apiErrorMessage}");
            }

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

    public async Task<ApiResponse<List<UploadedFileItem>?>> UploadFileAsync(
        string tokenType,
        string token,
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filePath));

        using var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "files", Path.GetFileName(filePath));

        var request = new HttpRequestMessage(HttpMethod.Post, "api/Files/Upload")
        {
            Content = multipartContent
        };

        request.Headers.TryAddWithoutValidation("platform", Platform);
        request.Headers.Authorization = new AuthenticationHeaderValue(tokenType, token);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateRequestException("上传文件失败", response.StatusCode, body);
        }

        var result = JsonSerializer.Deserialize(body, JikeJsonSerializerContext.Default.ApiResponseListUploadedFileItem);
        if (result is null)
        {
            throw new JikeCliException("上传接口返回为空。");
        }

        if (result.Code != 200)
        {
            throw new JikeCliException(string.IsNullOrWhiteSpace(result.Message)
                ? $"上传文件失败，接口返回 code={result.Code}。"
                : $"上传文件失败: {result.Message}");
        }

        return result;
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".zip" => "application/zip",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private static StringContent CreateJsonContent<T>(T payload, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(payload, typeInfo);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static StringContent CreateLoginContent(string username, string password)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("userName", username);
            writer.WriteString("passWord", password);
            writer.WriteEndObject();
        }

        return new StringContent(Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8, "application/json");
    }

    private static LoginResponse DeserializeLoginResponse(string body)
    {
        try
        {
            var generatedResult = JsonSerializer.Deserialize(body, JikeJsonSerializerContext.Default.LoginResponse);
            if (generatedResult is not null && !string.IsNullOrWhiteSpace(generatedResult.AccessToken))
            {
                return generatedResult;
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JikeCliException("登录接口返回格式不正确。");
            }

            return new LoginResponse
            {
                AccessToken = GetString(root, "access_Token", "accessToken"),
                ExpiresIn = GetInt32(root, "expires_In", "expiresIn"),
                TokenType = GetString(root, "token_Type", "tokenType"),
                Scope = GetString(root, "scope"),
                RefreshToken = GetString(root, "refresh_Token", "refreshToken")
            };
        }
        catch (JsonException ex)
        {
            throw new JikeCliException($"登录接口返回解析失败: {ex.Message}");
        }
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

    private static string? GetApiErrorMessage(string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize(body, JikeJsonSerializerContext.Default.ApiErrorResponse);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return GetString(document.RootElement, "message");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return value.ToString();
        }

        return null;
    }

    private static int GetInt32(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numericValue))
            {
                return numericValue;
            }

            if (value.ValueKind == JsonValueKind.String
                && int.TryParse(value.GetString(), out var stringValue))
            {
                return stringValue;
            }
        }

        return 0;
    }
}
