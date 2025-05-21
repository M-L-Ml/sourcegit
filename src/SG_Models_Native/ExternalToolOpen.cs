using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sausa;

namespace SourceGit.Models
{
    // Pure domain model: no UI dependencies
    public class ExternalToolOpen
    {

        public ExternalTool Info { get; private set; }

        public ExternalToolOpen(ExternalTool info//, string execFile//, Func<string, string>? execArgsGenerator = null
            )
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
            //    ExecFile = execFile;
            //  ExecArgsGenerator = info.ExecArgsGenerator //execArgsGenerator ??
            //   (repo => $"\"{repo}\"");
        }

        public void Open(string repo)
        {
            Process.Start(new ProcessStartInfo()
            {
                WorkingDirectory = repo,
                FileName = Info.Location,
                Arguments = Info.ExecArgsGenerator?.Invoke(repo) ?? $"\"{repo}\"",
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



    public class ExternalToolsFinder2 : Sausa.ExternalToolsFinder
    {
        // private readonly SourceGit.Native.IOSPlatform _os;

        public IReadOnlyList<ExternalTool> Founded
          => base._tools;
        //{
        //    get;
        //    private set;
        //} = new List<ExternalTool>();

        public ExternalToolsFinder2()
        {
            var customPathsConfig = Path.Combine(Native.OS.DataDir, "external_editors.json");
            try
            {
                if (File.Exists(customPathsConfig))
                    _customPaths = JsonSerializer.Deserialize(File.ReadAllText(customPathsConfig), ModelsN.JsonCodeGen.Default.ExternalToolPaths);
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
        public bool TryAdd(ExternalToolInfo2 toolInfo, string? type = "external tool")
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

            }

            return TryAdd(toolInfo,
                 ///.Name, toolInfo.IconName ?? 
                 ///toolInfo.Name.ToLowerInvariant(), 
                 toolPath, type: type);//:

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
            (
                Name: name,
                IconName: icon,
                LocationFinder: finder,
                ExecArgsGenerator: execArgsGenerator
            );
            return TryAdd(toolInfo);
        }




        /// <summary>
        /// Adds an external editor tool to the list, using the provided parameters.
        /// </summary>
        /// <param name="toolInfo">Information about the editor tool to add</param>
        /// <returns>True if the tool was added, false otherwise</returns>
        public bool AddEditorTool(ExternalToolInfo2 toolInfo)
        {
            // ExternalToolInfo2 toolInfo2;
            // Set the IconName based on the editor name if not already set
            if (string.IsNullOrEmpty(toolInfo.IconName) && EditorIconMap.TryGetValue(toolInfo.Name, out var iconName))
            {
                TryAdd(toolInfo with { IconName = iconName });
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




        public override void FindJetBrainsFromToolbox(Func<string> platformFinder) 
        {
            var exclude = new List<string> { "fleet", "dotmemory", "dottrace", "resharper-u", "androidstudio" };
            var supported_icons = new List<string> { "CL", "DB", "DL", "DS", "GO", "JB", "PC", "PS", "PY", "QA", "QD", "RD", "RM", "RR", "WRS", "WS" };
            var state = Path.Combine(platformFinder(), "state.json");
            if (File.Exists(state))
            {
                var stateData = JsonSerializer.Deserialize(File.ReadAllText(state), ModelsN.JsonCodeGen.Default.JetBrainsState);
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

        public IReadOnlyList<ExternalTool> ToList()
        {
            return Founded;
        }

        private readonly ExternalToolPaths _customPaths = null;
    }
}
