using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

using SourceGit.Models;
using Sausa;
using ExternalToolInfo2 = Sausa.ExternalToolInfo2;

namespace SourceGit.Native
{
    // Original file: src/SG_Models_Native/Windows.cs
    [SupportedOSPlatform("windows")]
    internal partial class Windows : IOSPlatform, IApplicationSetup, IFileOpener, IExternalTools, IProcessLauncher
    {
        internal struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [LibraryImport("dwmapi.dll")]
        private static partial int DwmExtendFrameIntoClientArea(nint hwnd, ref MARGINS margins);

        [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial bool PathFindOnPath(StringBuilder pszFile, string[] ppszOtherDirs);

        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial nint ILCreateFromPathW(string pszPath);

        [LibraryImport("shell32.dll")]
        private static partial void ILFree(nint pidl);

        [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int SHOpenFolderAndSelectItems(nint pidlFolder, int cild, nint apidl, int dwFlags);

        [LibraryImport("user32.dll")]
        private static partial bool GetWindowRect(nint hwnd, out RECT lpRect);

        public void SetupApp(object builder)
        {
            var appBuilder = PlatformAdapters.AsAppBuilder(builder);
            
            // Fix drop shadow issue on Windows 10
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 22000, 0))
            {
                Window.WindowStateProperty.Changed.AddClassHandler<Window>((w, _) => FixWindowFrameOnWin10(w));
                Control.LoadedEvent.AddClassHandler<Window>((w, _) => FixWindowFrameOnWin10(w));
            }
        }

        // Implementation for OS.IBackend interface  
        public void SetupApp(AppBuilder builder)
        {
            // Call our new implementation with the builder as object
            SetupApp((object)builder);
        }

        // Implementation for IOSPlatform interface
        public void SetupWindow(object window)
        {
            var avWindow = PlatformAdapters.AsWindow(window);
            SetupWindow(avWindow);
        }
        
        // Implementation for OS.IBackend interface
        // Original file: src/SG_Models_Native/Windows.cs Windows.SetupWindow
        public void SetupWindow(Window window)
        {
            window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            window.ExtendClientAreaToDecorationsHint = true;
            window.Classes.Add("fix_maximized_padding");

            Win32Properties.AddWndProcHookCallback(window, (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                // Custom WM_NCHITTEST
                if (msg == 0x0084)
                {
                    handled = true;

                    if (window.WindowState == WindowState.FullScreen || window.WindowState == WindowState.Maximized)
                        return 1; // HTCLIENT

                    var p = IntPtrToPixelPoint(lParam);
                    GetWindowRect(hWnd, out var rcWindow);

                    var borderThinkness = (int)(4 * window.RenderScaling);
                    int y = 1;
                    int x = 1;
                    if (p.X >= rcWindow.left && p.X < rcWindow.left + borderThinkness)
                        x = 0;
                    else if (p.X < rcWindow.right && p.X >= rcWindow.right - borderThinkness)
                        x = 2;

                    if (p.Y >= rcWindow.top && p.Y < rcWindow.top + borderThinkness)
                        y = 0;
                    else if (p.Y < rcWindow.bottom && p.Y >= rcWindow.bottom - borderThinkness)
                        y = 2;

                    var zone = y * 3 + x;
                    switch (zone)
                    {
                        case 0:
                            return 13; // HTTOPLEFT
                        case 1:
                            return 12; // HTTOP
                        case 2:
                            return 14; // HTTOPRIGHT
                        case 3:
                            return 10; // HTLEFT
                        case 4:
                            return 1;  // HTCLIENT
                        case 5:
                            return 11; // HTRIGHT
                        case 6:
                            return 16; // HTBOTTOMLEFT
                        case 7:
                            return 15; // HTBOTTOM
                        default:
                            return 17; // HTBOTTOMRIGHT
                    }
                }

                return IntPtr.Zero;
            });
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

        // Original file: src/SG_Models_Native/Windows.cs Windows.FindTerminal
        public string FindTerminal(ShellOrTerminal shell)
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

        public Sausa.ExternalToolsFinder FindExternalTools()
        {
            var finder = new Sausa.ExternalToolsFinder();

            // Define standard editor tools with their custom LocationFinder delegates
            ExternalToolInfo2[] editorTools =
            [
                new ExternalToolInfo2(
                    Name: "Visual Studio Code",
                    LocationFinder: FindVSCode
                ),
                new ExternalToolInfo2(
                    Name: "Visual Studio Code - Insiders",
                    LocationFinder: FindVSCodeInsiders
                ),
                new ExternalToolInfo2(
                    Name: "VSCodium",
                    LocationFinder: FindVSCodium
                ),
                new ExternalToolInfo2(
                    Name: "Fleet",
                    LocationFinder: () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Fleet", "Fleet.exe")
                ),
                new ExternalToolInfo2(
                    Name: "Sublime Text",
                    LocationFinder: FindSublimeText
                ),
                new ExternalToolInfo2(
                    Name: "Visual Studio",
                    LocationFinder: FindVisualStudio,
                    ExecArgsGenerator: GenerateCommandlineArgsForVisualStudio
                )
            ];

            // Add all editor tools in a loop
            foreach (var tool in editorTools)
                finder.AddEditorTool(tool);

            finder.FindJetBrainsFromToolbox(() =>
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JetBrains", "Toolbox"));

            return finder;
        }

        // --- Editor finder methods, now self-contained and private static ---

        private static string FindVSCode()
        {
            // Try registry uninstall keys
            var path = FindFromRegistryUninstall(
                "{EA457B21-F73E-494C-ACAB-524FDE069978}_is1",
                "{771FD6B0-FA20-440A-A002-3B3BAC16DC50}_is1");
            if (!string.IsNullOrEmpty(path)) return path;

            // Try default install locations
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var paths = new[]
            {
                Path.Combine(localApp, "Programs", "Microsoft VS Code", "Code.exe"),
                Path.Combine(programFiles, "Microsoft VS Code", "Code.exe")
            };
            foreach (var p in paths)
                if (File.Exists(p)) return p;

            // Try on PATH
            var builder = new StringBuilder("code.cmd", 259);
            if (PathFindOnPath(builder, null))
                return builder.ToString();

            return string.Empty;
        }

        private static string FindVSCodeInsiders()
        {
            var path = FindFromRegistryUninstall(
                "{1287CAD5-7C8D-410D-88B9-0D1EE4A83FF2}_is1",
                "{217B4C08-948D-4276-BFBB-BEE930AE5A2C}_is1");
            if (!string.IsNullOrEmpty(path)) return path;

            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var p = Path.Combine(localApp, "Programs", "Microsoft VS Code Insiders", "Code - Insiders.exe");
            return File.Exists(p) ? p : string.Empty;
        }

        private static string FindVSCodium()
        {
            var path = FindFromRegistryUninstall(
                "{88DA3577-054F-4CA1-8122-7D820494CFFB}_is1",
                "{2E1F05D1-C245-4562-81EE-28188DB6FD17}_is1");
            if (!string.IsNullOrEmpty(path)) return path;

            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var p = Path.Combine(localApp, "Programs", "VSCodium", "VSCodium.exe");
            return File.Exists(p) ? p : string.Empty;
        }

        private static string FindSublimeText()
        {
            var path = FindFromRegistryUninstall("Sublime Text_is1", "Sublime Text 3_is1");
            if (!string.IsNullOrEmpty(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    var subl = Path.Combine(dir, "subl.exe");
                    if (File.Exists(subl)) return subl;
                }
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var p = Path.Combine(programFiles, "Sublime Text", "sublime_text.exe");
            return File.Exists(p) ? p : string.Empty;
        }

        private static string FindFromRegistryUninstall(string systemKey, string userKey)
        {
            const string uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";
            // Try system-wide
            var localMachine = Microsoft.Win32.RegistryKey.OpenBaseKey(
                Microsoft.Win32.RegistryHive.LocalMachine,
                Microsoft.Win32.RegistryView.Registry64);
            var sys = localMachine.OpenSubKey(uninstall + systemKey);
            if (sys != null)
            {
                var path = sys.GetValue("DisplayIcon") as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }
            // Try user
            var currentUser = Microsoft.Win32.RegistryKey.OpenBaseKey(
                Microsoft.Win32.RegistryHive.CurrentUser,
                Microsoft.Win32.RegistryView.Registry64);
            var user = currentUser.OpenSubKey(uninstall + userKey);
            if (user != null)
            {
                var path = user.GetValue("DisplayIcon") as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }
            return string.Empty;
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

        private PixelPoint IntPtrToPixelPoint(IntPtr param)
        {
            var v = IntPtr.Size == 4 ? param.ToInt32() : (int)(param.ToInt64() & 0xFFFFFFFF);
            return new PixelPoint((short)(v & 0xffff), (short)(v >> 16));
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
