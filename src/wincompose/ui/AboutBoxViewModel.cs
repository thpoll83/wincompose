//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2019 Sam Hocevar <sam@hocevar.net>
//              2014—2015 Benjamin Litzelmann
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace WinCompose
{
    public class AboutBoxViewModel : ViewModelBase
    {
        private readonly DelegateCommand m_openwebsite_command;
        private readonly DelegateCommand m_reportbug_command;
        private readonly DelegateCommand m_openupstream_command;
        private readonly DelegateCommand m_opendonate_command;
        private readonly DelegateCommand m_openlogfolder_command;

        public AboutBoxViewModel()
        {
            m_openwebsite_command = new DelegateCommand(OnOpenWebsiteCommandExecuted);
            m_reportbug_command = new DelegateCommand(OnReportBugCommandExecuted);
            m_openupstream_command = new DelegateCommand(OnOpenUpstreamCommandExecuted);
            m_opendonate_command = new DelegateCommand(OnOpenDonateCommandExecuted);
            m_openlogfolder_command = new DelegateCommand(OnOpenLogFolderCommandExecuted);
        }


        public ICommand OpenWebsiteCommand => m_openwebsite_command;
        public ICommand OpenReportBugCommand => m_reportbug_command;
        public ICommand OpenUpstreamCommand => m_openupstream_command;
        public ICommand OpenDonateCommand => m_opendonate_command;
        public ICommand OpenLogFolderCommand => m_openlogfolder_command;

        public Stream AuthorsDocument
            => Application.GetResourceStream(new Uri("pack://application:,,,/res/contributors.html")).Stream;

        public Stream LicenseDocument
            => Application.GetResourceStream(new Uri("pack://application:,,,/res/copying.html")).Stream;

        // Rendered as plain text rather than through a WebBrowser: that control
        // is an HwndHost, so it paints over neighbouring WPF content and ignores
        // the surrounding ScrollViewer's clipping.
        public string AuthorsText => HtmlToText(AuthorsDocument);

        public string LicenseText => HtmlToText(LicenseDocument);

        private static string HtmlToText(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();

                text = Regex.Replace(text, "<li>", "• ", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, "<br ?/?>", "\n", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, "<[^>]+>", "");

                // &amp; last, so an escaped entity is not decoded twice.
                text = text.Replace("&lt;", "<").Replace("&gt;", ">")
                           .Replace("&quot;", "\"").Replace("&#39;", "'")
                           .Replace("&amp;", "&");

                // Stripping the tags leaves the runs of blank lines they sat on.
                var lines = text.Replace("\r", "").Split('\n')
                                .Select(line => line.TrimEnd());
                var result = new System.Text.StringBuilder();
                var blank = 0;
                foreach (var line in lines)
                {
                    blank = line.Length == 0 ? blank + 1 : 0;
                    if (blank > 1)
                        continue;
                    result.AppendLine(line);
                }
                return result.ToString().Trim('\n');
            }
        }

        public string Version => Settings.Version;

        // This fork's own page, not the original project's site.
        private static void OnOpenWebsiteCommandExecuted(object parameter)
            => System.Diagnostics.Process.Start("https://www.polykybd.org/software/wincompose/");

        // Bugs in this fork belong in this fork's tracker; the original project
        // is no longer actively developed and should not receive our reports.
        private static void OnReportBugCommandExecuted(object parameter)
            => System.Diagnostics.Process.Start("https://github.com/thpoll83/wincompose/issues/new");

        private static void OnOpenUpstreamCommandExecuted(object parameter)
            => System.Diagnostics.Process.Start("https://github.com/samhocevar/wincompose");

        // Donations go to the original author, whose work this all is.
        private static void OnOpenDonateCommandExecuted(object parameter)
            => System.Diagnostics.Process.Start("http://wincompose.info/donate/");

        /// <summary>
        /// Opens the folder holding the log, with the file selected when it is
        /// already there -- on a first run it may not be.
        ///
        /// Unlike the buttons above this one, it can genuinely fail: the
        /// folder may not exist, and Explorer may be unavailable or replaced.
        /// A diagnostic aid that throws while the user is trying to report a
        /// problem would be a poor joke, so it logs and gives up.
        /// </summary>
        private static void OnOpenLogFolderCommandExecuted(object parameter)
        {
            try
            {
                var file = Logging.FilePath;
                if (File.Exists(file))
                {
                    // No space after the comma: Explorer treats "/select, x"
                    // as two arguments and silently opens Documents instead.
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file}\"");
                }
                else
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(file)}\"");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not open the log folder");
            }
        }

        private static NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
