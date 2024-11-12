using System.Diagnostics;

public class ConsoleHelpers
{
    public static void HandleAudioEvents(AudioSourceController audioSourceController)
    {
        audioSourceController.DisplayOutput += OnAudioSourceControllerDisplayOutput;
        audioSourceController.SoundOutput += OnAudioSourceControllerSoundOutput;
    }

    public static Task HandleKeysAsync(AudioSourceController audioSourceController, SpeakerAudioOutputStream speaker)
    {
        return Task.Run(() =>
        {
            HandleKeys(audioSourceController, speaker);
        });
    }

    public static void Write(string message, ConsoleColor? color = null, bool savePos = false, int atX = -1, int atY = -1)
    {
        lock (_consoleWriteSyncLock)
        {
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;

            var x = Console.CursorLeft;
            var y = Console.CursorTop;

            var setAndRestoreColor = color != null;
            if (setAndRestoreColor) Console.ForegroundColor = color!.Value;

            if (savePos)
            {
                if (atX >= 0 && atY >= 0)
                {
                    Console.SetCursorPosition(atX, atY);
                }
                else if (atX >= 0)
                {
                    Console.SetCursorPosition(atX, y);
                }
                else if (atY >= 0)
                {
                    Console.SetCursorPosition(x, atY);
                }
            }

            Console.Write(message);

            if (setAndRestoreColor)
            {
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
            }

            if (savePos)
            {
                Console.SetCursorPosition(x, y);
            }
        }
    }

    private static void HandleKeys(AudioSourceController audioSourceController, SpeakerAudioOutputStream speaker)
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                HandleKey(key, audioSourceController, speaker);
            }
        }
    }

    private static void HandleKey(ConsoleKeyInfo key, AudioSourceController audioSourceController, SpeakerAudioOutputStream speaker)
    {
        if (key.Key == ConsoleKey.X)
        {
            Environment.Exit(0);
        }
        else if (key.Key == ConsoleKey.D)
        {
            Program.Debug = !Program.Debug;
            ConsoleHelpers.Write($"Debug mode: {Program.Debug}\n\n");
        }
        else if (key.Key == ConsoleKey.M)
        {
            audioSourceController.ToggleMute();
        }
        else if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter)
        {
            speaker.ClearPlayback();
            audioSourceController.ToggleState();
        }
        else if (key.Key == ConsoleKey.K)
        {
            audioSourceController.ToggleKeywordState();
        }
    }

    private static void OnAudioSourceControllerSoundOutput(object? sender, SoundEventArgs e)
    {
        e.PlaySound();
    }

    private static void OnAudioSourceControllerDisplayOutput(object? sender, string e)
    {
        var message = $"[{e}]";

        var addSpaces = Console.WindowWidth - message.Length;
        if (addSpaces > 0)
        {
            message += new string(' ', addSpaces);
        }

        var y = Math.Max(Console.CursorTop - 1, 0);
        Write($"{message}\r", ConsoleColor.DarkBlue, true, 0, y);
    }

    private static object _consoleWriteSyncLock = new();
}