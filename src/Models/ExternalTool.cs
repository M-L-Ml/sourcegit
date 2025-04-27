using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SourceGit.Native;

namespace SourceGit.Models
{
    // Pure domain model: no UI dependencies
    public class ExternalTool : ExternalToolInfo
    {
        public string ExecFile { get; private set; }

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
                Arguments = ExecArgsGenerator?.Invoke(repo) ?? $"\"{repo}\"",
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
        public string? InstallLocation { get; set; }
        [JsonPropertyName("launchCommand")]
        public string LaunchCommand { get; set; }
    }

    public class ExternalToolPaths
    {
        [JsonPropertyName("tools")]
        public Dictionary<string, string> Tools { get; set; } = new Dictionary<string, string>();
    }
    /// <summary>
    /// Encapsulates information for any external tool (editor or otherwise).
    /// </summary>
    public class ExternalToolInfo
    {
        public required string Name { get; set; }
        public string? IconName { get; set; }
        public Func<string, string>? ExecArgsGenerator { get; set; }
        // Add more as needed for broader tool support
    }

    /// <summary>
    /// Information for parameterizing editor tools (inherits from ExternalToolInfo for backward compatibility).
    /// </summary>
    public class ExternalToolInfo2 : ExternalToolInfo
    {
        public required Func<string> LocationFinder { get; set; }
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
        /// <param name="toolInfo">Information about the tool to add</param>
        /// <returns>True if the tool was added, false otherwise</returns>
        public bool TryAdd(ExternalToolInfo2 toolInfo)
        {
            string toolPath;

            // First check for custom path in settings
            if (_customPaths.Tools.TryGetValue(toolInfo.Name, out var customPath) && File.Exists(customPath))
            {
                toolPath = customPath;
            }
            else
            {
                // Then try to find the tool using the provided finder
                toolPath = toolInfo.LocationFinder();
                if (string.IsNullOrEmpty(toolPath) || !File.Exists(toolPath))
                {
                    return false;
                }
            }

            // Add the tool with the found path
            Founded_Add(new ExternalTool(toolInfo.Name, toolInfo.IconName ?? toolInfo.Name.ToLowerInvariant(), toolPath, toolInfo.ExecArgsGenerator));
            return true;
        }

        /// <summary>
        /// Tries to add an external tool to the list of founded tools.
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <param name="icon">The icon identifier for the tool</param>
        /// <param name="finder">Function that returns the path to the tool</param>
        /// <param name="execArgsGenerator">Optional function to generate command line arguments</param>
        /// <returns>True if the tool was added, false otherwise</returns>
        private bool TryAdd(string name, string icon, Func<string> finder, Func<string, string>? execArgsGenerator = null)
        {
            var toolInfo = new ExternalToolInfo2
            {
                Name = name,
                IconName = icon,
                LocationFinder = finder,
                ExecArgsGenerator = execArgsGenerator
            };
            return TryAdd(toolInfo);
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
        public bool AddEditorTool(ExternalToolInfo2 toolInfo)
        {
            // Set the IconName based on the editor name if not already set
            if (string.IsNullOrEmpty(toolInfo.IconName) && EditorIconMap.TryGetValue(toolInfo.Name, out var iconName))
            {
                toolInfo.IconName = iconName;
            }

            return TryAdd(toolInfo);
        }

        // Dictionary mapping editor names to their icon names
        private static readonly Dictionary<string, string> EditorIconMap = new()
        {
            ["Visual Studio Code"] = "vscode",
            ["Visual Studio Code - Insiders"] = "vscode_insiders",
            ["VSCodium"] = "codium",
            ["Fleet"] = "fleet",
            ["Sublime Text"] = "sublime_text",
            ["Zed"] = "zed",
            ["Visual Studio"] = "vs"
        };



        /// <summary>
        /// Adds a predefined set of common editor tools.
        /// </summary>
        public void AddDefaultEditorTools(IEnumerable<ExternalToolInfo2> tools)
        {
            foreach (var tool in tools)
            {
                AddEditorTool(tool);
            }
        }


        [Obsolete("Use the AddEditorTool method with ExternalToolInfo2 instead")]
        public static readonly ExternalToolInfo[] DefaultEditors =
                {
            new() { Name = "Visual Studio Code", IconName = "vscode" },
            new() { Name = "Visual Studio Code - Insiders", IconName = "vscode_insiders" },
            new() { Name = "VSCodium", IconName = "codium" },
            new() { Name = "Fleet", IconName = "fleet" },
            new() { Name = "Sublime Text", IconName = "sublime_text" },
            new() { Name = "Zed", IconName = "zed" },
        };


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

                    if (string.IsNullOrEmpty(tool.InstallLocation) || string.IsNullOrEmpty(tool.LaunchCommand))
                        continue;

                    Founded_Add(new ExternalTool(
                        $"{tool.DisplayName} {tool.DisplayVersion}",
                        supported_icons.Contains(tool.ProductCode) ? $"JetBrains/{tool.ProductCode}" : "JetBrains/JB",
                        Path.Combine(tool.InstallLocation, tool.LaunchCommand)));
                }
            }
        }

        public List<ExternalTool> ToList()
        {
            return Founded;
        }

        private readonly ExternalToolPaths _customPaths = null;
    }
}
