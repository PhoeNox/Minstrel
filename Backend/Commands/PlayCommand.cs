namespace Backend.Commands;

using Core;

public sealed record PlayCommand(string? SongId);

public sealed record SelectCommand(GamePhase Phase, string SongId);

public sealed record MoveCommand(GamePhase Phase, int OldIndex, int NewIndex);
