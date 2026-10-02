//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
//              2014—2015 Benjamin Litzelmann
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace WinCompose
{

static class Updater
{
    public static void Init()
    {
        m_thread = new Thread(Run);
        m_thread.Start();
    }

    public static void Fini()
    {
        m_exiting = true;
        m_wake.Set();
        m_thread.Join();
    }

    /// <summary>
    /// Other modules can listen to this event to be warned when upgrade information
    /// has been retrieved.
    /// </summary>
    public static event Action Changed;

    /// <summary>
    /// Raised after every query, carrying whether the query itself got an
    /// answer. <see cref="Changed"/> fires only when there IS a newer
    /// version, so on its own it cannot tell "you are up to date" apart from
    /// "still checking" -- which is the whole of what a manual check has to
    /// report.
    ///
    /// The flag is there because <see cref="UpdateStatus"/> swallows its own
    /// network failures: without it a click made offline reports "up to
    /// date", read off a dictionary nothing managed to fill.
    /// </summary>
    public static event Action<bool> Checked;

    /// <summary>
    /// Query the status file now instead of waiting out the sleep. Returns
    /// immediately; the answer arrives on <see cref="Checked"/>.
    ///
    /// This is the only way to force a check. The loop below reads
    /// Settings.CheckUpdates at the top of each pass, so even switching the
    /// automatic check back on does nothing until the current sleep expires,
    /// which is up to 90 minutes away.
    /// </summary>
    public static void CheckNow()
    {
        Interlocked.Exchange(ref m_forced, 1);
        m_wake.Set();
    }

    private static void Run()
    {
        for (;;)
        {
            // Take the flag atomically: a separate read-then-clear loses a
            // click that lands between the two, and with the automatic check
            // switched off nothing else would ever query -- so the button
            // would sit on "Checking..." for the rest of the session.
            //
            // A manual check must query even when the automatic one is off.
            // The click IS the consent, and a button that reports "up to date"
            // without having asked anything would be worse than no button.
            bool forced = Interlocked.Exchange(ref m_forced, 0) != 0;

            try
            {
                bool queried = false, answered = false;
                if (forced || Settings.CheckUpdates.Value)
                {
                    answered = UpdateStatus();
                    queried = true;
                }

                if (HasNewerVersion)
                {
                    Raise(() => Changed?.Invoke(), nameof(Changed));
                }

                // Only when something was actually asked. Otherwise the timer
                // would keep announcing a verdict drawn from data nobody
                // refreshed, on a tab the user may be reading.
                if (queried)
                    Raise(() => Checked?.Invoke(answered), nameof(Checked));
            }
            catch (Exception ex)
            {
                // Belt and braces for everything that is not a subscriber --
                // Raise already handles those, and UpdateStatus swallows its
                // own network failures. Nothing restarts this thread, so a
                // dead one never checks again and never says so.
                Logger.Warn(ex, "Update check failed");
            }

            // Wait 30 to 90 minutes, or until CheckNow or Fini signals us. The
            // spread is what keeps every install from querying at once; waiting
            // on an event rather than sleeping is what lets a manual check cut
            // it short.
            m_wake.WaitOne(TimeSpan.FromMinutes(m_random.Next(30, 90)));
            if (m_exiting)
                return;
        }
    }

    /// <summary>
    /// Fire one event, keeping a throwing subscriber to itself.
    ///
    /// Each list is raised separately because they are not interchangeable:
    /// a Changed handler that throws used to skip the Checked below it, which
    /// left a manual check sitting on "Checking..." for the rest of the
    /// session -- the one state the button exists to leave.
    /// </summary>
    private static void Raise(Action raise, string name)
    {
        try
        {
            raise();
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"A {name} subscriber threw");
        }
    }

    public static bool HasNewerVersion
    {
        get
        {
            string latest = Get("Latest");
            if (latest == null)
                return false;

            return IsNewerVersion(Settings.Version, latest);
        }
    }

    /// <summary>
    /// Whether <see cref="available"/> is a later version than
    /// <see cref="current"/>. Compare component by component and decide on the
    /// first one that differs.
    ///
    /// The loop this replaces only ever tested for "less than" and did not stop
    /// on a greater component, so a later component could overrule an earlier
    /// one: running 0.10.0 against an available 0.9.20 answered yes, because
    /// 10 &lt; 9 is false but 0 &lt; 20 is true. That offers a downgrade as an
    /// update, and it becomes reachable as soon as the minor version climbs
    /// past the patch number. The same shape made 1.2.3 accept 1.2.3beta1.
    ///
    /// Split out of the property so it can be tested: the property reads the
    /// running assembly's own version, which under a test host is the test
    /// assembly's rather than WinCompose's.
    /// </summary>
    internal static bool IsNewerVersion(string current, string available)
    {
        var c = SplitVersionString(current);
        var a = SplitVersionString(available);

        for (int i = 0; i < 4; ++i)
            if (c[i] != a[i])
                return c[i] < a[i];

        return false;
    }

    private static List<int> SplitVersionString(string str)
    {
        List<int> ret = new List<int>();
        int tmp;

        // If we fail to parse a chunk, use 1 instead of 0 so that version
        // 1.2.3.foo is still greater than 1.2.3.0.
        foreach (var e in str.Replace("beta", ".").Split(new char[] { '.' }))
            ret.Add(int.TryParse(e, out tmp) ? tmp : 1);

        // If fewer than 4 elements, add zeroes; if more than 4, remove them
        while (ret.Count < 4)
            ret.Add(0);

        while (ret.Count > 4)
            ret.RemoveAt(ret.Count - 1);

        // Handle beta versions in the form 1.2.3beta456 that need to be
        // smaller than 1.2.3.0 but greater than any realistic 1.2.2.x.
        if (str.Contains("beta"))
        {
            ret[2] -= 1;
            ret[3] += 100000000;
        }

        return ret;
    }

    public static string Get(string key)
    {
        string ret = null;
        m_data.TryGetValue(key, out ret);
        return ret;
    }

    /// <summary>
    /// Query our own status file for update information. This used to point at
    /// http://wincompose.info/status.txt, which is upstream's server: it is
    /// plain HTTP, we do not control it, and it advertises upstream releases
    /// that do not correspond to this fork's builds.
    ///
    /// Returns whether the file was read. Failure stays silent in the log --
    /// the automatic check runs every 30 to 90 minutes and an offline machine
    /// would fill the file with it -- so the return value is the only way a
    /// caller can tell "no newer version" from "never got an answer".
    /// </summary>
    private static bool UpdateStatus()
    {
        try
        {
            WebClient browser = new WebClient();
            browser.Headers.Add("user-agent", GetUserAgent());
            using (Stream s = browser.OpenRead(STATUS_URL))
            using (StreamReader sr = new StreamReader(s))
            {
                m_data.Clear();

                for (string line = sr.ReadLine(); line != null;  line = sr.ReadLine())
                {
                    string pattern = "^([^#: ][^: ]*):  *(.*[^ ]) *$";
                    var m = Regex.Match(line, pattern);
                    if (m.Groups.Count == 3)
                    {
                        string key = m.Groups[1].Captures[0].ToString();
                        string val = m.Groups[2].Captures[0].ToString();
                        m_data[key] = val;
                    }
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetUserAgent()
    {
        var flavour = Utils.IsDebugging ? "; Development" :
                      Utils.IsInstalled ? "" : "; Portable";
        return $"WinCompose/{Settings.Version} ({Environment.OSVersion}{flavour})";
    }

    private const string STATUS_URL
        = "https://raw.githubusercontent.com/thpoll83/wincompose/main/status.txt";

    private static Dictionary<string, string> m_data = new Dictionary<string, string>();
    private static Thread m_thread;

    // Set by CheckNow (query now) and by Fini (stop); m_exiting says which.
    private static readonly AutoResetEvent m_wake = new AutoResetEvent(false);
    private static volatile bool m_exiting;
    // Plain int, not volatile bool: Interlocked needs a ref to a non-volatile
    // field (CS0420) and has no bool overload. 0 = no request, 1 = check now.
    private static int m_forced;
    private static readonly Random m_random = new Random();

    private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
}

}

