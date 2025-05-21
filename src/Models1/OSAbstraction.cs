using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Controls;
using Sausa;
using SourceGit.Models;

namespace SourceGit.Native
{
    public static partial class OS
    {
        public interface IBackend
        {
            void SetupApp(AppBuilder builder);
            void SetupWindow(Window window);

            string FindGitExecutable();
            string FindTerminal(ShellOrTerminal shell);
            ExternalToolsFinder2 FindExternalTools();

            void OpenTerminal(string workdir);
            void OpenInFileManager(string path, bool select);
            void OpenBrowser(string url);
            void OpenWithDefaultEditor(string file);
        }

        public static string DataDir
             => s_OSAbstraction.DataDir;

        public static string GitExecutable
        {
            get => s_OSAbstraction.GitExecutable;
            set => s_OSAbstraction.GitExecutable = value;
        }

        public static string GitVersionString
            => s_OSAbstraction.GitVersionString;

        public static Version GitVersion
            => s_OSAbstraction.GitVersion;
        public static string ShellOrTerminal
        {
            get => s_OSAbstraction.ShellOrTerminal;
            set => s_OSAbstraction.ShellOrTerminal = value;
        }
        public static IReadOnlyList<ExternalTool> ExternalTools
        => s_OSAbstraction.ExternalTools;

        public static bool UseSystemWindowFrame
        {
            get => s_OSAbstraction.UseSystemWindowFrame;
            set => s_OSAbstraction.UseSystemWindowFrame = value;
        }

        static OS()
        {
            // Create the platform-agnostic implementation
            Sausa.IOSPlatform platform;

            if (OperatingSystem.IsWindows())
            {
                platform = new Windows();
            }
            else if (OperatingSystem.IsMacOS())
            {
                platform = new MacOS();
            }
            else if (OperatingSystem.IsLinux())
            {
                platform = new Linux();
            }
            else
            {
                throw new NotSupportedException("Platform unsupported!!!");
            }

            // Create the adapter that implements IBackend
            _backend = new OSBackendAdapter(platform);
            s_OSAbstraction = new OSAbstraction(platform);
        }

        public static void SetupApp(AppBuilder builder)
        {
            s_OSAbstraction.SetupApp(builder);
        }

        public static void SetupDataDir()
        {
            s_OSAbstraction.SetupDataDir();
        }

        public static void SetupEnternalTools()
        {
            s_OSAbstraction.SetupExternalTools();
        }

        public static void SetupForWindow(Window window)
        {
            _backend.SetupWindow(window);
        }

        public static string FindGitExecutable()
        {
            return s_OSAbstraction.FindGitExecutable();
        }

        public static bool TestShellOrTerminal(ShellOrTerminal shell)
        {
            return !string.IsNullOrEmpty(_backend.FindTerminal(shell));
        }

        public static void SetShellOrTerminal(ShellOrTerminal shell)
        {
            s_OSAbstraction.SetShellOrTerminal(shell);
        }

        public static void OpenInFileManager(string path, bool select = false)
        {
            s_OSAbstraction.OpenInFileManager(path, select);
        }

        public static void OpenBrowser(string url)
        {
            s_OSAbstraction.OpenBrowser(url);
        }

        public static void OpenTerminal(string workdir)
        {
            if (string.IsNullOrEmpty(ShellOrTerminal))
                App.RaiseException(workdir, $"Terminal is not specified! Please confirm that the correct shell/terminal has been configured.");
            else
                s_OSAbstraction.OpenTerminal(workdir);
            //_backend.OpenTerminal(workdir);
        }

        public static void OpenWithDefaultEditor(string file)
        {
            s_OSAbstraction.OpenWithDefaultEditor(file);
        }

        public static string GetAbsPath(string root, string sub)
        {
            var fullpath = Path.Combine(root, sub);
            if (OperatingSystem.IsWindows())
                return fullpath.Replace('/', '\\');

            return fullpath;
        }

        public static void UpdateGitVersion()
        {

            s_OSAbstraction.UpdateGitVersion();

        }

        private static OSAbstraction s_OSAbstraction;
        private static IBackend _backend = null;
    }
}
