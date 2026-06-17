// ReSharper disable ClassNeverInstantiated.Global
namespace Backend.Features.Playback;

using Core;

public sealed record PlayCommand(string? SongId);

public sealed record SelectCommand(GamePhase Phase, int Index);

public sealed record SetGainCommand(GamePhase Phase, double Value);
