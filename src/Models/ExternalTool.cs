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

        public void TryAdd(string name, string icon, Func<string> finder, Func<string, string> execArgsGenerator = null)
        {
            if (_customPaths.Tools.TryGetValue(name, out var customPath) && File.Exists(customPath))
            {
                Founded.Add(new ExternalTool(name, icon, customPath, execArgsGenerator));
            }
            else
            {
                var path = finder();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    Founded.Add(new ExternalTool(name, icon, path, execArgsGenerator));
            }
        }

        /// <summary>
        /// Adds an external editor tool to the list, using the provided parameters.
        /// </summary>
        public void AddEditorTool(string name, string icon, Func<string> platformFinder, Func<string, string> execArgsGenerator = null)
        {
            TryAdd(name, icon, platformFinder, execArgsGenerator);
        }

        /// <summary>
        /// Information for parameterizing editor tools.
        /// </summary>
        public class EditorToolInfo
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public Func<string> Finder { get; set; }
            public Func<string, string> ExecArgsGenerator { get; set; }
        }

        /// <summary>
        /// Adds a predefined set of common editor tools.
        /// </summary>
        public void AddDefaultEditorTools(IEnumerable<EditorToolInfo> tools)
        {
            foreach (var tool in tools)
            {
                AddEditorTool(tool.Name, tool.Icon, tool.Finder, tool.ExecArgsGenerator);
            }
        }

        // Deprecated: Use AddEditorTool instead for new code
        [Obsolete("Use AddEditorTool instead.")]
        public void VSCode(Func<string> platformFinder)
        {
            AddEditorTool("Visual Studio Code", "vscode", platformFinder);
        }

        [Obsolete("Use AddEditorTool instead.")]
        public void VSCodeInsiders(Func<string> platformFinder)
        {
            AddEditorTool("Visual Studio Code - Insiders", "vscode_insiders", platformFinder);
        }

        [Obsolete("Use AddEditorTool instead.")]
        public void VSCodium(Func<string> platformFinder)
        {
            AddEditorTool("VSCodium", "codium", platformFinder);
        }

        [Obsolete("Use AddEditorTool instead.")]
        public void Fleet(Func<string> platformFinder)
        {
            AddEditorTool("Fleet", "fleet", platformFinder);
        }

        [Obsolete("Use AddEditorTool instead.")]
        public void SublimeText(Func<string> platformFinder)
        {
            AddEditorTool("Sublime Text", "sublime_text", platformFinder);
        }

        [Obsolete("Use AddEditorTool instead.")]
        public void Zed(Func<string> platformFinder)
        {
            AddEditorTool("Zed", "zed", platformFinder);
        }

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

                    Founded.Add(new ExternalTool(
                        $"{tool.DisplayName} {tool.DisplayVersion}",
                        supported_icons.Contains(tool.ProductCode) ? $"JetBrains/{tool.ProductCode}" : "JetBrains/JB",
                        Path.Combine(tool.InstallLocation, tool.LaunchCommand)));
                }
            }
        }

        private ExternalToolPaths _customPaths = null;
    }
}
