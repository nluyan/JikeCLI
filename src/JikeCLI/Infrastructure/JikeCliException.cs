using System.ComponentModel.DataAnnotations;

namespace JikeCLI.Infrastructure;

public sealed class JikeCliException(string message) : ValidationException(message)
{
}
