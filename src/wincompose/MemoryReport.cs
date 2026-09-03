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

    public static void Init()
    {
        Report("startup");
        m_timer = new Timer(_ => Report("periodic"), null, Interval, Interval);
    }

    public static void Fini()
    {
        m_timer?.Dispose();
        m_timer = null;
        Report("shutdown");
    }

    /// <summary>
    /// Three numbers, because they answer different questions. The working set
    /// is what Task Manager shows and therefore what a user reports; the
    /// private bytes are what the process actually commits; and the managed
    /// heap says how much of that is our own objects rather than WPF's
    /// unmanaged rendering and the framework itself. Without the third, there
    /// is no way to tell a data-structure problem from a WPF one.
    /// </summary>
    public static void Report(string reason)
    {
        try
        {
            using (var proc = Process.GetCurrentProcess())
            {
                Logger.Info($"Memory ({reason}): working set {Mb(proc.WorkingSet64)}, "
                          + $"private {Mb(proc.PrivateMemorySize64)}, "
                          + $"managed heap {Mb(GC.GetTotalMemory(false))}, "
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

    private static NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
}

}
