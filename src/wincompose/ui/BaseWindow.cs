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
using Wpf.Ui.Appearance;

namespace WinCompose
{
    public class BaseWindow : Wpf.Ui.Controls.FluentWindow
    {
        static BaseWindow()
        {
        }

        public BaseWindow()
        {
            Closing += (o, e) => { Hide(); e.Cancel = true; };

            // Every window here is a cached singleton, so this subscription
            // lives as long as the process and needs no matching removal.
            Settings.ThemeMode.ValueChanged += UpdateBackground;
        }

        /// <summary>
        /// Hand the window frame to the theme. Two DWM attributes are involved
        /// and neither is set for us: DWMWA_USE_IMMERSIVE_DARK_MODE, and
        /// DWMWA_CAPTION_COLOR, which WPF-UI sets to “none” so our own
        /// <c>ui:TitleBar</c> shows instead of the caption DWM would paint.
        /// FluentWindow only does this while applying a backdrop, and ours is
        /// <c>None</c>, so a window opened in the dark theme came up with a
        /// white caption strip above a #202020 body until the user toggled the
        /// theme — which is what ran this, through ValueChanged.
        /// </summary>
        private void UpdateBackground()
            => WindowBackgroundManager.UpdateBackground(this,
                   ApplicationThemeManager.GetAppTheme(),
                   Wpf.Ui.Controls.WindowBackdropType.None);

        /// <summary>
        /// The earliest point at which the window has an HWND, which
        /// DWMWA_CAPTION_COLOR needs: unlike the dark-mode and backdrop calls
        /// beside it, that one does not defer itself to Loaded — it returns
        /// false and leaves the caption alone.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            UpdateBackground();
        }
    }
}
