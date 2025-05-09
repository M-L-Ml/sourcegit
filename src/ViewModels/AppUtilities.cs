using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Data.Converters;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SourceGit.ViewModels
{
    public partial class App : Models.App
    {

        private static dynamic AppDyn =>
            Application.Current;

        public static void LogException(Exception ex)
        {
            if (AppDyn != null)
                AppDyn.LogExceptionI(ex);

            Debug.WriteLine(ex.Message);
        }

        public static void SetTheme(string theme, string themeOverridesFile)
        {
            AppDyn.SetTheme(theme, themeOverridesFile);
        }
        public static void SetLocale(string localeKey)
        {
            AppDyn.SetLocale(localeKey);
        }
        public static void SetFonts(string defaultFont, string monospaceFont, bool onlyUseMonospaceFontInEditor)
        {
            AppDyn.SetFonts(defaultFont, monospaceFont, onlyUseMonospaceFontInEditor);
        }


        public static new void RaiseException(string context, string message)
        {
            GetLauncer().DispatchNotification(context, message, true);
        }
        public static void RaiseException(string context, string messagef, Exception original)
        {
            string message = string.Format(messagef, original.Message);
            RaiseException(context, message);
        }
        public static void SendNotification(string context, string message)
        {
            GetLauncer().DispatchNotification(context, message, false);

        }

        public static async Task CopyText(string data)
        {
            if (CurrentDesktopAppLifetime is { } desktop)
            {
                if (desktop.MainWindow?.Clipboard is { } clipboard)
                    await clipboard.SetTextAsync(data ?? "");
            }
            else
                Debug.Assert(false, "Clipboard not available in this context.");
        }

        public static async Task<string> GetClipboardTextAsync()
        {
            if (CurrentDesktopAppLifetime is { } desktop)
            {
                if (desktop.MainWindow?.Clipboard is { } clipboard)
                {
                    return await clipboard.GetTextAsync();
                }
            }
            else
                Debug.Assert(false, "Clipboard not available in this context.");
            return default;
        }

        /// <summary> 
        /// TODO: refactor, it can be a part of a data template in an axaml
        /// first move it to Views
        /// <see cref="SourceGit.ViewModels.MenuItem.Header"/>
        /// </summary>
        /// <returns></returns>
        public static string Text(string key)
            => TextInternal(key);
        public static string Text(string key, params object[] args)
            => TextInternal(key, args);

        private static string TextInternal(string key, params object[] args)
        {
            var fmt = Application.Current?.FindResource($"Text.{key}") as string;
            if (string.IsNullOrWhiteSpace(fmt))
            {
                Debug.Assert(false, $"Text resource not found: {key}");
                //Debug.Assert(!(args == null || args.Length == 0), $"Text resource not found: {key}");
                return $"Text.{key}";
            }
            if (args == null || args.Length == 0)
                return fmt;

            return string.Format(fmt, args);
        }


        /// <summary> 
        /// TODO: refactor, it can be a part of a data template in an axaml
        /// first move it to Views
        /// <see cref="SourceGit.ViewModels.MenuItem.IconKey"/>
        /// </summary>
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
            if (CurrentDesktopAppLifetime is { } desktop)
                return desktop.MainWindow?.StorageProvider;

            return null;
        }


        /// <summary>
        /// <see cref="SourceGit.App.GetLauncherI"/>
        /// </summary>
        /// <returns></returns>
        public static Launcher GetLauncer()
        {

            return AppDyn.GetLauncherI();// Application.Current is SourceGit.App app ? app._launcher : null;
        }

        public static bool GetCurrentDesktopAppLifetime(out IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop1)
            {
                desktop = desktop1;
                return true;
            }
            Debug.Assert(false);
            desktop = null;
            return false;
        }
        [MaybeNull]
        public static IClassicDesktopStyleApplicationLifetime CurrentDesktopAppLifetime
        {
            get
            {
                // Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                if (GetCurrentDesktopAppLifetime(out var d))
                {
                    return d;
                }
                else
                {
                    return null;
                }
            }
        }

        public static void Quit(int exitCode)
        {
            if (CurrentDesktopAppLifetime is { } desktop)
            {
                desktop.MainWindow?.Close();
                desktop.Shutdown(exitCode);
            }
            else
            {
                Environment.Exit(exitCode);
            }
        }

        public static void TryOpenRepositoryInTab(ViewModels.RepositoryNode node, LauncherPage arg)
        {
            GetLauncer().OpenRepositoryInTab(node, arg);
        }

        /// <summary>
        /// This is a stub do nothing , but maybe it will return some class in future
        /// or just check if it's a key.
        /// <see cref="App.Text"/> App.Text(stringKey);
        /// <param name="stringKey">for <see cref="App.Text"/> </param>
        /// </summary>
        public static StringResource ResText(string key, params object[] args)
        {
            return new StringResource(key, args);
        }

        /// <summary>
        ///  <see cref="App.CreateMenuIcon"/> <code>App.CreateMenuIcon(iconKey);</code>
        /// </summary>
        public static string MenuIconKey(string iconKey)
        {
            return iconKey;
        }
    }

    //namespace SourceGit.Converters

    /// <summary>
    /// TODO: FormatByResourceKeyConverter describe , refactor.
    /// </summary>
    public class FormatByResourceKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = parameter as string;
            return ViewModels.App.Text(key, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public static readonly FormatByResourceKeyConverter FormatByResourceKey = new FormatByResourceKeyConverter();
    }

    public record struct StringResource(string Key, params object[] Args)
    {
        //TODO: remove this operators in future
        // Implicit conversion from MyRecord to int
        public static implicit operator string(StringResource r) => r.Text();

        // Explicit conversion from int to MyRecord
        public static implicit operator StringResource(string s) => new StringResource(s, string.Empty);

        public bool DontLookUpResource => Args?.Length == 1 && (Args[0] is string s && (s == string.Empty));
        public override string ToString() =>Key + (DontLookUpResource ? " |" : " (Key) ") + nameof(StringResource) + ". ";
        public string Text()
        {
            if (DontLookUpResource)
            {
                //TODO: remove assert.
                Debug.Assert(!Regex.IsMatch(Key, @"\w*\.\w*", RegexOptions.IgnoreCase),  "check");
                return Key;
            }
            return ViewModels.App.Text(Key, Args);
        }
    }
    public static class ObsSExtensions
    {

        public static IStorageProvider GetStorageProvider(this CommunityToolkit.Mvvm.ComponentModel.ObservableObject _)
        {
            return ViewModels.App.GetStorageProvider();
        }
    }
}
