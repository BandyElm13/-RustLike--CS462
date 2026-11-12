using UnityEngine;

/// <summary>
/// Time spent in the current run. Static, so it keeps counting across scene loads
/// (planet -> dungeon -> planet). It only advances while something calls Tick each frame
/// (TimerHUD does), so it doesn't count in the Main Menu.
///
/// Hooks for later:
///   RunTimer.Stop()      — freeze it (e.g. on player death) so the death screen can show it.
///   RunTimer.Elapsed     — seconds in this run; RunTimer.Format(RunTimer.Elapsed) gives "00:00:00".
///   RunTimer.ResetRun()  — back to 0 and running (e.g. when Play is pressed for a new run).
/// </summary>
public static class RunTimer
{
    public static float Elapsed { get; private set; }
    public static bool IsRunning { get; private set; } = true;

    private static int lastTickFrame = -1;

    /// <summary>Advance the timer. Safe to call from several places: only counts once per frame.</summary>
    public static void Tick(float deltaTime)
    {
        if (!IsRunning || Time.frameCount == lastTickFrame) return;
        lastTickFrame = Time.frameCount;
        Elapsed += deltaTime;
    }

    public static void Stop() => IsRunning = false;
    public static void Resume() => IsRunning = true;

    public static void ResetRun()
    {
        Elapsed = 0f;
        IsRunning = true;
        lastTickFrame = -1;
    }

    /// <summary>Hours:minutes:seconds, e.g. 00:04:27.</summary>
    public static string Format(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";
    }

    // Start every Play session at 0, even if Domain Reload is turned off in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => ResetRun();
}
