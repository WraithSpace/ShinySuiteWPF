using System;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ShinySuite.Services;

public static class SoundService
{
    private static int _tickBusy;

    private static Stream? GetStream(string name)
    {
        var uri = new Uri($"pack://application:,,,/ShinySuite;component/Assets/Sounds/{name}");
        var info = Application.GetResourceStream(uri);
        return info?.Stream;
    }

    // Short click on every encounter increment
    public static void PlayTick()
    {
        if (Interlocked.CompareExchange(ref _tickBusy, 1, 0) != 0) return;
        Task.Run(() =>
        {
            try
            {
                using var stream = GetStream("tick.wav");
                if (stream == null) return;
                using var player = new SoundPlayer(stream);
                player.PlaySync();
            }
            finally { Volatile.Write(ref _tickBusy, 0); }
        });
    }

    // Two-tone chime rising: timer started
    public static void PlayStart() => Task.Run(() =>
    {
        using var stream = GetStream("start.wav");
        if (stream == null) return;
        using var player = new SoundPlayer(stream);
        player.PlaySync();
    });

    // Two-tone chime falling: timer stopped
    public static void PlayStop() => Task.Run(() =>
    {
        using var stream = GetStream("stop.wav");
        if (stream == null) return;
        using var player = new SoundPlayer(stream);
        player.PlaySync();
    });

    // Ascending C major arpeggio: shiny found
    public static void PlayShinyFound() => Task.Run(() =>
    {
        using var stream = GetStream("shiny.wav");
        if (stream == null) return;
        using var player = new SoundPlayer(stream);
        player.PlaySync();
    });
}
