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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace WinCompose
{
    /// <summary>
    /// All possible commands that the notification area menu can execute
    /// </summary>
    public enum MenuCommand
    {
        ShowSequences,
        ShowOptions,
        About,
        DebugWindow,
        VisitWebsite,
        DonationPage,
        Download,
        Exit,
    }

    /// <summary>
    /// Interaction logic for NotificationIcon.xaml
    /// </summary>
    public partial class NotificationIcon : TaskbarIcon, INotifyPropertyChanged, IDisposable
    {
        public NotificationIcon()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            Application.RemoteControl.ExitEvent += OnExitEvent;
            Application.RemoteControl.OpenEvent += OnOpenEvent;

            TrayMouseDoubleClick += NotifyiconDoubleclicked;

            Settings.DisableIcon.ValueChanged += MarkIconDirty;
            Settings.ComposeKeys.ValueChanged += MarkIconDirty;
            Settings.UseXComposeRules.ValueChanged += MarkIconDirty;
            Settings.UseEmojiRules.ValueChanged += MarkIconDirty;
            Settings.UseXorgRules.ValueChanged += MarkIconDirty;
            Composer.Changed += MarkIconDirty;
            Updater.Changed += MarkIconDirty;
            MarkIconDirty();

            Updater.Changed += UpdaterStateChanged;
            UpdaterStateChanged();

            CompositionTarget.Rendering += UpdateNotificationIcon;
        }
        public new void Dispose()
        {
            GC.SuppressFinalize(this);
            CompositionTarget.Rendering -= UpdateNotificationIcon;

            Settings.DisableIcon.ValueChanged -= MarkIconDirty;
            Settings.ComposeKeys.ValueChanged -= MarkIconDirty;
            Settings.UseXComposeRules.ValueChanged -= MarkIconDirty;
            Settings.UseEmojiRules.ValueChanged -= MarkIconDirty;
            Settings.UseXorgRules.ValueChanged -= MarkIconDirty;
            Composer.Changed -= MarkIconDirty;
            Updater.Changed -= MarkIconDirty;
            Updater.Changed -= UpdaterStateChanged;
			
            Application.RemoteControl.ExitEvent -= OnExitEvent;
            Application.RemoteControl.OpenEvent -= OnOpenEvent;

            Visibility = Visibility.Collapsed;

            base.Dispose(true);
        }
        //protected virtual void Dispose(bool disposing)
        //{
        //    CompositionTarget.Rendering -= UpdateNotificationIcon;

        //    Composer.Changed -= MarkIconDirty;
        //    Updater.Changed -= MarkIconDirty;
        //    Updater.Changed -= UpdaterStateChanged;

        //    Application.RemoteControl.ExitEvent -= OnExitEvent;
        //    Application.RemoteControl.OpenEvent -= OnOpenEvent;

        //    Visibility = Visibility.Collapsed;

        //}

        public ICommand MenuItemCommand
        {
            get { return m_menu_item_command ?? (m_menu_item_command = new DelegateCommand(OnCommand)); }
        }

        private DelegateCommand m_menu_item_command;

        private void OnCommand(object o)
        {
            switch (o as MenuCommand?)
            {
                case MenuCommand.ShowSequences:
                    m_sequencewindow = m_sequencewindow ?? new SequenceWindow();
                    m_sequencewindow.Show();
                    m_sequencewindow.Activate();
                    break;

                case MenuCommand.ShowOptions:
                    m_optionswindow = m_optionswindow ?? new SettingsWindow();
                    m_optionswindow.Show();
                    m_optionswindow.Activate();
                    break;

                case MenuCommand.DebugWindow:
                    m_debugwindow = m_debugwindow ?? new DebugWindow();
                    m_debugwindow.Show();
                    m_debugwindow.Activate();
                    break;

                case MenuCommand.About:
                    m_about_box = m_about_box ?? new AboutBox();
                    m_about_box.Show();
                    m_about_box.Activate();
                    break;

                case MenuCommand.Download:
                    var url = Utils.IsInstalled ? Updater.Get("Installer")
                                                : Updater.Get("Portable");
                    Process.Start(url);
                    break;

                case MenuCommand.VisitWebsite:
                    Process.Start("http://wincompose.info/");
                    break;

                case MenuCommand.DonationPage:
                    Process.Start("http://wincompose.info/donate/");
                    break;

                case MenuCommand.Exit:
                    Application.Current.Shutdown();
                    break;
            }
        }

        private SequenceWindow m_sequencewindow;
        private SettingsWindow m_optionswindow;
        private DebugWindow m_debugwindow;
        private AboutBox m_about_box;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Frames of the composing animation, and how long each is shown.
        /// A diamond has 4-fold symmetry, so it cannot be SEEN to rotate:
        /// turning it 90 degrees is the identity. One lit quarter walking
        /// clockwise around the dark legend is what reads as motion instead.
        /// </summary>
        private const int SpinFrames = 4;
        private const int SpinFrameMs = 140;

        /// <summary>
        /// Whether the composing state animates at all. Off when the user has
        /// turned Windows animations off, and off when the icon is hidden --
        /// there is nothing to animate then, and the frames would still cost a
        /// Shell_NotifyIcon call each.
        /// </summary>
        private static bool Animate
            => !Settings.DisableIcon.Value && SystemParameters.ClientAreaAnimation;

        private static readonly Stopwatch m_spin_clock = new Stopwatch();

        /// <summary>
        /// Run the animation clock while a sequence is in progress. Restarting
        /// it means every compose begins on the same quarter; a free-running
        /// clock would start each one at an arbitrary phase, and a sequence is
        /// usually over in well under one turn.
        /// </summary>
        private static void UpdateSpinClock()
        {
            bool spin = Composer.IsComposing && Animate;
            if (spin == m_spin_clock.IsRunning)
                return;
            if (spin)
                m_spin_clock.Restart();
            else
                m_spin_clock.Reset();
        }

        /// <summary>
        /// Which quarter is lit. Read from a clock rather than counted per
        /// tick: CompositionTarget.Rendering is the render loop, not a
        /// metronome, so counting ticks would make the animation run at
        /// whatever rate WPF happens to be drawing at. Reading the clock makes
        /// an irregular tick drop a frame instead of changing the speed.
        /// </summary>
        private static int CurrentSpinFrame
            => m_spin_clock.IsRunning
                 ? (int)(m_spin_clock.ElapsedMilliseconds / SpinFrameMs) % SpinFrames
                 : 0;

        private static int CurrentIconIndex
            => (Composer.IsComposing?    0x1 : 0x0) |
               (Updater.HasNewerVersion? 0x2 : 0x0) |
               (CurrentSpinFrame << 2);

        public static System.Drawing.Icon GetIcon(int index)
        {
            if (m_icon_cache == null)
                m_icon_cache = new System.Drawing.Icon[4 * SpinFrames];

            if (m_icon_cache[index] == null)
            {
                bool is_composing = (index & 0x1) != 0;
                bool has_update = (index & 0x2) != 0;
                int frame = index >> 2;

                // XXX: if you create new bitmap images here instead of using bitmaps from
                // resources, make sure the DPI settings match. Our PNGs are 72 DPI whereas
                // new Bitmap objects appear to use 96 by default (even if copy-constructed).
                // A reasonable workaround might be to use Clone().
                using (Bitmap bitmap = Properties.Resources.KeyEmpty)
                using (Graphics canvas = Graphics.FromImage(bitmap))
                {
                    // The legend is the dark diamond in both states; composing
                    // lights one quarter of it and walks that quarter clockwise.
                    canvas.DrawImage(Properties.Resources.DecalActive, 0, 0);
                    if (is_composing)
                        canvas.DrawImage(SpinDecal(frame), 0, 0);

                    // Tiny yellow exclamation mark to advertise updates
                    if (has_update)
                        canvas.DrawImage(Properties.Resources.DecalUpdate, 0, 0);

                    canvas.Save();
                    m_icon_cache[index] = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                }
            }

            return m_icon_cache[index];
        }

        /// <summary>
        /// The lit quarter for a frame. A switch rather than a name looked up
        /// in the ResourceManager, so that renaming one of these is a build
        /// error instead of a blank quarter at run time.
        /// </summary>
        private static Bitmap SpinDecal(int frame)
        {
            switch (frame)
            {
                case 1:  return Properties.Resources.DecalSpin1;
                case 2:  return Properties.Resources.DecalSpin2;
                case 3:  return Properties.Resources.DecalSpin3;
                default: return Properties.Resources.DecalSpin0;
            }
        }

        private static System.Drawing.Icon[] m_icon_cache;

        private int m_icon_index = -1;

        private misc.AtomicFlag m_dirty;

        private void MarkIconDirty() => m_dirty.Set();

        private void UpdateNotificationIcon(object o, EventArgs e)
        {
            // The animation has to advance whether or not anything marked the
            // icon dirty, so this part runs on every tick. It is a clock read
            // and an int compare; the dirty-gated work below is unaffected.
            UpdateSpinClock();
            var index = CurrentIconIndex;
            if (index != m_icon_index)
            {
                m_icon_index = index;
                // Assigning Icon is one Shell_NotifyIcon NIM_MODIFY (see
                // TaskbarIcon.Icon in wpf-notifyicon) -- NOT a delete and
                // re-add, so it neither moves the icon nor makes it flicker,
                // and animating through it is what that call is for. Still
                // only assign on a real change: this event is the WPF render
                // loop, which runs far faster than the animation.
                Icon = GetIcon(index);
            }

            if (m_dirty.Get())
            {
                // Only assign on an actual change: this runs on every dirty
                // tick, and each assignment makes the tray icon be removed and
                // re-added.
                var wanted = Settings.DisableIcon.Value ? Visibility.Collapsed
                                                        : Visibility.Visible;
                if (Visibility != wanted)
                    Visibility = wanted;

                CurrentToolTip = GetCurrentToolTip();
            }
        }

        private string m_current_tooltip;

        public string CurrentToolTip
        {
            get => m_current_tooltip;
            set
            {
                if (value == m_current_tooltip) return;
                m_current_tooltip = value;
                // The name must match the property exactly; WPF compares it
                // ordinally, so a case mismatch silently drops the update.
                OnPropertyChanged(nameof(CurrentToolTip));
            }
        }

        private static string GetCurrentToolTip()
        {
            var ret = string.Format(i18n.Text.TrayToolTip,
                                    Settings.ComposeKeys.Value.FriendlyName,
                                    Settings.SequenceCount,
                                    Settings.Version);

            if (Updater.HasNewerVersion)
                ret += "\n" + i18n.Text.UpdatesToolTip;

            return ret;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this , new PropertyChangedEventArgs(propertyName));
        }


        public void NotifyiconDoubleclicked(object sender, EventArgs e)
        {
            m_sequencewindow = m_sequencewindow ?? new SequenceWindow();

            if (m_sequencewindow.IsVisible)
            {
                m_sequencewindow.Hide();
            }
            else
            {
                m_sequencewindow.Show();
                m_sequencewindow.Activate();
            }
        }

        public bool HasNewerVersion => Updater.HasNewerVersion;
        public string DownloadHeader => string.Format(i18n.Text.Download, Updater.Get("Latest") ?? "");

        private void UpdaterStateChanged()
        {
            PropertyChanged?.Invoke(this , new PropertyChangedEventArgs(nameof(HasNewerVersion)));
            PropertyChanged?.Invoke(this , new PropertyChangedEventArgs(nameof(DownloadHeader)));
        }

        private void OnExitEvent() => Application.Current.Shutdown();

        private void OnOpenEvent(MenuCommand cmd) => OnCommand(cmd);
    }
}
