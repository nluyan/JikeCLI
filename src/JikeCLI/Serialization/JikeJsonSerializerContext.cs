using JikeCLI.Models;
using System.Text.Json.Serialization;

namespace JikeCLI.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(JikeConfig))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(AddOrderWorkRequest))]
[JsonSerializable(typeof(ApiResponse<string?>))]
[JsonSerializable(typeof(ApiResponse<List<UploadedFileItem>?>))]
[JsonSerializable(typeof(ApiErrorResponse))]
public partial class JikeJsonSerializerContext : JsonSerializerContext
{
}
