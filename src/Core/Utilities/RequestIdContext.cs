namespace Bit.Core.Utilities;

/// <summary>
/// Ambient request ID storage for the current async flow, used by logging enrichers.
/// </summary>
public static class RequestIdContext
{
    private static readonly AsyncLocal<string?> _currentRequestId = new();

    public static string? CurrentRequestId
    {
        get => _currentRequestId.Value;
        set => _currentRequestId.Value = value;
    }
}
