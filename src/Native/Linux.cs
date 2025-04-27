using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.Versioning;

using Avalonia;

namespace SourceGit.Native
{
    [SupportedOSPlatform("linux")]
    internal class Linux : OS.IBackend
    {
        public void SetupApp(AppBuilder builder)
        {
            builder.With(new X11PlatformOptions() { EnableIme = true });
        }

        public string FindGitExecutable()
        {
            return FindExecutable("git");
        }

        public string FindTerminal(Models.ShellOrTerminal shell)
        {
            if (shell.Type.Equals("custom", StringComparison.Ordinal))
                return string.Empty;

            return FindExecutable(shell.Exec);
        }

        public Models.ExternalToolsFinder FindExternalTools()
        {
            var finder = new Models.ExternalToolsFinder();
            
            // Add standard editor tools using ExternalToolInfo objects
            finder.AddEditorTool(new Models.ExternalToolsFinder.EditorToolInfo 
            { 
                Name = "Visual Studio Code", 
                IconName = "vscode", 
                LocationFinder = () => FindExecutable("code") 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.EditorToolInfo 
            { 
                Name = "Visual Studio Code - Insiders", 
                IconName = "vscode_insiders", 
                LocationFinder = () => FindExecutable("code-insiders") 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.EditorToolInfo 
            { 
                Name = "VSCodium", 
                IconName = "codium", 
                LocationFinder = () => FindExecutable("codium") 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.EditorToolInfo 
            { 
                Name = "Fleet", 
                IconName = "fleet", 
                LocationFinder = FindJetBrainsFleet 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.EditorToolInfo 
            { 
                Name = "Sublime Text", 
                IconName = "sublime_text", 
                LocationFinder = () => FindExecutable("subl") 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.EditorToolInfo 
            { 
                Name = "Zed", 
                IconName = "zed", 
                LocationFinder = () => FindExecutable("zeditor") 
            });
            
            finder.FindJetBrainsFromToolbox(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/JetBrains/Toolbox");
            
            return finder;
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
