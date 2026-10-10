# WPF-UI and themes

_Moved verbatim from `CLAUDE.md` on 2026-10-10. CLAUDE.md keeps a short pointer._

## WPF-UI: the traps that cost rounds

The UI is [WPF-UI](https://github.com/lepoco/wpfui) 3.0.5 (`WPF-UI` NuGet), merged
in `Application.xaml` as `ThemesDictionary` + `ControlsDictionary` + our own
`StyleOverrides.xaml`. Four things about it are not guessable from the API, and
each was found the slow way.

⚠️ **A WPF implicit style matches the EXACT type, and this bites TWICE over.**
`StyleOverrides.xaml` already records it for `emoji:TextBlock`, which is its own type and so missed
the 12px `TextBlock` setter. The window version is worse in both directions:

- WPF-UI's window styles are keyed `{x:Type FluentWindow}` / `{x:Type Window}`. A
  subclass gets one only because `FluentWindow` overrides
  `DefaultStyleKeyProperty` to `typeof(FluentWindow)` — that indirection, not the
  implicit lookup, is what carries the style down to `BaseWindow` and to
  `SettingsWindow` under it.
- A style **we** write keyed `{x:Type BaseWindow}` reaches nothing, because no
  window IS a `BaseWindow` — they are `SettingsWindow`, `SequenceWindow`,
  `AboutBox` and `KeySelector`. It fails silently: the window simply keeps the WPF
  default `SystemColors.WindowBrush`, which is white in BOTH themes, so in light
  it looks perfect and in dark it is white-on-white with only the colour emoji
  legible. `BaseWindow` asks for its style by name with
  `SetResourceReference(StyleProperty, typeof(BaseWindow))`; keep that line.

⚠️ **`FluentWindow` takes the caption over whatever `ExtendsContentIntoTitleBar`
says.** Its `OnSourceInitialized` calls
`OnExtendsContentIntoTitleBarChanged(default, ExtendsContentIntoTitleBar)`
unconditionally, and that handler ignores both values: it forces
`WindowStyle.SingleBorderWindow`, applies a `WindowChrome` with `CaptionHeight = 0`
and `GlassFrameThickness = -1`, then calls `RemoveWindowTitlebarContents`. So
clearing the property does **not** give you the caption Windows draws — deriving
from plain `Window` is what does, which is why `BaseWindow` does.

⚠️ **`WindowBackgroundManager.UpdateBackground` ends by setting
DWMWA_CAPTION_COLOR to "none".** That is right for a window drawing its own
`ui:TitleBar` and is exactly what stops Windows painting a caption for one that
is not. With the native caption, use `ApplyDarkThemeToWindow` /
`RemoveDarkThemeFromWindow` alone — DWMWA_USE_IMMERSIVE_DARK_MODE is then the only
attribute that is ours. Whichever you call, call it from `OnSourceInitialized`:
the caption-colour call needs an HWND and, unlike its neighbours, does not defer
itself to `Loaded` — it returns false and leaves the caption alone.

⚠️ **Changing a window's base class is a SWEEP, not an edit — there are five
windows and the C# names only three of them.** `SettingsWindow` and
`SequenceWindow` declare no base in their `.xaml.cs` (it comes from the XAML
root), so a grep of the C# finds `AboutBox`, `DebugWindow` and `KeySelector` and
misses the two biggest. That is how `DebugWindow` was left on plain `Window` when
the other four moved to `BaseWindow` (#19): WPF-UI's implicit `{x:Type Window}`
style still themed its body, so it was a dark window with a light caption, and
neither the build nor the tests can see that. Grep both:

```bash
grep -rn "class .*: *\(Window\|BaseWindow\)\b" --include=*.cs src/
grep -rn "^<[A-Za-z:]*\(BaseWindow\|Window\)\b" --include=*.xaml src/
```

⚠️ `RemoteControl` is the one deliberate exception: a message-only window hosting
the HWND hook, never shown, so it stays a plain `Window` and needs no theming.

**A hardcoded value inside a WPF-UI template can only be overridden by copying
that template.** `StyleOverrides.xaml` carries four such copies — `TabItem`
(MinWidth 180), `CheckBox` (a 22px glyph), `TabControl` (`Background="Transparent"`
on the header panel) and `MenuItem` (32px rows). Each is the theme's own template
verbatim with one value changed, and each says which one in a comment. ⚠️ A
template's **triggers do not come along with `BasedOn`**, so a copy that drops them
loses the hover highlight, the checked state and the rest.

## Themes

`theme_mode` in `settings.ini` is `System` (the default, and what an EMPTY value
means), `Light` or `Dark`. `Settings.SetTheme()` resolves it and **logs what it
resolved to** — before that the only trace was a `Debug.WriteLine`, so "it is
still bright everywhere" was answerable from no artifact at all. Read
`%LocalAppData%\WinCompose\wincompose.log` for `Theme: System resolved to Dark`
before theorising.

- ⚠️ **Empty used to fall through to Light**, silently, so WinCompose stayed white
  on a machine running Windows in dark mode — by design, with nothing saying so,
  and with the theme combo box showing blank because `""` matched neither entry.
  If a user says "I use the dark theme", ask whether they mean Windows or the app.
- A Windows theme switch arrives as **WM_SETTINGCHANGE with "ImmersiveColorSet"**
  in lParam. `RemoteControl` already owns an HWND hook for the life of the
  process, so that is where it is read; no watcher, no window to keep alive.
  ⚠️ lParam is a string pointer for only *some* values of that message, so it is
  read defensively — this drives the colours and must never be able to take the
  process down.
- ⚠️ **`theme_mode` does NOT move when Windows flips its theme, so nothing may
  key off the SETTING to react to a theme change.** `SystemThemeEvent` →
  `SetTheme()` → `ApplySystemTheme()` re-resolves and re-applies while
  `theme_mode` stays `System`, so `ThemeMode.ValueChanged` never fires. The auto
  mode shipped with `BaseWindow.ApplyThemeToFrame` bound to exactly that: an open
  window's content re-themed and its native caption did not, which is the dark
  content over a light title bar that whole change existed to remove. Subscribe
  to **`ApplicationThemeManager.Changed`** instead — WPF-UI raises it from inside
  `Apply()`, so the preference route and the Windows route both reach it, and it
  reports the theme that was applied rather than the one that was asked for.
  (Caught by CodeRabbit on #19, fixed in `33c8312`.)
- ⚠️ **Colours hardcoded in XAML are invisible to a scan of the images.** Two
  rounds went into "where is the old beige icon?" when the answer was that the
  sequence list draws its keycaps as XAML gradients, and six more literals
  (`Background="White"`, `Foreground="Black"`, `LightGray` borders) dated from
  when white was the only background a window had. When the icon or the theme
  changes, **grep the XAML for literal colours**, not just `res/`.
- **A stale taskbar icon is Windows, not us.** The exe carries no
  `<ApplicationIcon>`, no `.rc` and no `.res`: its icons come only from
  `InsertIcons.exe wincompose.exe res` after the build, and the tray icon is
  composited at runtime from `key_empty.png` plus a decal. `ie4uinit.exe -show`,
  or a reboot, is the fix.

## The tray icon animates while composing

Idle is the cap with the **dark** diamond; composing lights one quarter of that
diamond and walks it clockwise at 280 ms a frame (`SpinFrames` / `SpinFrameMs`
in `NotificationIcon.xaml.cs`). The full account — why a diamond cannot be seen
to rotate, and the measured still-pair table this replaced — is in
`art/README.md`. Three things that are easy to get wrong:

- ⚠️ **`TaskbarIcon.Icon` is ONE `Shell_NotifyIcon` NIM_MODIFY, not a delete
  and re-add.** wpf-notifyicon's setter writes `iconData.IconHandle` then calls
  `Util.WriteIconData(ref iconData, NotifyCommand.Modify, …)`
  (`src/NotifyIconWpf/TaskbarIcon.Declarations.cs`). So animating through it
  costs nothing, and the icon neither flickers nor moves. **Our own comment
  said the opposite**, which is exactly the kind of note that
  talks the next person out of a change that was always cheap — the *Visibility*
  setter is the one that really does delete and re-add, and that comment is
  correct where it sits, three lines below.
- ⚠️ **Cache every frame.** `Icon.FromHandle(bitmap.GetHicon())` never destroys
  the handle, so building a frame per tick leaks an HICON every 280 ms. The
  cache is indexed by the state bits plus the frame (`index >> 2`).
- ⚠️ **Drive the frame off a `Stopwatch`, not a tick count.**
  `CompositionTarget.Rendering` is the WPF render loop, not a metronome;
  counting ticks makes the animation run at whatever rate WPF happens to be
  drawing at, where reading a clock drops a frame instead.

`Animate` is off when Windows animations are off (`SystemParameters.
ClientAreaAnimation`) or the icon is hidden; the composing icon is then the
static frame 0, which is still a lit quarter and so still differs from idle.
