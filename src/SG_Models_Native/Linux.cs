using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using SourceGit.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Sausa;

namespace SourceGit.Native
{
    // Original file: src/SG_Models_Native/Linux.cs
    [SupportedOSPlatform("linux")]
    internal class Linux : IOSPlatform, IApplicationSetup, IFileOpener, IExternalTools, IProcessLauncher
    {
        // Implementation for IOSPlatform interface
        void IApplicationSetup.SetupApp(object builder)
        {
            var appBuilder = PlatformAdapters.AsAppBuilder(builder);
            appBuilder.With(new X11PlatformOptions() { EnableIme = true });
        }
        
        // Implementation for OS.IBackend interface
        // Original file: src/SG_Models_Native/Linux.cs Linux.SetupApp
        public void SetupApp(AppBuilder builder)
        {
            // Call our new implementation
            ((IApplicationSetup)this).SetupApp(builder);
        }

        // Implementation for IOSPlatform interface
        void IApplicationSetup.SetupWindow(object window)
        {
            var avWindow = PlatformAdapters.AsWindow(window);
            SetupWindow(avWindow);
        }
        
        // Implementation for OS.IBackend interface
        // Original file: src/SG_Models_Native/Linux.cs Linux.SetupWindow
        public void SetupWindow(Window window)
        {
            if (OS.UseSystemWindowFrame)
            {
                window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
                window.ExtendClientAreaToDecorationsHint = false;
            }
            else
            {
                window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
                window.ExtendClientAreaToDecorationsHint = true;
                window.ExtendClientAreaTitleBarHeightHint = -1;
                window.Classes.Add("custom_window_frame");
            }
        }

        public string FindGitExecutable()
        {
            return FindExecutable("git");
        }

        public string FindTerminal(ShellOrTerminal shell)
        {
            if (shell.Type.Equals("custom", StringComparison.Ordinal))
                return string.Empty;

            return FindExecutable(shell.Exec);
        }

        public Sausa.ExternalToolsFinder FindExternalTools()
        {
            // Original implementation from src/SG_Models_Native/Linux.cs Linux.FindExternalTools
            var finder = new Models.ExternalToolsFinder2();

            // Define standard editor tools with their custom LocationFinder delegates
            ExternalToolInfo2[] editorTools =
            [
                new ExternalToolInfo2(
                    Name: "Visual Studio Code",
                    LocationFinder: () => FindExecutable("code")
                ),
                new ExternalToolInfo2(
                    Name: "Visual Studio Code - Insiders",
                    LocationFinder: () => FindExecutable("code-insiders")
                ),
                new ExternalToolInfo2(
                    Name: "VSCodium",
                    LocationFinder: () => FindExecutable("codium")
                ),
                new ExternalToolInfo2(
                    Name: "Fleet",
                    LocationFinder: FindJetBrainsFleet
                ),
                new ExternalToolInfo2(
                    Name: "Sublime Text",
                    LocationFinder: () => FindExecutable("subl")
                ),
                new ExternalToolInfo2(
                    Name: "Zed",
                    LocationFinder: () => FindExecutable("zeditor")
                )
            ];

            // Add all editor tools in a loop
            foreach (var tool in editorTools)
                finder.AddEditorTool(tool);

            finder.FindJetBrainsFromToolbox(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/JetBrains/Toolbox");

            // Convert to Sausa namespace using adapter
            return PlatformAdapters.AsExternalToolsFinder(finder);
        }

        public void OpenBrowser(string url)
        {
            Process.Start("xdg-open", $"\"{url}\"");
        }

        public void OpenInFileManager(string path, bool select)
        {
            if (Directory.Exists(path))
            {
                Process.Start("xdg-open", $"\"{path}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir))
                    Process.Start("xdg-open", $"\"{dir}\"");
            }
        }

        public void OpenTerminal(string workdir)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var cwd = string.IsNullOrEmpty(workdir) ? home : workdir;
            var terminal = OS.ShellOrTerminal;

            var startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = cwd;
            startInfo.FileName = terminal;

            if (terminal.EndsWith("wezterm", StringComparison.OrdinalIgnoreCase))
                startInfo.Arguments = $"start --cwd \"{cwd}\"";
            else if (terminal.EndsWith("ptyxis", StringComparison.OrdinalIgnoreCase))
                startInfo.Arguments = $"--new-window --working-directory=\"{cwd}\"";

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                App.RaiseException(workdir, $"Failed to start '{OS.ShellOrTerminal}'. Reason: {e.Message}");
            }
        }

        public void OpenWithDefaultEditor(string file)
        {
            var proc = Process.Start("xdg-open", $"\"{file}\"");
            if (proc != null)
            {
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                    App.RaiseException("", $"Failed to open \"{file}\"");

                proc.Close();
            }
        }

        private string FindExecutable(string filename)
        {
            var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathes = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in pathes)
            {
                var test = Path.Combine(path, filename);
                if (File.Exists(test))
                    return test;
            }

            return string.Empty;
        }

        private string FindJetBrainsFleet()
        {
            var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/JetBrains/Toolbox/apps/fleet/bin/Fleet";
            return File.Exists(path) ? path : FindExecutable("fleet");
        }
    }
}
