using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SourceGit.Native;

namespace SourceGit.Models
{
    // Pure domain model: no UI dependencies
    public class ExternalTool
    {
        public string Name { get; private set; }
        public string IconName { get; private set; } // Store icon name as string, not Bitmap
        public string ExecFile { get; private set; }
        public Func<string, string> ExecArgsGenerator { get; private set; }

        public ExternalTool(string name, string iconName, string execFile, Func<string, string> execArgsGenerator = null)
        {
            Name = name;
            IconName = iconName;
            ExecFile = execFile;
            ExecArgsGenerator = execArgsGenerator ?? (repo => $"\"{repo}\"");
        }

        public void Open(string repo)
        {
            Process.Start(new ProcessStartInfo()
            {
                WorkingDirectory = repo,
                FileName = ExecFile,
                Arguments = ExecArgsGenerator.Invoke(repo),
                UseShellExecute = false,
            });
        }
    }

    public class JetBrainsState
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;
        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = string.Empty;
        [JsonPropertyName("tools")]
        public List<JetBrainsTool> Tools { get; set; } = new List<JetBrainsTool>();
    }

    public class JetBrainsTool
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }
        [JsonPropertyName("toolId")]
        public string ToolId { get; set; }
        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; }
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("displayVersion")]
        public string DisplayVersion { get; set; }
        [JsonPropertyName("buildNumber")]
        public string BuildNumber { get; set; }
        [JsonPropertyName("installLocation")]
        public string InstallLocation { get; set; }
        [JsonPropertyName("launchCommand")]
        public string LaunchCommand { get; set; }
    }

    public class ExternalToolPaths
    {
        [JsonPropertyName("tools")]
        public Dictionary<string, string> Tools { get; set; } = new Dictionary<string, string>();
    }

    public class ExternalToolsFinder
    {
       // private readonly SourceGit.Native.IOSPlatform _os;

        public List<ExternalTool> Founded
        {
            get;
            private set;
        } = new List<ExternalTool>();

        public ExternalToolsFinder()
        {
            var customPathsConfig = Path.Combine(Native.OS.DataDir, "external_editors.json");
            try
            {
                if (File.Exists(customPathsConfig))
                    _customPaths = JsonSerializer.Deserialize(File.ReadAllText(customPathsConfig), JsonCodeGen.Default.ExternalToolPaths);
            }
            catch
            {
                // Ignore
            }
            if (_customPaths == null)
                _customPaths = new ExternalToolPaths();
        }

        /// <summary>
        /// Tries to add an external tool to the list of founded tools.
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <param name="icon">The icon identifier for the tool</param>
        /// <param name="finder">Function that returns the path to the tool</param>
        /// <param name="execArgsGenerator">Optional function to generate command line arguments</param>
        /// <returns>True if the tool was added, false otherwise</returns>
        public bool TryAdd(string name, string icon, Func<string> finder, Func<string, string>? execArgsGenerator = null)
        {
            string toolPath;
            
            // First check for custom path in settings
            if (_customPaths.Tools.TryGetValue(name, out var customPath) && File.Exists(customPath))
            {
                toolPath = customPath;
            }
            else
            {
                // Then try to find the tool using the provided finder
                toolPath = finder();
                if (string.IsNullOrEmpty(toolPath) || !File.Exists(toolPath))
                {
                    return false;
                }
            }
            
            // Add the tool with the found path
            Founded_Add(new ExternalTool(name, icon, toolPath, execArgsGenerator));
            return true;
        }

        private void Founded_Add(ExternalTool externalTool)
        {
           Founded.Add(externalTool);
        }


        /// <summary>
        /// Adds an external editor tool to the list, using the provided parameters.
        /// </summary>
        /// <param name="toolInfo">Information about the editor tool to add</param>
        /// <returns>True if the tool was added, false otherwise</returns>
        public bool AddEditorTool(EditorToolInfo toolInfo)
        {
            return TryAdd(toolInfo.Name, toolInfo.Icon, toolInfo.LocationFinder, toolInfo.ExecArgsGenerator);
        }

        /// <summary>
        /// Encapsulates information for any external tool (editor or otherwise).
        /// </summary>
        public class ExternalToolInfo
        {
            public required string Name { get; set; }
            public required string Icon { get; set; }
            public Func<string, string>? ExecArgsGenerator { get; set; }
            // Add more as needed for broader tool support
            public required Func<string> LocationFinder { get; set; }
        }

        /// <summary>
        /// Information for parameterizing editor tools (inherits from ExternalToolInfo for backward compatibility).
        /// </summary>
        public class EditorToolInfo : ExternalToolInfo
        {
        }

        /// <summary>
        /// Adds a predefined set of common editor tools.
        /// </summary>
        public void AddDefaultEditorTools(IEnumerable<EditorToolInfo> tools)
        {
            foreach (var tool in tools)
            {
                AddEditorTool(tool);
            }
        }

        public static readonly EditorToolInfo[] DefaultEditors = new EditorToolInfo[]
        {
            new() { Name = "Visual Studio Code", Icon = "vscode", LocationFinder = () => VSCodeFinder() },
            new() { Name = "Visual Studio Code - Insiders", Icon = "vscode_insiders", LocationFinder = () => VSCodeInsidersFinder() },
            new() { Name = "VSCodium", Icon = "codium", LocationFinder = () => VSCodiumFinder() },
            new() { Name = "Fleet", Icon = "fleet", LocationFinder = () => FleetFinder() },
            new() { Name = "Sublime Text", Icon = "sublime_text", LocationFinder = () => SublimeTextFinder() },
            new() { Name = "Zed", Icon = "zed", LocationFinder = () => ZedFinder() },
        };

        // These static finder methods must be implemented elsewhere in the codebase or here as stubs.
        // For illustration, here are stubs (replace with actual logic as needed):
        private static string VSCodeFinder() => "";
        private static string VSCodeInsidersFinder() => "";
        private static string VSCodiumFinder() => "";
        private static string FleetFinder() => "";
        private static string SublimeTextFinder() => "";
        private static string ZedFinder() => "";

        public void FindJetBrainsFromToolbox(Func<string> platformFinder)
        {
            var exclude = new List<string> { "fleet", "dotmemory", "dottrace", "resharper-u", "androidstudio" };
            var supported_icons = new List<string> { "CL", "DB", "DL", "DS", "GO", "JB", "PC", "PS", "PY", "QA", "QD", "RD", "RM", "RR", "WRS", "WS" };
            var state = Path.Combine(platformFinder(), "state.json");
            if (File.Exists(state))
            {
                var stateData = JsonSerializer.Deserialize(File.ReadAllText(state), JsonCodeGen.Default.JetBrainsState);
                foreach (var tool in stateData.Tools)
                {
                    if (exclude.Contains(tool.ToolId.ToLowerInvariant()))
                        continue;

                    Founded_Add(new ExternalTool(
                        $"{tool.DisplayName} {tool.DisplayVersion}",
                        supported_icons.Contains(tool.ProductCode) ? $"JetBrains/{tool.ProductCode}" : "JetBrains/JB",
                        Path.Combine(tool.InstallLocation, tool.LaunchCommand)));
                }
            }
        }

        private ExternalToolPaths _customPaths = null;
    }
}
