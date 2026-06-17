// ReSharper disable ClassNeverInstantiated.Global
namespace Backend.Features.Library;

using Core;

public sealed record AddCommand(GamePhase Phase, string SongId);
public sealed record RemoveCommand(GamePhase Phase, int Index);
public sealed record ShuffleCommand(GamePhase Phase);
public sealed record MoveCommand(GamePhase Phase, int OldIndex, int NewIndex);
