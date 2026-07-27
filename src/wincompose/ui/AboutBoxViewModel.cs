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
using System.Windows.Input;

namespace WinCompose
{
    public class AboutBoxViewModel : ViewModelBase
    {
        private readonly DelegateCommand m_openwebsite_command;
        private readonly DelegateCommand m_reportbug_command;
        private readonly DelegateCommand m_openupstream_command;
        private readonly DelegateCommand m_opendonate_command;

        public AboutBoxViewModel()
        {
            m_openwebsite_command = new DelegateCommand(OnOpenWebsiteCommandExecuted);
            m_reportbug_command = new DelegateCommand(OnReportBugCommandExecuted);
            m_openupstream_command = new DelegateCommand(OnOpenUpstreamCommandExecuted);
            m_opendonate_command = new DelegateCommand(OnOpenDonateCommandExecuted);
        }


        public ICommand OpenWebsiteCommand => m_openwebsite_command;
        public ICommand OpenReportBugCommand => m_reportbug_command;
        public ICommand OpenUpstreamCommand => m_openupstream_command;
        public ICommand OpenDonateCommand => m_opendonate_command;

        public Stream AuthorsDocument
            => Application.GetResourceStream(new Uri("pack://application:,,,/res/contributors.html")).Stream;

        public Stream LicenseDocument
            => Application.GetResourceStream(new Uri("pack://application:,,,/res/copying.html")).Stream;

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
    }
}
