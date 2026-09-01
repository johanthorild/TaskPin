namespace TaskPin.Models;

public sealed record TaskRecord(Guid Id, string Text, bool IsCompleted);