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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Interop;

namespace WinCompose
{
    public class RemoteControl : Window
    {
        public RemoteControl()
        {
            // Cannot set ShowInTaskbar = false, or WndProc will not be handled
            // correctly (maybe because we use HWND_BROADCAST).
            ShowActivated = false;
            Width = Height = 0;
            WindowState = WindowState.Minimized;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Opacity = 0.0;

            SourceInitialized += (o, e) =>
                (PresentationSource.FromVisual(this) as HwndSource).AddHook(WndProc);

            // Allow these messages to reach us even from a process with a
            // lower integrity level. This may happen when WinCompose was
            // launched at high level (e.g. through the installer) but
            // wincompose-settings.exe is launched at medium level (through
            // the start menu). The security risk seems very low since all
            // we do is open an existing window.
            // Note: this function does not exist on Windows XP
            if (NativeMethods.EXISTS("user32", "ChangeWindowMessageFilter"))
                NativeMethods.ChangeWindowMessageFilter(WM_WINCOMPOSE.OPEN, MSGFLT.ADD);

            Show();
            Hide();
        }

        [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
        protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINCOMPOSE.EXIT)
            {
                if (Process.GetCurrentProcess().Id != (int)wParam)
                    ExitEvent?.Invoke();
                handled = true;
            }
            else if (msg == WM_WINCOMPOSE.OPEN)
            {
                OpenEvent?.Invoke((MenuCommand)wParam);
                handled = true;
            }
            else if (msg == (int)WM.SETTINGCHANGE)
            {
                // Windows broadcasts this to every top-level window with
                // “ImmersiveColorSet” in lParam when the user switches between
                // the light and dark app theme. This window is the one that is
                // up for the life of the process and already has a hook, so it
                // is where we hear about it. Deliberately not marked handled:
                // the message is a broadcast and none of it is ours.
                if (IsImmersiveColorSet(lParam))
                    SystemThemeEvent?.Invoke();
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// lParam is a string pointer for some WM_SETTINGCHANGE values and
        /// something else entirely for others, so read it defensively: this
        /// drives nothing but the app's colours, and must never be able to take
        /// the process down.
        /// </summary>
        private static bool IsImmersiveColorSet(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
                return false;

            try
            {
                return Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public event Action ExitEvent;
        public event Action<MenuCommand> OpenEvent;

        /// <summary>Raised when Windows changes its light/dark app theme.</summary>
        public event Action SystemThemeEvent;
    }
}

