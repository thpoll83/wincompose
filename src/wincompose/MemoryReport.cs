//
//  WinCompose - a compose key for Windows - http://wincompose.info/
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Diagnostics;
using System.Threading;

namespace WinCompose
{

/// <summary>
/// Records how much memory the process is using, at startup, at shutdown and
/// every few minutes in between.
///
/// WinCompose sits in the tray all day, so "it uses 100 MB" is a fair thing to
/// be told and an unfalsifiable one to act on: nothing in the app reported its
/// own footprint, which left reading the source as the only way to guess what
/// to optimise. These lines make the question answerable from a log a user can
/// send, and make it visible whether a footprint is steady or climbing.
///
/// They go through the ordinary logger, so they appear in the debug window and
/// in wincompose.log without any UI of their own.
/// </summary>
public static class MemoryReport
{
    /// <summary>
    /// Long enough to stay out of the way -- the log file archives daily, and
    /// this adds under 300 lines to it -- and short enough that an hour of
    /// running says whether the footprint is stable.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The -memprofile window. Five minutes between samples is right for a
    /// day-long run and useless for the question "when did the heap grow",
    /// because the growth all happens in the first of those intervals and a
    /// sample that does not collect cannot separate live objects from garbage
    /// anyway. Thirty seconds for ten minutes brackets it, at the cost of
    /// twenty forced collections that only a diagnostic run should pay.
    /// </summary>
    private static readonly TimeSpan ProfileInterval = TimeSpan.FromSeconds(30);
    private static readonly int ProfileSamples = 20;

    /// <param name="profile">Start with the dense window described above.
    /// Off by default: a full blocking collection twice a minute is fine when
    /// someone is measuring and wrong to inflict on everyone else.</param>
    public static void Init(bool profile = false)
    {
        Report("startup");
        m_profile_left = profile ? ProfileSamples : 0;
        if (m_profile_left > 0)
            Logger.Info($"Memory profiling: {ProfileSamples} samples, "
                      + $"one every {ProfileInterval.TotalSeconds:F0} s, each with a full collection.");
        m_clock.Start();
        // One-shot, rescheduled from the callback, so the cadence can change
        // without a second timer or a period-change race.
        m_timer = new Timer(OnTick, null, NextDelay(), Timeout.InfiniteTimeSpan);
    }

    private static TimeSpan NextDelay() => m_profile_left > 0 ? ProfileInterval : Interval;

    private static void OnTick(object unused)
    {
        bool profiling = m_profile_left > 0;
        var elapsed = m_clock.Elapsed;
        Report(profiling ? $"profile +{elapsed.TotalSeconds:F0}s" : "periodic", collect: profiling);

        if (profiling && --m_profile_left == 0)
            Logger.Info("Memory profiling window over; back to the periodic sample.");

        try
        {
            m_timer?.Change(NextDelay(), Timeout.InfiniteTimeSpan);
        }
        catch (ObjectDisposedException)
        {
            // Fini() won the race. Nothing to reschedule.
        }
    }

    public static void Fini()
    {
        m_timer?.Dispose();
        m_timer = null;
        // The one place a full collection is free: the app is going away, so
        // the pause is invisible and the reading is the session's only honest
        // answer to "how much of the heap was actually live".
        Report("shutdown", collect: true);
    }

    /// <summary>
    /// Three numbers, because they answer different questions. The working set
    /// is what Task Manager shows and therefore what a user reports; the
    /// private bytes are what the process actually commits; and the managed
    /// heap says how much of that is our own objects rather than WPF's
    /// unmanaged rendering and the framework itself. Without the third, there
    /// is no way to tell a data-structure problem from a WPF one.
    ///
    /// <paramref name="collect"/> decides whether the heap figure is what has
    /// been allocated or what is still live, and the difference decides what
    /// the number can be used for. A periodic sample must not perturb the app,
    /// so it does not collect and its heap figure includes garbage awaiting a
    /// GC. That makes it useless for measuring a *release*: freeing an object
    /// graph moves nothing until something collects it, so an uncollected
    /// reading after a release shows no drop and reads as "the release did
    /// nothing". Pass true when the point of the reading is a before/after
    /// difference -- it is a full blocking collection, so only at startup or
    /// after a deliberate user action, never on the timer.
    /// </summary>
    public static void Report(string reason, bool collect = false)
    {
        try
        {
            using (var proc = Process.GetCurrentProcess())
            {
                var heap = GC.GetTotalMemory(collect);
                Logger.Info($"Memory ({reason}): working set {Mb(proc.WorkingSet64)}, "
                          + $"private {Mb(proc.PrivateMemorySize64)}, "
                          + $"{(collect ? "live heap" : "managed heap")} {Mb(heap)}, "
                          + $"collections {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");
            }
        }
        catch (Exception ex)
        {
            // A diagnostic must never be the reason the app misbehaves.
            Logger.Warn(ex, "Could not read memory counters");
        }
    }

    private static string Mb(long bytes) => $"{bytes / 1048576.0:F1} MB";

    private static Timer m_timer;

    private static int m_profile_left;

    private static readonly Stopwatch m_clock = new Stopwatch();

    private static NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
}

}
