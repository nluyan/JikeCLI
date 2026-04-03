using System.Text.Json;
using System.Text.Json.Serialization;

namespace JikeCLI.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("passWord")]
    public string? PassWord { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }
}

public sealed class LoginResponse
{
    [JsonPropertyName("access_Token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_In")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_Type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_Token")]
    public string? RefreshToken { get; set; }
}

public sealed class AddOrderWorkRequest
{
    [JsonPropertyName("startUserPhone")]
    public string? StartUserPhone { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("urgency")]
    public string? Urgency { get; set; }

    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    [JsonPropertyName("requiredTime")]
    public string? RequiredTime { get; set; }

    [JsonPropertyName("files")]
    public string[]? Files { get; set; }
}

public sealed class ApiResponse<T>
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("stackMessage")]
    public string? StackMessage { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}

public sealed class ApiErrorResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("stackMessage")]
    public string? StackMessage { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

public sealed class UploadedFileItem
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
