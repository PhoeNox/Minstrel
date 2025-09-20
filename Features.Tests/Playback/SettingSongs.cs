using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

namespace Features.Tests.Playback;

[UseAppContext]
public class SettingSongs(AppContext appContext, IDispatcher dispatcher)
{
    [Test]
    public async Task SettingSongForDay()
    {
        var requestedSong = Dummies.Song;
        var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
        actionSubscriber.SubscribeToAction<LoadSongSignal>(this, action => requestedSong = action.Song);
        
        var song1 = Dummies.Song with {Title = "Song1"};
        var song2 = Dummies.Song with {Title = "Song2"};
        dispatcher.Dispatch(new SetPlaylistsAction([song1, song2], []));
        
        dispatcher.Dispatch(new SetCurrentSongForDayAction(song2));
        
        await Assert.That(requestedSong).IsEqualTo(song2);
    }
    
    [Test]
    public async Task SettingSongForNight()
    {
        var requestedSong = Dummies.Song;
        var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
        actionSubscriber.SubscribeToAction<LoadSongSignal>(this, action => requestedSong = action.Song);
        
        var song1 = Dummies.Song with {Title = "Song1"};
        var song2 = Dummies.Song with {Title = "Song2"};
        dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2]));
        
        dispatcher.Dispatch(new SetCurrentSongForNightAction(song2));
        
        await Assert.That(requestedSong).IsEqualTo(song2);
    }
}