//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Windows.Threading;

namespace WinCompose
{
    /// <summary>
    /// Interaction logic for WinCompose.xaml
    /// </summary>
    public partial class Application : System.Windows.Application
    {
        public Application()
        {
            try
            {
                InitializeComponent();
                Settings.SetTheme();
            }
            catch (Exception ex)
            {
                OnFatalError(ex);
            }
        }

        /// <summary>
        /// The memory reading in Program.cs is taken before this class is even
        /// constructed, so it sees our data structures and none of WPF: the
        /// framework, the theme, the tray icon and Emoji.Wpf all initialise
        /// after it. That left the largest single step in the process's
        /// footprint inside a blind spot, and a 40-minute field log showed the
        /// gap as an unexplained jump from 60 MB to 233 MB with no user action
        /// to pin it on.
        ///
        /// ApplicationIdle is the first moment WPF has nothing left to do, so
        /// this reading is "up and idle, nothing opened, nothing typed" -- the
        /// baseline every later sample should be read against.
        /// </summary>
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                                   new Action(() => MemoryReport.Report("ui ready", collect: true)));
        }

        private static void OnFatalError(Exception ex)
        {
            Logger.Fatal(ex, "Fatal Error");
            System.Windows.MessageBox.Show(ex.ToString(), "Fatal Error");
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
            => OnFatalError(e.Exception);

        public static RemoteControl RemoteControl => (Current as Application).RC;

        private static NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
