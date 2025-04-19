using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Fonts;
using SourceGit.ViewModels;
using Native = SourceGit.Native;

namespace SourceGit.ViewModels
{
    public static class AppUtilities
    {
        // Static property to encapsulate Application.Current as SourceGit.App
        public static SourceGit.App? CurrentApp => Application.Current as SourceGit.App;
        private static ViewModels.Launcher _launcher = null;

        public static void OpenDialog(Window window)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } owner })
                window.ShowDialog(owner);
        }

        public static void RaiseException(string context, string message)
        {
            if (_launcher() != null)
                _launcher.DispatchNotification(context, message, true);
        }

        public static void SendNotification(string context, string message)
        {
            if (_launcher != null)
                _launcher.DispatchNotification(context, message, false);
        }

        public static void SetLocale(string localeKey)
        {
            var app = CurrentApp;
            if (app == null)
                return;

            var targetLocale = app.Resources[localeKey] as ResourceDictionary;
            if (targetLocale == null || targetLocale == app._activeLocale)
                return;

            if (app._activeLocale != null)
                app.Resources.MergedDictionaries.Remove(app._activeLocale);

            app.Resources.MergedDictionaries.Add(targetLocale);
            app._activeLocale = targetLocale;
        }

        public static void SetTheme(string theme, string themeOverridesFile)
        {
            var app = CurrentApp;
            if (app == null)
                return;

            if (theme.Equals("Light", StringComparison.OrdinalIgnoreCase))
                app.RequestedThemeVariant = ThemeVariant.Light;
            else if (theme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                app.RequestedThemeVariant = ThemeVariant.Dark;
            else
                app.RequestedThemeVariant = ThemeVariant.Default;

            if (app._themeOverrides != null)
            {
                app.Resources.MergedDictionaries.Remove(app._themeOverrides);
                app._themeOverrides = null;
            }

            if (!string.IsNullOrEmpty(themeOverridesFile) && File.Exists(themeOverridesFile))
            {
                try
                {
                    var resDic = new ResourceDictionary();
                    var overrides = JsonSerializer.Deserialize(File.ReadAllText(themeOverridesFile), JsonCodeGen.Default.ThemeOverrides);
                    foreach (var kv in overrides.BasicColors)
                    {
                        if (kv.Key.Equals("SystemAccentColor", StringComparison.Ordinal))
                            resDic["SystemAccentColor"] = kv.Value;
                        else
                            resDic[$"Color.{kv.Key}"] = kv.Value;
                    }

                    if (overrides.GraphColors.Count > 0)
                        Models.CommitGraph.SetPens(overrides.GraphColors, overrides.GraphPenThickness);
                    else
                        Models.CommitGraph.SetDefaultPens(overrides.GraphPenThickness);

                    Models.Commit.OpacityForNotMerged = overrides.OpacityForNotMergedCommits;

                    app.Resources.MergedDictionaries.Add(resDic);
                    app._themeOverrides = resDic;
                }
                catch
                {
                    // ignore
                }
            }
            else
            {
                Models.CommitGraph.SetDefaultPens();
            }
        }

        public static void SetFonts(string defaultFont, string monospaceFont, bool onlyUseMonospaceFontInEditor)
        {
            var app = CurrentApp;
            if (app == null)
                return;

            if (app._fontsOverrides != null)
            {
                app.Resources.MergedDictionaries.Remove(app._fontsOverrides);
                app._fontsOverrides = null;
            }

            defaultFont = app.FixFontFamilyName(defaultFont);
            monospaceFont = app.FixFontFamilyName(monospaceFont);

            var resDic = new ResourceDictionary();
            if (!string.IsNullOrEmpty(defaultFont))
                resDic.Add("Fonts.Default", new FontFamily(defaultFont));

            if (string.IsNullOrEmpty(monospaceFont))
            {
                if (!string.IsNullOrEmpty(defaultFont))
                {
                    monospaceFont = $"fonts:SourceGit#JetBrains Mono,{defaultFont}";
                    resDic.Add("Fonts.Monospace", new FontFamily(monospaceFont));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(defaultFont) && !monospaceFont.Contains(defaultFont, StringComparison.Ordinal))
                    monospaceFont = $"{monospaceFont},{defaultFont}";

                resDic.Add("Fonts.Monospace", new FontFamily(monospaceFont));
            }

            if (onlyUseMonospaceFontInEditor)
            {
                if (string.IsNullOrEmpty(defaultFont))
                    resDic.Add("Fonts.Primary", new FontFamily("fonts:Inter#Inter"));
                else
                    resDic.Add("Fonts.Primary", new FontFamily(defaultFont));
            }
            else
            {
                if (!string.IsNullOrEmpty(monospaceFont))
                    resDic.Add("Fonts.Primary", new FontFamily(monospaceFont));
            }

            if (resDic.Count > 0)
            {
                app.Resources.MergedDictionaries.Add(resDic);
                app._fontsOverrides = resDic;
            }
        }

        public static async void CopyText(string data)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.Clipboard is { } clipboard)
                    await clipboard.SetTextAsync(data ?? "");
            }
        }

        public static async Task<string> GetClipboardTextAsync()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.Clipboard is { } clipboard)
                {
                    return await clipboard.GetTextAsync();
                }
            }
            return default;
        }

        public static string Text(string key, params object[] args)
        {
            var fmt = Application.Current?.FindResource($"Text.{key}") as string;
            if (string.IsNullOrWhiteSpace(fmt))
                return $"Text.{key}";

            if (args == null || args.Length == 0)
                return fmt;

            return string.Format(fmt, args);
        }

        public static Avalonia.Controls.Shapes.Path CreateMenuIcon(string key)
        {
            var icon = new Avalonia.Controls.Shapes.Path();
            icon.Width = 12;
            icon.Height = 12;
            icon.Stretch = Stretch.Uniform;

            var geo = Application.Current?.FindResource(key) as Avalonia.Media.StreamGeometry;
            if (geo != null)
                icon.Data = geo;

            return icon;
        }

        public static IStorageProvider GetStorageProvider()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow?.StorageProvider;

            return null;
        }

        public static Launcher GetLauncer()
        {
            return _launcher;// Application.Current is SourceGit.App app ? app._launcher : null;
        }

        public static void Quit(int exitCode)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Close();
                desktop.Shutdown(exitCode);
            }
            else
            {
                Environment.Exit(exitCode);
            }
        }

        public static void TryLaunchAsNormal(IClassicDesktopStyleApplicationLifetime desktop)
        {
            Native.OS.SetupEnternalTools();
            Models.AvatarManager.Instance.Start();

            string startupRepo = null;
            if (desktop.Args != null && desktop.Args.Length == 1 && Directory.Exists(desktop.Args[0]))
                startupRepo = desktop.Args[0];

            var pref = Preferences.Instance;
            pref.SetCanModify();

            var launcher = _launcher = new Launcher(startupRepo);
            if (desktop.MainWindow is IDisposable disposable)
                disposable.Dispose();
            desktop.MainWindow = new Views.Launcher() { DataContext = launcher };

    #if !DISABLE_UPDATE_DETECTION
            if (pref.ShouldCheck4UpdateOnStartup())
                AppUtilities.Check4Update();
    #endif
        }

        public static void TryOpenRepositoryInTab(object node, object arg)
        {
            if (CurrentApp != null && CurrentApp._launcher != null)
                CurrentApp._launcher.OpenRepositoryInTab(node, arg);
        }
    }
}
