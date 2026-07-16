namespace App.Application;

public sealed record ApplicationResult(byte[] Response, bool CloseConnection = false)
{
    public static ApplicationResult Empty { get; } = new([]);
}
