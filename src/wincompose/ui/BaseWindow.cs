//
//  WinCompose — a compose key for Windows — http://wincompose.info/
//
//  Copyright © 2013—2019 Sam Hocevar <sam@hocevar.net>
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace WinCompose
{
    /// <summary>
    /// A plain WPF window, so WINDOWS draws the caption: it is then the colour
    /// the user picked for every other title bar on the machine, it dims when
    /// the window goes inactive, and it follows dark mode and high contrast
    /// with nothing to match by hand.
    ///
    /// This used to derive from Wpf.Ui.Controls.FluentWindow, which takes the
    /// caption over (WindowChrome with CaptionHeight 0, then
    /// RemoveWindowTitlebarContents) and leaves the app to draw its own
    /// ui:TitleBar — those are gone with it. Note FluentWindow does that from
    /// OnSourceInitialized whatever ExtendsContentIntoTitleBar says, so
    /// clearing the property would not have been enough.
    /// </summary>
    public class BaseWindow : Window
    {
        public BaseWindow()
        {
            // Ask for our own style by name. An implicit style matches the EXACT
            // type, and nothing here IS a BaseWindow - they are SettingsWindow,
            // SequenceWindow, AboutBox and KeySelector - so the Style keyed
            // {x:Type BaseWindow} in StyleOverrides would never have been applied
            // to any of them. This resolves it through the ordinary lookup chain
            // instead, which ends at Application.Resources, and every subclass
            // gets it.
            SetResourceReference(StyleProperty, typeof(BaseWindow));

            Closing += (o, e) => { Hide(); e.Cancel = true; };

            // The APPLIED theme, not the setting. ThemeMode.ValueChanged fires
            // only when the user picks a different entry, so a Windows theme
            // switch — which resolves through the same Apply() without touching
            // theme_mode — left an open window's content dark and its caption
            // light. WPF-UI raises this from inside Apply(), so both routes hit
            // it. Every window here is a cached singleton, so the subscription
            // lives as long as the process and needs no matching removal.
            ApplicationThemeManager.Changed += OnApplicationThemeChanged;
        }

        private void OnApplicationThemeChanged(ApplicationTheme theme, Color accent)
            => ApplyThemeToFrame();

        /// <summary>
        /// The one DWM attribute still ours to set: DWMWA_USE_IMMERSIVE_DARK_MODE,
        /// which darkens the caption Windows draws. Deliberately NOT
        /// WindowBackgroundManager.UpdateBackground, whose last act is to set
        /// DWMWA_CAPTION_COLOR to “none” — right when the app draws its own
        /// title bar, and precisely what would stop Windows painting this one.
        /// </summary>
        private void ApplyThemeToFrame()
        {
            if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
                WindowBackgroundManager.ApplyDarkThemeToWindow(this);
            else
                WindowBackgroundManager.RemoveDarkThemeFromWindow(this);
        }

        /// <summary>
        /// The earliest point at which the window has an HWND, which the DWM
        /// call needs, and the only route for a window opened AFTER the theme
        /// was applied — the event above fires at the moment of applying. It
        /// was never made at startup before: the two windows that themed their
        /// frame at all did it from ThemeMode.ValueChanged, so a window opened
        /// in the dark theme kept a light frame until the user toggled the
        /// setting.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyThemeToFrame();
        }
    }
}
