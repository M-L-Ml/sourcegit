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
    public static class App
    {
        //public static void ShowWindow(object data, bool showAsDialog)
        //=>
        //    AppUtilities.ShowWindow(data, showAsDialog);
    //}
    //public static class AppUtilities
    //{
        // Static property to encapsulate Application.Current as SourceGit.App
        private static ViewModels.Launcher _launcher = null;

        public static void ShowWindow(object data, bool showAsDialog)
        {
            dynamic appd = Application.Current;
            appd.ShowWindow(data, showAsDialog);
        }
        public static void RaiseException(string context, string message)
        {
            if (_launcher != null)
                _launcher.DispatchNotification(context, message, true);
        }

        public static void SendNotification(string context, string message)
        {
            if (_launcher != null)
                _launcher.DispatchNotification(context, message, false);

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

        //    public static void TryLaunchAsNormal(IClassicDesktopStyleApplicationLifetime desktop)
        //    {
        //        Native.OS.SetupEnternalTools();
        //        Models.AvatarManager.Instance.Start();

        //        string startupRepo = null;
        //        if (desktop.Args != null && desktop.Args.Length == 1 && Directory.Exists(desktop.Args[0]))
        //            startupRepo = desktop.Args[0];

        //        var pref = Preferences.Instance;
        //        pref.SetCanModify();

        //        var launcher = _launcher = new Launcher(startupRepo);
        //        if (desktop.MainWindow is IDisposable disposable)
        //            disposable.Dispose();
        //        desktop.MainWindow = new Views.Launcher() { DataContext = launcher };

        //#if !DISABLE_UPDATE_DETECTION
        //        if (pref.ShouldCheck4UpdateOnStartup())
        //            AppUtilities.Check4Update();
        //#endif
        //    }

        public static void TryOpenRepositoryInTab(ViewModels.RepositoryNode node, LauncherPage arg)
        {
            if (_launcher != null)
                _launcher.OpenRepositoryInTab(node, arg);
        }
    }
}
