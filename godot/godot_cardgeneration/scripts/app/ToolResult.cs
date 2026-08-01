namespace CardGeneration.App;

public sealed class ToolResult
{
    public bool Success { get; }
    public string Message { get; }
    public int ExitCode => Success ? 0 : 1;

    private ToolResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static ToolResult Ok(string message)
    {
        return new ToolResult(true, message);
    }

    public static ToolResult Fail(string message)
    {
        return new ToolResult(false, message);
    }
}
