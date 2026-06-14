namespace Backend.Commands;

using Core;

public sealed record PlayCommand(string? SongId);

public sealed record SelectCommand(GamePhase Phase, int Index);

public sealed record AddCommand(GamePhase Phase, string SongId);

public sealed record RemoveCommand(GamePhase Phase, int Index);

public sealed record ShuffleCommand(GamePhase Phase);

public sealed record MoveCommand(GamePhase Phase, int OldIndex, int NewIndex);

public sealed record SetGainCommand(GamePhase Phase, double Value);

public sealed record TimerStartCommand(double Duration);
