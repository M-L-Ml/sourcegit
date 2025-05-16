using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Sausa;
using OS = SourceGit.Native.OS;
using ExternalToolsFinder = Sausa.ExternalToolsFinder;
using SourceGit.Models;

namespace SourceGit.Native
{
    // Original file: src/SG_Models_Native/MacOS.cs
    [SupportedOSPlatform("macOS")]
    internal partial class MacOS : IOSPlatform, IApplicationSetup, IFileSystem, IExternalTools, IProcessLauncher
    {
        public void SetupApp(object builder)
        {
            var appBuilder = PlatformAdapters.AsAppBuilder(builder);
            appBuilder.With(new MacOSPlatformOptions()
            {
                DisableDefaultApplicationMenuItems = true,
            });

            // Fix `PATH` env on macOS.
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                path = "/opt/homebrew/bin:/opt/homebrew/sbin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";
            else if (!path.Contains("/opt/homebrew/", StringComparison.Ordinal))
                path = "/opt/homebrew/bin:/opt/homebrew/sbin:" + path;

            var customPathFile = Path.Combine(OS.DataDir, "PATH");
            if (File.Exists(customPathFile))
            {
                var env = File.ReadAllText(customPathFile).Trim();
                if (!string.IsNullOrEmpty(env))
                    path = env;
            }

            Environment.SetEnvironmentVariable("PATH", path);
        }

        public void SetupWindow(object window)
        {
            // Original implementation from src/SG_Models_Native/MacOS.cs MacOS.SetupWindow
            var avWindow = PlatformAdapters.AsWindow(window);
            avWindow.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.SystemChrome;
            avWindow.ExtendClientAreaToDecorationsHint = true;
        }

        public string FindGitExecutable()
        {
            var gitPathVariants = new List<string>() {
                 "/usr/bin/git", "/usr/local/bin/git", "/opt/homebrew/bin/git", "/opt/homebrew/opt/git/bin/git"
            };
            foreach (var path in gitPathVariants)
                if (File.Exists(path))
                    return path;
            return string.Empty;
        }

        public string FindTerminal(Sausa.ShellOrTerminal shell)
        {
            switch (shell.Type)
            {
                case "mac-terminal":
                    return "Terminal";
                case "iterm2":
                    return "iTerm";
                case "warp":
                    return "Warp";
                case "ghostty":
                    return "Ghostty";
                case "kitty":
                    return "kitty";
            }

            return string.Empty;
        }

        // FindExternalTools implementation is in the partial class MacOS_FindExternalTools.cs

        public void OpenBrowser(string url)
        {
            Process.Start("open", url);
        }

        public void OpenInFileManager(string path, bool select)
        {
            if (Directory.Exists(path))
                Process.Start("open", $"\"{path}\"");
            else if (File.Exists(path))
                Process.Start("open", $"\"{path}\" -R");
        }

        public void OpenTerminal(string workdir)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dir = string.IsNullOrEmpty(workdir) ? home : workdir;
            Process.Start("open", $"-a {OS.ShellOrTerminal} \"{dir}\"");
        }

        public void OpenWithDefaultEditor(string file)
        {
            Process.Start("open", $"\"{file}\"");
        }
    }
}
