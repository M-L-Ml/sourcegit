using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SourceGit;
using SourceGit.Models;

namespace Sausa
{
    /// <summary>
    /// Provides OS abstraction services through dependency inversion.
    /// This class replaces the static OS class with a proper dependency-injected service.
    /// </summary>
    public class OSAbstraction
    {
        private readonly IOSPlatform _platform;
        private string _gitExecutable = string.Empty;
        private bool _enableSystemWindowFrame = false;

        /// <summary>
        /// Directory for application data storage
        /// </summary>
        public string DataDir { get; private set; } = string.Empty;

        /// <summary>
        /// Path to the Git executable
        /// </summary>
        public string GitExecutable
        {
            get => _gitExecutable;
            set
            {
                if (_gitExecutable != value)
                {
                    _gitExecutable = value;
                    UpdateGitVersion();
                }
            }
        }

        /// <summary>
        /// Git version string
        /// </summary>
        public string GitVersionString { get; private set; } = string.Empty;

        /// <summary>
        /// Git version as a Version object
        /// </summary>
        public Version GitVersion { get; private set; } = new Version(0, 0, 0);

        /// <summary>
        /// Path to the configured shell or terminal
        /// </summary>
        public string ShellOrTerminal { get; set; } = string.Empty;

        /// <summary>
        /// List of available external tools
        /// </summary>
        public List<ExternalTool> ExternalTools { get; set; } = [];

        /// <summary>
        /// Whether to use the system window frame on Linux
        /// </summary>
        public bool UseSystemWindowFrame
        {
            get => OperatingSystem.IsLinux() && _enableSystemWindowFrame;
            set => _enableSystemWindowFrame = value;
        }

        /// <summary>
        /// Creates a new instance of OSAbstraction with the specified platform
        /// </summary>
        /// <param name="platform">Platform implementation to use</param>
        public OSAbstraction(IOSPlatform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        /// <summary>
        /// Sets up the app builder with platform-specific options
        /// </summary>
        /// <param name="builder">App builder to set up</param>
        public void SetupApp(object builder)
        {
            _platform.SetupApp(builder);
        }

        /// <summary>
        /// Sets up the data directory for the application
        /// </summary>
        public void SetupDataDir()
        {
            if (OperatingSystem.IsWindows())
            {
                var execFile = Process.GetCurrentProcess().MainModule!.FileName;
                var portableDir = Path.Combine(Path.GetDirectoryName(execFile), "data");
                if (Directory.Exists(portableDir))
                {
                    DataDir = portableDir;
                    return;
                }
            }

            var osAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(osAppDataDir))
                DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sourcegit");
            else
                DataDir = Path.Combine(osAppDataDir, "SourceGit");

            if (!Directory.Exists(DataDir))
                Directory.CreateDirectory(DataDir);
        }

        /// <summary>
        /// Sets up external tools
        /// </summary>
        public void SetupExternalTools()
        {
            ExternalTools = _platform.FindExternalTools().ToList();
        }

        /// <summary>
        /// Sets up platform-specific window settings
        /// </summary>
        /// <param name="window">Window to set up</param>
        public void SetupForWindow(object window)
        {
            _platform.SetupWindow(window);
        }

        /// <summary>
        /// Finds the Git executable on the system
        /// </summary>
        /// <returns>Path to Git executable</returns>
        public string FindGitExecutable()
        {
            // Use the IExternalTools version of FindGitExecutable to avoid ambiguity
            return ((IExternalTools)_platform).FindGitExecutable();
        }

        /// <summary>
        /// Tests if the specified shell or terminal is available
        /// </summary>
        /// <param name="shell">Shell or terminal to test</param>
        /// <returns>True if available, false otherwise</returns>
        public bool TestShellOrTerminal(ShellOrTerminal shell)
        {
            return !string.IsNullOrEmpty(_platform.FindTerminal(shell));
        }

        /// <summary>
        /// Sets the shell or terminal to use
        /// </summary>
        /// <param name="shell">Shell or terminal to use</param>
        public void SetShellOrTerminal(ShellOrTerminal shell)
        {
            if (shell == null)
                ShellOrTerminal = string.Empty;
            else
                ShellOrTerminal = _platform.FindTerminal(shell);
        }

        /// <summary>
        /// Opens the specified path in the file manager
        /// </summary>
        /// <param name="path">Path to open</param>
        /// <param name="select">Whether to select the item in the file manager</param>
        public void OpenInFileManager(string path, bool select = false)
        {
            _platform.OpenInFileManager(path, select);
        }

        /// <summary>
        /// Opens the specified URL in the browser
        /// </summary>
        /// <param name="url">URL to open</param>
        public void OpenBrowser(string url)
        {
            _platform.OpenBrowser(url);
        }

        /// <summary>
        /// Opens a terminal in the specified working directory
        /// </summary>
        /// <param name="workdir">Working directory to open the terminal in</param>
        public void OpenTerminal(string workdir)
        {
            if (string.IsNullOrEmpty(ShellOrTerminal))
                throw new InvalidOperationException("Terminal is not specified! Please confirm that the correct shell/terminal has been configured.");
            else
                _platform.OpenTerminal(workdir);
        }

        /// <summary>
        /// Opens the specified file with the default editor
        /// </summary>
        /// <param name="file">File to open</param>
        public void OpenWithDefaultEditor(string file)
        {
            _platform.OpenWithDefaultEditor(file);
        }

        /// <summary>
        /// Gets the absolute path for a file or directory
        /// </summary>
        /// <param name="root">Root directory</param>
        /// <param name="sub">Subdirectory or file</param>
        /// <returns>Absolute path</returns>
        public string GetAbsPath(string root, string sub)
        {
            var fullpath = Path.Combine(root, sub);
            if (OperatingSystem.IsWindows())
                return fullpath.Replace('/', '\\');

            return fullpath;
        }

        private void UpdateGitVersion()
        {
            if (string.IsNullOrEmpty(_gitExecutable) || !File.Exists(_gitExecutable))
            {
                GitVersionString = string.Empty;
                GitVersion = new Version(0, 0, 0);
                return;
            }

            var start = new ProcessStartInfo();
            start.FileName = _gitExecutable;
            start.Arguments = "--version";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;

            try
            {
                using var proc = Process.Start(start);
                if (proc == null) return;

                var line = proc.StandardOutput.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    GitVersionString = line;

                    var ver = line.Split(' ').Last().Split('.');
                    if (ver.Length >= 3)
                    {
                        var major = int.Parse(ver[0]);
                        var minor = int.Parse(ver[1]);
                        var build = int.Parse(ver[2]);
                        GitVersion = new Version(major, minor, build);
                    }
                }

                proc.WaitForExit();
            }
            catch
            {
                GitVersionString = string.Empty;
                GitVersion = new Version(0, 0, 0);
            }
        }
    }
}
