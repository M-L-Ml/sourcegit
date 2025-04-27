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

        public List<Models.ExternalTool> FindExternalTools()
        {
            var finder = new Models.ExternalToolsFinder();
            finder.VSCode(FindVSCode);
            finder.VSCodeInsiders(FindVSCodeInsiders);
            finder.VSCodium(FindVSCodium);
            finder.Fleet(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Programs\\Fleet\\Fleet.exe");
            finder.FindJetBrainsFromToolbox(() => $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\JetBrains\\Toolbox");
            finder.SublimeText(FindSublimeText);
            finder.TryAdd("Visual Studio", "vs", FindVisualStudio, GenerateCommandlineArgsForVisualStudio);
            return finder.Founded;
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

        #region EXTERNAL_EDITOR_FINDER
        private class RegistryEditorFinder
        {
            private readonly string _systemRegistryPath;
            private readonly string _userRegistryPath;
            private readonly Func<string, string> _pathProcessor;

            public RegistryEditorFinder(string systemRegistryPath, string userRegistryPath, Func<string, string> pathProcessor = null)
            {
                _systemRegistryPath = systemRegistryPath;
                _userRegistryPath = userRegistryPath;
                _pathProcessor = pathProcessor ?? (path => path);
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
                    return _pathProcessor(path);
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
                        return _pathProcessor(path);
                    }
                }

                return string.Empty;
            }
        }

        private static class EditorFinderFactory
        {
            public static RegistryEditorFinder CreateVSCodeFinder()
            {
                return new RegistryEditorFinder(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{EA457B21-F73E-494C-ACAB-524FDE069978}_is1",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{771FD6B0-FA20-440A-A002-3B3BAC16DC50}_is1");
            }

            public static RegistryEditorFinder CreateVSCodeInsidersFinder()
            {
                return new RegistryEditorFinder(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{1287CAD5-7C8D-410D-88B9-0D1EE4A83FF2}_is1",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{217B4C08-948D-4276-BFBB-BEE930AE5A2C}_is1");
            }

            public static RegistryEditorFinder CreateVSCodiumFinder()
            {
                return new RegistryEditorFinder(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{88DA3577-054F-4CA1-8122-7D820494CFFB}_is1",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{2E1F05D1-C245-4562-81EE-28188DB6FD17}_is1");
            }

            public static RegistryEditorFinder CreateSublimeTextFinder()
            {
                return new RegistryEditorFinder(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Sublime Text_is1",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Sublime Text 3_is1",
                    path => Path.Combine(Path.GetDirectoryName(path)!, "subl.exe"));
            }
        }

        private string FindVSCode()
        {
            return EditorFinderFactory.CreateVSCodeFinder().Find();
        }

        private string FindVSCodeInsiders()
        {
            return EditorFinderFactory.CreateVSCodeInsidersFinder().Find();
        }

        private string FindVSCodium()
        {
            return EditorFinderFactory.CreateVSCodiumFinder().Find();
        }

        private string FindSublimeText()
        {
            return EditorFinderFactory.CreateSublimeTextFinder().Find();
        }

        private string FindVisualStudio()
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
        #endregion

        private void OpenFolderAndSelectFile(string folderPath)
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

        private string GenerateCommandlineArgsForVisualStudio(string repo)
        {
            var sln = FindVSSolutionFile(new DirectoryInfo(repo), 4);
            return string.IsNullOrEmpty(sln) ? $"\"{repo}\"" : $"\"{sln}\"";
        }

        private string FindVSSolutionFile(DirectoryInfo dir, int leftDepth)
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
