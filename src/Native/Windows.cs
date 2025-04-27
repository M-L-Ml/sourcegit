using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SourceGit.Native
{
    [SupportedOSPlatform("windows")]
    internal class Windows : OS.IBackend, IOSPlatform
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", SetLastError = false)]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

        public void SetupApp(AppBuilder builder)
        {
            // Fix drop shadow issue on Windows 10
            RTL_OSVERSIONINFOEX v = new RTL_OSVERSIONINFOEX();
            v.dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOEX>();
            if (RtlGetVersion(ref v) == 0 && (v.dwMajorVersion < 10 || v.dwBuildNumber < 22000))
            {
                Window.WindowStateProperty.Changed.AddClassHandler<Window>((w, _) => FixWindowFrameOnWin10(w));
                Control.LoadedEvent.AddClassHandler<Window>((w, _) => FixWindowFrameOnWin10(w));
            }
        }

        public string FindGitExecutable()
        {
            var reg = Microsoft.Win32.RegistryKey.OpenBaseKey(
                Microsoft.Win32.RegistryHive.LocalMachine,
                Microsoft.Win32.RegistryView.Registry64);

            var git = reg.OpenSubKey("SOFTWARE\\GitForWindows");
            if (git != null && git.GetValue("InstallPath") is string installPath)
            {
                return Path.Combine(installPath, "bin", "git.exe");
            }

            var builder = new StringBuilder("git.exe", 259);
            if (!PathFindOnPath(builder, null))
            {
                return null;
            }

            var exePath = builder.ToString();
            if (!string.IsNullOrEmpty(exePath))
            {
                return exePath;
            }

            return null;
        }

        public string FindTerminal(Models.ShellOrTerminal shell)
        {
            switch (shell.Type)
            {
                case "git-bash":
                    if (string.IsNullOrEmpty(OS.GitExecutable))
                        break;

                    var binDir = Path.GetDirectoryName(OS.GitExecutable)!;
                    var bash = Path.Combine(binDir, "bash.exe");
                    if (!File.Exists(bash))
                        break;

                    return bash;
                case "pwsh":
                    var localMachine = Microsoft.Win32.RegistryKey.OpenBaseKey(
                            Microsoft.Win32.RegistryHive.LocalMachine,
                            Microsoft.Win32.RegistryView.Registry64);

                    var pwsh = localMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\pwsh.exe");
                    if (pwsh != null)
                    {
                        var path = pwsh.GetValue(null) as string;
                        if (File.Exists(path))
                            return path;
                    }

                    var pwshFinder = new StringBuilder("powershell.exe", 512);
                    if (PathFindOnPath(pwshFinder, null))
                        return pwshFinder.ToString();

                    break;
                case "cmd":
                    return "C:\\Windows\\System32\\cmd.exe";
                case "wt":
                    var wtFinder = new StringBuilder("wt.exe", 512);
                    if (PathFindOnPath(wtFinder, null))
                        return wtFinder.ToString();

                    break;
            }

            return string.Empty;
        }

        public Models.ExternalToolsFinder FindExternalTools()
        {
            var finder = new Models.ExternalToolsFinder();
            
            // Add standard editor tools using ExternalToolInfo2 objects
            finder.AddEditorTool(new Models.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Visual Studio Code", 
                LocationFinder = FindVSCode 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Visual Studio Code - Insiders", 
                LocationFinder = FindVSCodeInsiders 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "VSCodium", 
                LocationFinder = FindVSCodium 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Fleet", 
                LocationFinder = () => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Programs\\Fleet\\Fleet.exe" 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Sublime Text", 
                LocationFinder = FindSublimeText 
            });
            
            finder.AddEditorTool(new Models.ExternalToolsFinder.ExternalToolInfo2 
            { 
                Name = "Visual Studio", 
                LocationFinder = FindVisualStudio,
                ExecArgsGenerator = GenerateCommandlineArgsForVisualStudio
            });
            
            finder.FindJetBrainsFromToolbox(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\JetBrains\\Toolbox");
            
            return finder;
        }

        public void OpenBrowser(string url)
        {
            var info = new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"");
            info.CreateNoWindow = true;
            Process.Start(info);
        }

        public void OpenTerminal(string workdir)
        {
            if (!File.Exists(OS.ShellOrTerminal))
            {
                App.RaiseException(workdir, $"Terminal is not specified! Please confirm that the correct shell/terminal has been configured.");
                return;
            }

            var startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = workdir;
            startInfo.FileName = OS.ShellOrTerminal;

            // Directly launching `Windows Terminal` need to specify the `-d` parameter
            if (OS.ShellOrTerminal.EndsWith("wt.exe", StringComparison.OrdinalIgnoreCase))
                startInfo.Arguments = $"-d \"{workdir}\"";

            Process.Start(startInfo);
        }

        public void OpenInFileManager(string path, bool select)
        {
            string fullpath;
            if (File.Exists(path))
            {
                fullpath = new FileInfo(path).FullName;
                select = true;
            }
            else
            {
                fullpath = new DirectoryInfo(path!).FullName;
                fullpath += Path.DirectorySeparatorChar;
            }

            if (select)
            {
                OpenFolderAndSelectFile(fullpath);
            }
            else
            {
                Process.Start(new ProcessStartInfo(fullpath)
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                });
            }
        }

        public void OpenWithDefaultEditor(string file)
        {
            var info = new FileInfo(file);
            var start = new ProcessStartInfo("cmd", $"/c start \"\" \"{info.FullName}\"");
            start.CreateNoWindow = true;
            Process.Start(start);
        }

        private void FixWindowFrameOnWin10(Window w)
        {
            // Schedule the DWM frame extension to run in the next render frame
            // to ensure proper timing with the window initialization sequence
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var platformHandle = w.TryGetPlatformHandle();
                if (platformHandle == null)
                    return;

                var margins = new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
                DwmExtendFrameIntoClientArea(platformHandle.Handle, ref margins);
            }, DispatcherPriority.Render);
        }

        private static void OpenFolderAndSelectFile(string folderPath)
        {
            var pidl = ILCreateFromPathW(folderPath);

            try
            {
                SHOpenFolderAndSelectItems(pidl, 0, 0, 0);
            }
            finally
            {
                ILFree(pidl);
            }
        }

        /// <summary>
        /// Registry-based editor finder that encapsulates the logic for finding editors in Windows registry
        /// </summary>
        private sealed class RegistryEditorFinder
        {
            private readonly string _systemRegistryPath;
            private readonly string _userRegistryPath;
            private readonly Func<string, string>? _pathProcessor;

            public RegistryEditorFinder(string systemRegistryPath, string userRegistryPath, Func<string, string>? pathProcessor = null)
            {
                _systemRegistryPath = systemRegistryPath;
                _userRegistryPath = userRegistryPath;
                _pathProcessor = pathProcessor;
            }

            public string Find()
            {
                // Try system-wide installation first
                var localMachine = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry64);

                var systemEditor = localMachine.OpenSubKey(_systemRegistryPath);
                if (systemEditor != null)
                {
                    var path = systemEditor.GetValue("DisplayIcon") as string;
                    if (path != null)
                    {
                        return _pathProcessor != null ? _pathProcessor(path) : path;
                    }
                }

                // Then try user installation
                if (!string.IsNullOrEmpty(_userRegistryPath))
                {
                    var currentUser = Microsoft.Win32.RegistryKey.OpenBaseKey(
                        Microsoft.Win32.RegistryHive.CurrentUser,
                        Microsoft.Win32.RegistryView.Registry64);

                    var userEditor = currentUser.OpenSubKey(_userRegistryPath);
                    if (userEditor != null)
                    {
                        var path = userEditor.GetValue("DisplayIcon") as string;
                        if (path != null)
                        {
                            return _pathProcessor != null ? _pathProcessor(path) : path;
                        }
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Factory for creating editor finders with appropriate configurations
        /// </summary>
        private static class EditorFinderFactory
        {
            private const string UninstallRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";

            // Dictionary mapping editor types to their registry finder configurations
            private static readonly Dictionary<string, RegistryEditorFinder> EditorFinders = new()
            {
                ["VSCode"] = new RegistryEditorFinder(
                    $"{UninstallRegistryPath}{{EA457B21-F73E-494C-ACAB-524FDE069978}}_is1",
                    $"{UninstallRegistryPath}{{771FD6B0-FA20-440A-A002-3B3BAC16DC50}}_is1"),

                ["VSCodeInsiders"] = new RegistryEditorFinder(
                    $"{UninstallRegistryPath}{{1287CAD5-7C8D-410D-88B9-0D1EE4A83FF2}}_is1",
                    $"{UninstallRegistryPath}{{217B4C08-948D-4276-BFBB-BEE930AE5A2C}}_is1"),

                ["VSCodium"] = new RegistryEditorFinder(
                    $"{UninstallRegistryPath}{{88DA3577-054F-4CA1-8122-7D820494CFFB}}_is1",
                    $"{UninstallRegistryPath}{{2E1F05D1-C245-4562-81EE-28188DB6FD17}}_is1"),

                ["SublimeText"] = new RegistryEditorFinder(
                    $"{UninstallRegistryPath}Sublime Text_is1",
                    $"{UninstallRegistryPath}Sublime Text 3_is1",
                    path => Path.Combine(Path.GetDirectoryName(path)!, "subl.exe"))
            };

            public static RegistryEditorFinder GetFinder(string editorType)
            {
                if (EditorFinders.TryGetValue(editorType, out var finder))
                {
                    return finder;
                }

                throw new ArgumentException($"No finder configured for editor type: {editorType}", nameof(editorType));
            }
        }

        private static string FindVSCode()
        {
            return EditorFinderFactory.GetFinder("VSCode").Find();
        }

        private static string FindVSCodeInsiders()
        {
            return EditorFinderFactory.GetFinder("VSCodeInsiders").Find();
        }

        private static string FindVSCodium()
        {
            return EditorFinderFactory.GetFinder("VSCodium").Find();
        }

        private static string FindSublimeText()
        {
            return EditorFinderFactory.GetFinder("SublimeText").Find();
        }

        private static string FindVisualStudio()
        {
            var localMachine = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry64);

            // Get default class for VisualStudio.Launcher.sln - the handler for *.sln files
            if (localMachine.OpenSubKey(@"SOFTWARE\Classes\VisualStudio.Launcher.sln\CLSID") is Microsoft.Win32.RegistryKey launcher)
            {
                // Get actual path to the executable
                if (launcher.GetValue(string.Empty) is string CLSID &&
                    localMachine.OpenSubKey(@$"SOFTWARE\Classes\CLSID\{CLSID}\LocalServer32") is Microsoft.Win32.RegistryKey devenv &&
                    devenv.GetValue(string.Empty) is string localServer32)
                {
                    return localServer32!.Trim('\"');
                }
            }

            return string.Empty;
        }

        private static string GenerateCommandlineArgsForVisualStudio(string repo)
        {
            var sln = FindVSSolutionFile(new DirectoryInfo(repo), 4);
            return string.IsNullOrEmpty(sln) ? $"\"{repo}\"" : $"\"{sln}\"";
        }

        private static string FindVSSolutionFile(DirectoryInfo dir, int leftDepth)
        {
            var files = dir.GetFiles();
            foreach (var f in files)
            {
                if (f.Name.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                    return f.FullName;
            }

            if (leftDepth <= 0)
                return null;

            var subDirs = dir.GetDirectories();
            foreach (var subDir in subDirs)
            {
                var first = FindVSSolutionFile(subDir, leftDepth - 1);
                if (!string.IsNullOrEmpty(first))
                    return first;
            }

            return null;
        }

        public static string GetProgramFilesPath() => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        public static string GetLocalAppDataPath() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }
}
