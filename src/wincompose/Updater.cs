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
        m_thread.Interrupt();
        m_thread.Join();
    }

    /// <summary>
    /// Other modules can listen to this event to be warned when upgrade information
    /// has been retrieved.
    /// </summary>
    public static event Action Changed;

    private static void Run()
    {
        for (;;)
        {
            try
            {
                if (Settings.CheckUpdates.Value)
                    UpdateStatus();

                if (HasNewerVersion)
                {
                    Changed?.Invoke();
                }

                // Sleep between 30 and 90 minutes before querying again
                Thread.Sleep(new Random().Next(30, 90) * 60 * 1000);
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
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
    /// </summary>
    private static void UpdateStatus()
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
        }
        catch (Exception) {}
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
}

}

