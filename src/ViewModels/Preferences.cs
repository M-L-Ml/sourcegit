using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Data.Converters;
using System.Diagnostics;
using Avalonia;

namespace SourceGit.ViewModels
{
    public class Preferences : ObservableObject
    {
        [JsonIgnore]
        public static Preferences Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = Load();
                _instance._isLoading = false;

                _instance.PrepareGit();
                _instance.PrepareShellOrTerminal();
                _instance.PrepareWorkspaces();

                return _instance;
            }
        }

        public string Locale
        {
            get => _locale;
            set
            {
                if (SetProperty(ref _locale, value) && !_isLoading)
                    App.SetLocale(value);
            }
        }

        public string Theme
        {
            get => _theme;
            set
            {
                if (SetProperty(ref _theme, value) && !_isLoading)
                    App.SetTheme(_theme, _themeOverrides);
            }
        }

        public string ThemeOverrides
        {
            get => _themeOverrides;
            set
            {
                if (SetProperty(ref _themeOverrides, value) && !_isLoading)
                    App.SetTheme(_theme, value);
            }
        }

        public string DefaultFontFamily
        {
            get => _defaultFontFamily;
            set
            {
                if (SetProperty(ref _defaultFontFamily, value) && !_isLoading)
                    App.SetFonts(value, _monospaceFontFamily, _onlyUseMonoFontInEditor);
            }
        }

        public string MonospaceFontFamily
        {
            get => _monospaceFontFamily;
            set
            {
                if (SetProperty(ref _monospaceFontFamily, value) && !_isLoading)
                    App.SetFonts(_defaultFontFamily, value, _onlyUseMonoFontInEditor);
            }
        }

        public bool OnlyUseMonoFontInEditor
        {
            get => _onlyUseMonoFontInEditor;
            set
            {
                if (SetProperty(ref _onlyUseMonoFontInEditor, value) && !_isLoading)
                    App.SetFonts(_defaultFontFamily, _monospaceFontFamily, _onlyUseMonoFontInEditor);
            }
        }

        public bool UseSystemWindowFrame
        {
            get => Native.OS.UseSystemWindowFrame;
            set => Native.OS.UseSystemWindowFrame = value;
        }

        public double DefaultFontSize
        {
            get => _defaultFontSize;
            set => SetProperty(ref _defaultFontSize, value);
        }

        public double EditorFontSize
        {
            get => _editorFontSize;
            set => SetProperty(ref _editorFontSize, value);
        }

        public int EditorTabWidth
        {
            get => _editorTabWidth;
            set => SetProperty(ref _editorTabWidth, value);
        }

        public LayoutInfo Layout
        {
            get => _layout;
            set => SetProperty(ref _layout, value);
        }

        public int MaxHistoryCommits
        {
            get => _maxHistoryCommits;
            set => SetProperty(ref _maxHistoryCommits, value);
        }

        public int SubjectGuideLength
        {
            get => _subjectGuideLength;
            set => SetProperty(ref _subjectGuideLength, value);
        }

        public int DateTimeFormat
        {
            get => Models.DateTimeFormat.ActiveIndex;
            set
            {
                if (value != Models.DateTimeFormat.ActiveIndex &&
                    value >= 0 &&
                    value < Models.DateTimeFormat.Supported.Count)
                {
                    Models.DateTimeFormat.ActiveIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UseFixedTabWidth
        {
            get => _useFixedTabWidth;
            set => SetProperty(ref _useFixedTabWidth, value);
        }

        public bool Check4UpdatesOnStartup
        {
            get => _check4UpdatesOnStartup;
            set => SetProperty(ref _check4UpdatesOnStartup, value);
        }

        public bool ShowAuthorTimeInGraph
        {
            get => _showAuthorTimeInGraph;
            set => SetProperty(ref _showAuthorTimeInGraph, value);
        }

        public bool ShowChildren
        {
            get => _showChildren;
            set => SetProperty(ref _showChildren, value);
        }

        public string IgnoreUpdateTag
        {
            get => _ignoreUpdateTag;
            set => SetProperty(ref _ignoreUpdateTag, value);
        }

        public bool ShowTagsAsTree
        {
            get;
            set;
        } = false;

        public bool ShowTagsInGraph
        {
            get => _showTagsInGraph;
            set => SetProperty(ref _showTagsInGraph, value);
        }

        public bool ShowSubmodulesAsTree
        {
            get;
            set;
        } = false;

        public bool UseTwoColumnsLayoutInHistories
        {
            get => _useTwoColumnsLayoutInHistories;
            set => SetProperty(ref _useTwoColumnsLayoutInHistories, value);
        }

        public bool DisplayTimeAsPeriodInHistories
        {
            get => _displayTimeAsPeriodInHistories;
            set => SetProperty(ref _displayTimeAsPeriodInHistories, value);
        }

        public bool UseSideBySideDiff
        {
            get => _useSideBySideDiff;
            set => SetProperty(ref _useSideBySideDiff, value);
        }

        public bool UseSyntaxHighlighting
        {
            get => _useSyntaxHighlighting;
            set => SetProperty(ref _useSyntaxHighlighting, value);
        }

        public bool IgnoreCRAtEOLInDiff
        {
            get => Models.DiffOption.IgnoreCRAtEOL;
            set
            {
                if (Models.DiffOption.IgnoreCRAtEOL != value)
                {
                    Models.DiffOption.IgnoreCRAtEOL = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IgnoreWhitespaceChangesInDiff
        {
            get => _ignoreWhitespaceChangesInDiff;
            set => SetProperty(ref _ignoreWhitespaceChangesInDiff, value);
        }

        public bool EnableDiffViewWordWrap
        {
            get => _enableDiffViewWordWrap;
            set => SetProperty(ref _enableDiffViewWordWrap, value);
        }

        public bool ShowHiddenSymbolsInDiffView
        {
            get => _showHiddenSymbolsInDiffView;
            set => SetProperty(ref _showHiddenSymbolsInDiffView, value);
        }

        public bool UseFullTextDiff
        {
            get => _useFullTextDiff;
            set => SetProperty(ref _useFullTextDiff, value);
        }

        public bool UseBlockNavigationInDiffView
        {
            get => _useBlockNavigationInDiffView;
            set => SetProperty(ref _useBlockNavigationInDiffView, value);
        }

        public Models.ChangeViewMode UnstagedChangeViewMode
        {
            get => _unstagedChangeViewMode;
            set => SetProperty(ref _unstagedChangeViewMode, value);
        }

        public Models.ChangeViewMode StagedChangeViewMode
        {
            get => _stagedChangeViewMode;
            set => SetProperty(ref _stagedChangeViewMode, value);
        }

        public Models.ChangeViewMode CommitChangeViewMode
        {
            get => _commitChangeViewMode;
            set => SetProperty(ref _commitChangeViewMode, value);
        }

        public string GitInstallPath
        {
            get => Native.OS.GitExecutable;
            set
            {
                if (Native.OS.GitExecutable != value)
                {
                    OnPropertyChanging();//just in case
                    UpdateGitVersion();
                    Native.OS.GitExecutable = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GitDefaultCloneDir
        {
            get => _gitDefaultCloneDir;
            set => SetProperty(ref _gitDefaultCloneDir, value);
        }

        public int ShellOrTerminal
        {
            get => _shellOrTerminal;
            set
            {
                if (SetProperty(ref _shellOrTerminal, value))
                {
                    if (value >= 0 && value < Models.ShellOrTerminal.Supported.Count)
                        Native.OS.SetShellOrTerminal(Models.ShellOrTerminal.Supported[value]);
                    else
                        Native.OS.SetShellOrTerminal(null);

                    OnPropertyChanged(nameof(ShellOrTerminalPath));
                }
            }
        }

        public string ShellOrTerminalPath
        {
            get => Native.OS.ShellOrTerminal;
            set
            {
                if (value != Native.OS.ShellOrTerminal)
                {
                    Native.OS.ShellOrTerminal = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ExternalMergeToolType
        {
            get => _externalMergeToolType;
            set
            {
                var changed = SetProperty(ref _externalMergeToolType, value);
                if (changed && !OperatingSystem.IsWindows() && value > 0 && value < Models.ExternalMerger.Supported.Count)
                {
                    var tool = Models.ExternalMerger.Supported[value];
                    if (File.Exists(tool.Exec))
                        ExternalMergeToolPath = tool.Exec;
                    else
                        ExternalMergeToolPath = string.Empty;
                }
            }
        }

        public string ExternalMergeToolPath
        {
            get => _externalMergeToolPath;
            set => SetProperty(ref _externalMergeToolPath, value);
        }

        public uint StatisticsSampleColor
        {
            get => _statisticsSampleColor;
            set => SetProperty(ref _statisticsSampleColor, value);
        }

        public List<RepositoryNode> RepositoryNodes
        {
            get;
            set;
        } = [];

        public List<Workspace> Workspaces
        {
            get;
            set;
        } = [];

        public AvaloniaList<Models.CustomAction> CustomActions
        {
            get;
            set;
        } = [];

        public AvaloniaList<Models.OpenAIService> OpenAIServices
        {
            get;
            set;
        } = [];

        public double LastCheckUpdateTime
        {
            get => _lastCheckUpdateTime;
            set => SetProperty(ref _lastCheckUpdateTime, value);
        }

        public void SetCanModify()
        {
            _isReadonly = false;
        }

        public bool IsGitConfigured()
        {
            var path = GitInstallPath;
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }
        // Helper to check if Git is configured
        //public bool IsGitConfigured()
        //{
        //    // Example logic: check if Git executable is set and exists
        //    var gitPath = Native.OS.GitExecutable;
        //    return !string.IsNullOrEmpty(gitPath) && File.Exists(gitPath);
        //}
        public bool ShouldCheck4UpdateOnStartup()
        {
            if (!_check4UpdatesOnStartup)
                return false;

            var lastCheck = DateTime.UnixEpoch.AddSeconds(LastCheckUpdateTime).ToLocalTime();
            var now = DateTime.Now;

            if (lastCheck.Year == now.Year && lastCheck.Month == now.Month && lastCheck.Day == now.Day)
                return false;

            LastCheckUpdateTime = now.Subtract(DateTime.UnixEpoch.ToLocalTime()).TotalSeconds;
            return true;
        }

        public Workspace GetActiveWorkspace()
        {
            foreach (var w in Workspaces)
            {
                if (w.IsActive)
                    return w;
            }

            var first = Workspaces[0];
            first.IsActive = true;
            return first;
        }

        public void AddNode(RepositoryNode node, RepositoryNode to, bool save)
        {
            var collection = to == null ? RepositoryNodes : to.SubNodes;
            collection.Add(node);
            collection.Sort((l, r) =>
            {
                if (l.IsRepository != r.IsRepository)
                    return l.IsRepository ? 1 : -1;

                return string.Compare(l.Name, r.Name, StringComparison.Ordinal);
            });

            if (save)
                Save();
        }


        // Git user configuration properties (moved from View)
        [JsonIgnore]
        public string DefaultUser
        {
            get => _defaultUser;
            set => SetProperty(ref _defaultUser, value);
        }

        [JsonIgnore]
        public string DefaultEmail
        {
            get => _defaultEmail;
            set => SetProperty(ref _defaultEmail, value);
        }

        [JsonIgnore]
        public Models.CRLFMode CRLFMode
        {
            get => _crlfMode;
            set => SetProperty(ref _crlfMode, value);
        }

        [JsonIgnore]
        public bool EnablePruneOnFetch
        {
            get => _enablePruneOnFetch;
            set => SetProperty(ref _enablePruneOnFetch, value);
        }

        [JsonIgnore]
        public string GitVersion
        {
            get => _gitVersion;
            set => SetProperty(ref _gitVersion, value);
        }

        [JsonIgnore]
        public bool ShowGitVersionWarning
        {
            get => _showGitVersionWarning;
            set => SetProperty(ref _showGitVersionWarning, value);
        }

        [JsonIgnore]
        public bool EnableGPGCommitSigning
        {
            get => _enableGPGCommitSigning;
            set => SetProperty(ref _enableGPGCommitSigning, value);
        }

        [JsonIgnore]
        public bool EnableGPGTagSigning
        {
            get => _enableGPGTagSigning;
            set => SetProperty(ref _enableGPGTagSigning, value);
        }

        [JsonIgnore]
        public Models.GPGFormat GPGFormat
        {
            get => _gpgFormat;
            set
            {
                if (SetProperty(ref _gpgFormat, value))
                {
                    var config = new Commands.Config(null).ListAll();
                    if (GPGFormat.Value == "openpgp" && config.TryGetValue("gpg.program", out var openpgp))
                        GPGExecutableFile = openpgp;
                    else if (config.TryGetValue($"gpg.{GPGFormat.Value}.program", out var gpgProgram))
                        GPGExecutableFile = gpgProgram;
                }
            }
        }

        [JsonIgnore]
        public string GPGExecutableFile
        {
            get => _gpgExecutableFile;
            set => SetProperty(ref _gpgExecutableFile, value);
        }

        [JsonIgnore]
        public string GPGUserKey
        {
            get => _gpgUserKey;
            set => SetProperty(ref _gpgUserKey, value);
        }

        [JsonIgnore]
        public bool EnableHTTPSSLVerify
        {
            get => _enableHTTPSSLVerify;
            set => SetProperty(ref _enableHTTPSSLVerify, value);
        }

        [JsonIgnore]
        public Models.OpenAIService SelectedOpenAIService
        {
            get => _selectedOpenAIService;
            set => SetProperty(ref _selectedOpenAIService, value);
        }

        [JsonIgnore]
        public Models.CustomAction SelectedCustomAction
        {
            get => _selectedCustomAction;
            set => SetProperty(ref _selectedCustomAction, value);
        }

        // Commands
        public IAsyncRelayCommand SelectThemeOverrideFileCommand { get; }
        public IAsyncRelayCommand SelectGitExecutableCommand { get; }
        public IAsyncRelayCommand SelectDefaultCloneDirCommand { get; }
        public IAsyncRelayCommand SelectGPGExecutableCommand { get; }
        public IAsyncRelayCommand SelectShellOrTerminalCommand { get; }
        public IAsyncRelayCommand SelectExternalMergeToolCommand { get; }
        public IRelayCommand AddOpenAIServiceCommand { get; }
        public IRelayCommand RemoveSelectedOpenAIServiceCommand { get; }
        public IRelayCommand AddCustomActionCommand { get; }
        public IAsyncRelayCommand<object> SelectExecutableForCustomActionCommand { get; }
        public IRelayCommand RemoveSelectedCustomActionCommand { get; }

        // === NODE MANAGEMENT ===
        public RepositoryNode FindNode(string id)
        {
            return FindNodeRecursive(id, RepositoryNodes);
        }

        // === SonarQube: Make FindOrAddNodeByRepositoryPath static ===

        public RepositoryNode FindOrAddNodeByRepositoryPath(string repo, RepositoryNode parent, bool shouldMoveNode)
        {
            var normalized = repo.Replace('\\', '/');
            if (normalized.EndsWith("/"))
                normalized = normalized.TrimEnd('/');

            var node = FindNodeRecursive(normalized, RepositoryNodes);
            if (node == null)
            {
                node = new RepositoryNode()
                {
                    Id = normalized,
                    Name = Path.GetFileName(normalized),
                    Bookmark = 0,
                    IsRepository = true,
                };

                AddNode(node, parent, true);
            }
            else if (shouldMoveNode)
            {
                MoveNode(node, parent, true);
            }

            return node;
        }

        //public static RepositoryNode FindOrAddNodeByRepositoryPath(List<RepositoryNode> nodes, string repositoryPath)
        //{
        //    var node = nodes.FirstOrDefault(n => n.RepositoryPath == repositoryPath);
        //    if (node == null)
        //    {
        //        node = new RepositoryNode { RepositoryPath = repositoryPath };
        //        nodes.Add(node);
        //    }
        //    return node;
        //}



        //public void MoveNode(RepositoryNode from, RepositoryNode to, bool asChild)
        //{
        //    RemoveNode(from);
        //    if (asChild)
        //        AddNode(from, to);
        //    else
        //        RepositoryNodes.Add(from);
        //}
        public void MoveNode(RepositoryNode node, RepositoryNode to, bool save)
        {
            if (to == null && RepositoryNodes.Contains(node))
                return;
            if (to != null && to.SubNodes.Contains(node))
                return;

            RemoveNode(node, false);
            AddNode(node, to, false);

            if (save)
                Save();
        }

        public void RemoveNode(RepositoryNode node, bool save)
        {
            RemoveNodeRecursive(node, RepositoryNodes);

            if (save)
                Save();

            //RepositoryNode parent = null){
            // if (parent == null)
            //     RepositoryNodes.Remove(node);
            // else
            //     parent.SubNodes.Remove(node);
        }



        public void SortByRenamedNode(RepositoryNode node)
        {
            var container = FindNodeContainer(node, RepositoryNodes);
            container?.Sort((l, r) =>
            {
                if (l.IsRepository != r.IsRepository)
                    return l.IsRepository ? 1 : -1;

                return string.Compare(l.Name, r.Name, StringComparison.Ordinal);
            });

            Save();
        }


        // Update Git version information
        public void UpdateGitVersion()
        { // see also OS.UpdateGitVersion
            GitVersion = Native.OS.GitVersionString;
            ShowGitVersionWarning = !string.IsNullOrEmpty(GitVersion) && Native.OS.GitVersion < Models.GitVersions.MINIMAL;
        }

        //TODO: rename . This is on closing , seems resets .  Save changes to Git configuration
        public void SaveGitConfig()
        {
            if (!IsGitConfigured())
                return;

            var config = new Commands.Config(null).ListAll();
            SetIfChanged(config, "user.name", DefaultUser, "");
            SetIfChanged(config, "user.email", DefaultEmail, "");
            SetIfChanged(config, "user.signingkey", GPGUserKey, "");
            SetIfChanged(config, "core.autocrlf", CRLFMode != null ? CRLFMode.Value : null, null);
            SetIfChanged(config, "fetch.prune", EnablePruneOnFetch ? "true" : "false", "false");
            SetIfChanged(config, "commit.gpgsign", EnableGPGCommitSigning ? "true" : "false", "false");
            SetIfChanged(config, "tag.gpgsign", EnableGPGTagSigning ? "true" : "false", "false");
            SetIfChanged(config, "http.sslverify", EnableHTTPSSLVerify ? "" : "false", "");
            SetIfChanged(config, "gpg.format", GPGFormat.Value, "openpgp");

            if (!GPGFormat.Value.Equals("ssh", StringComparison.Ordinal))
            {
                var oldGPG = string.Empty;
                if (GPGFormat.Value == "openpgp" && config.TryGetValue("gpg.program", out var openpgp))
                    oldGPG = openpgp;
                else if (config.TryGetValue($"gpg.{GPGFormat.Value}.program", out var gpgProgram))
                    oldGPG = gpgProgram;

                bool changed = false;
                if (!string.IsNullOrEmpty(oldGPG))
                    changed = oldGPG != GPGExecutableFile;
                else if (!string.IsNullOrEmpty(GPGExecutableFile))
                    changed = true;

                if (changed)
                    new Commands.Config(null).Set($"gpg.{GPGFormat.Value}.program", GPGExecutableFile);
            }

            Save();
        }

        // Helper function for Git config changes
        private void SetIfChanged(Dictionary<string, string> cached, string key, string value, string defValue)
        {
            bool changed = false;
            if (cached.TryGetValue(key, out var old))
                changed = old != value;
            else if (!string.IsNullOrEmpty(value) && value != defValue)
                changed = true;

            if (changed)
                new Commands.Config(null).Set(key, value);
        }

        // Load Git configuration
        public void LoadGitConfig()
        {
            if (!IsGitConfigured())
                return;

            var config = new Commands.Config(null).ListAll();

            if (config.TryGetValue("user.name", out var name))
                DefaultUser = name;
            if (config.TryGetValue("user.email", out var email))
                DefaultEmail = email;
            if (config.TryGetValue("user.signingkey", out var signingKey))
                GPGUserKey = signingKey;
            if (config.TryGetValue("core.autocrlf", out var crlf))
                CRLFMode = Models.CRLFMode.Supported.Find(x => x.Value == crlf);
            if (config.TryGetValue("fetch.prune", out var pruneOnFetch))
                EnablePruneOnFetch = (pruneOnFetch == "true");
            if (config.TryGetValue("commit.gpgsign", out var gpgCommitSign))
                EnableGPGCommitSigning = (gpgCommitSign == "true");
            if (config.TryGetValue("tag.gpgsign", out var gpgTagSign))
                EnableGPGTagSigning = (gpgTagSign == "true");
            if (config.TryGetValue("gpg.format", out var gpgFormat))
                GPGFormat = Models.GPGFormat.Supported.Find(x => x.Value == gpgFormat) ?? Models.GPGFormat.Supported[0];

            if (GPGFormat.Value == "openpgp" && config.TryGetValue("gpg.program", out var openpgp))
                GPGExecutableFile = openpgp;
            else if (config.TryGetValue($"gpg.{GPGFormat.Value}.program", out var gpgProgram))
                GPGExecutableFile = gpgProgram;

            if (config.TryGetValue("http.sslverify", out var sslVerify))
                EnableHTTPSSLVerify = sslVerify == "true";
            else
                EnableHTTPSSLVerify = true;

            UpdateGitVersion();
        }



        // === FIX: Remove invalid method group usage for Count ===
        public int CountNodes()
        {
            return RepositoryNodes != null ? RepositoryNodes.Count : 0;
        }

        // === FIX: Remove invalid method group usage for Count ===
        public int CountWorkspaces()
        {
            return Workspaces != null ? Workspaces.Count : 0;
        }

        public Preferences()
        {
            // Initialize commands
            SelectThemeOverrideFileCommand = new AsyncRelayCommand(SelectThemeOverrideFileAsync);
            SelectGitExecutableCommand = new AsyncRelayCommand(SelectGitExecutableAsync);
            SelectDefaultCloneDirCommand = new AsyncRelayCommand(SelectDefaultCloneDirAsync);
            SelectGPGExecutableCommand = new AsyncRelayCommand(SelectGPGExecutableAsync);
            SelectShellOrTerminalCommand = new AsyncRelayCommand(SelectShellOrTerminalAsync);
            SelectExternalMergeToolCommand = new AsyncRelayCommand(SelectExternalMergeToolAsync);
            AddOpenAIServiceCommand = new RelayCommand(AddOpenAIService);
            RemoveSelectedOpenAIServiceCommand = new RelayCommand(RemoveOpenAIService);
            AddCustomActionCommand = new RelayCommand(AddCustomAction);
            SelectExecutableForCustomActionCommand = new AsyncRelayCommand<object>(SelectExecutableForCustomActionAsync);
            RemoveSelectedCustomActionCommand = new RelayCommand(RemoveCustomAction);

            // Initialize default values
            _gpgFormat = Models.GPGFormat.Supported[0];
        }

        // TODO: Use DI/service locator to get IStorageProvider
        // var storageProvider = App.GetService<IStorageProvider>();
        //IStorageProvider storageProvider = null; // Replace with actual implementation
        //if (storageProvider == null)
        //    return;
        private IStorageProvider StorageProvider => this.GetStorageProvider();
        public async Task SelectThemeOverrideFileAsync()
        {


            //var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            //{
            //    Title = "Select Theme Override File",
            //    AllowMultiple = false,
            //    FileTypeFilter = new List<FilePickerFileType>
            //    {
            //        new("JSON Files") { Patterns = new[] { "*.json" } },
            //        new("All Files") { Patterns = new[] { "*" } },
            //    }
            //});
            var options = new FilePickerOpenOptions()
            {
                Title = "Select Theme Override File",
                FileTypeFilter = [new FilePickerFileType("Theme Overrides File") { Patterns = ["*.json"] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ThemeOverrides = selected[0].Path.LocalPath;
            }


            //if (file != null && file.Count > 0)
            //{
            //    ThemeOverrides = file[0].Path.LocalPath;
            //}

            // todo: Implement App.RaiseException or replace with appropriate notification logic
            // App.RaiseException(string.Empty, $"Failed to select theme override file");

        }

        public async Task SelectGitExecutableAsync()
        {
            var pattern = OperatingSystem.IsWindows() ? "git.exe" : "git";
            var options = new FilePickerOpenOptions()
            {
                Title = "Select Git Executable",
                FileTypeFilter = [new FilePickerFileType("Git Executable") { Patterns = [pattern] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                GitInstallPath = selected[0].Path.LocalPath;
                UpdateGitVersion();
            }

            // App.RaiseException(string.Empty, $"Failed to select Git executable");
        }

        public async Task SelectDefaultCloneDirAsync()
        {
            GitDefaultCloneDir = await GetSelectDefaultCloneDirAsync(StorageProvider);

        }
        public static async Task<string?> GetSelectDefaultCloneDirAsync(IStorageProvider storageProvider)
        {
            var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Default Clone Directory",
                AllowMultiple = false,
            });

            if (folder.Count == 1)
            {
                return folder[0].Path.LocalPath;
            }
            return null;

        }

        public async Task SelectGPGExecutableAsync()
        {

            var patterns = new List<string>();
            if (OperatingSystem.IsWindows())
                patterns.Add($"{GPGFormat.Program}.exe");
            else
                patterns.Add(GPGFormat.Program);

            var options = new FilePickerOpenOptions()
            {
                Title = "Select GPG Executable",
                FileTypeFilter = [new FilePickerFileType("GPG Program") { Patterns = patterns }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                GPGExecutableFile = selected[0].Path.LocalPath;
            }



        }

        public async Task SelectShellOrTerminalAsync()
        {

            var type = ShellOrTerminal;
            if (type == -1)
                return;

            var shell = Models.ShellOrTerminal.Supported[type];
            var options = new FilePickerOpenOptions()
            {
                Title = "Select Shell Or Terminal",
                FileTypeFilter = [new FilePickerFileType(shell.Name) { Patterns = [shell.Exec] }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ShellOrTerminalPath = selected[0].Path.LocalPath;
            }


        }

        public async Task SelectExternalMergeToolAsync()
        {
            var type = ExternalMergeToolType;
            if (type < 0 || type >= Models.ExternalMerger.Supported.Count)
            {
                ExternalMergeToolType = 0;

                return;
            }

            var tool = Models.ExternalMerger.Supported[type];
            var options = new FilePickerOpenOptions()
            {
                Title = "Select External Merge Tool",
                FileTypeFilter = [new FilePickerFileType(tool.Name) { Patterns = tool.GetPatterns() }],
                AllowMultiple = false,
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                ExternalMergeToolPath = selected[0].Path.LocalPath;
            }

        }


        public void AddOpenAIService()
        {
            var service = new Models.OpenAIService();
            OpenAIServices.Add(service);
            SelectedOpenAIService = service;
            Save();
        }

        public void RemoveOpenAIService()
        {
            if (SelectedOpenAIService != null)
            {
                OpenAIServices.Remove(SelectedOpenAIService);
                SelectedOpenAIService = OpenAIServices.Count > 0 ? OpenAIServices[0] : null;
                Save();
            }
        }

        public void AddCustomAction()
        {
            var action = new Models.CustomAction();
            CustomActions.Add(action);
            SelectedCustomAction = action;
            Save();
        }

        public void RemoveCustomAction()
        {
            if (SelectedCustomAction != null)
            {
                CustomActions.Remove(SelectedCustomAction);
                SelectedCustomAction = CustomActions.Count > 0 ? CustomActions[0] : null;
                Save();
            }
        }

        //TODO: localization of Titles here and elsewhere, using existing resource files 
        public async Task SelectExecutableForCustomActionAsync(object? parameter)
        {
            var options = new FilePickerOpenOptions()
            {
                Title = "Select Executable For Custom Action",
                FileTypeFilter = [new FilePickerFileType("Executable file(script)") { Patterns = ["*.*"] }],
                AllowMultiple = false, 
            };

            var selected = await StorageProvider.OpenFilePickerAsync(options);
            if (selected.Count == 1)
            {
                // TODO: refactor : Better ways to find this?
                if (parameter is IDataContextProvider { DataContext: Models.CustomAction action })
                    action.Executable = selected[0].Path.LocalPath;
                else
                    Debug.Assert(false, "sender is not a Button with DataContext of Models.CustomAction");
            }
        }
        public void AutoRemoveInvalidNode()
        {
            var changed = RemoveInvalidRepositoriesRecursive(RepositoryNodes);
            if (changed)
                Save();
        }
        
        public void Save()
        {
            if (_isLoading || _isReadonly)
                return;

            var file = Path.Combine(Native.OS.DataDir, "preference.json");
            var data = JsonSerializer.Serialize(this, JsonCodeGen.Default.Preferences);
            File.WriteAllText(file, data);
        }

        private static Preferences Load()
        {
            var path = Path.Combine(Native.OS.DataDir, "preference.json");
            if (!File.Exists(path))
                return new Preferences();

            try
            {
                return JsonSerializer.Deserialize(File.ReadAllText(path), JsonCodeGen.Default.Preferences);
            }
            catch
            {
                return new Preferences();
            }
        }

        private void PrepareGit()
        {
            var path = Native.OS.GitExecutable;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                GitInstallPath = Native.OS.FindGitExecutable();
        }

        private void PrepareShellOrTerminal()
        {
            if (_shellOrTerminal >= 0)
                return;

            for (int i = 0; i < Models.ShellOrTerminal.Supported.Count; i++)
            {
                var shell = Models.ShellOrTerminal.Supported[i];
                if (Native.OS.TestShellOrTerminal(shell))
                {
                    ShellOrTerminal = i;
                    break;
                }
            }
        }

        private void PrepareWorkspaces()
        {
            if (Workspaces.Count == 0)
            {
                Workspaces.Add(new Workspace() { Name = "Default" });
                return;
            }

            foreach (var workspace in Workspaces)
            {
                if (!workspace.RestoreOnStartup)
                {
                    workspace.Repositories.Clear();
                    workspace.ActiveIdx = 0;
                }
            }
        }

        private RepositoryNode FindNodeRecursive(string id, List<RepositoryNode> collection)
        {
            foreach (var node in collection)
            {
                if (node.Id == id)
                    return node;

                var sub = FindNodeRecursive(id, node.SubNodes);
                if (sub != null)
                    return sub;
            }

            return null;
        }


        [Obsolete("check this AI implemention")]
        private RepositoryNode FindNodeRecursive2(IEnumerable<RepositoryNode> nodes, string id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                    return node;
                var found = FindNodeRecursive2(node.SubNodes, id);
                if (found != null)
                    return found;
            }
            return null;
        }

        private List<RepositoryNode> FindNodeContainer(RepositoryNode node, List<RepositoryNode> collection)
        {
            foreach (var sub in collection)
            {
                if (node == sub)
                    return collection;

                var subCollection = FindNodeContainer(node, sub.SubNodes);
                if (subCollection != null)
                    return subCollection;
            }

            return null;
        }

        private bool RemoveNodeRecursive(RepositoryNode node, List<RepositoryNode> collection)
        {
            if (collection.Contains(node))
            {
                collection.Remove(node);
                return true;
            }

            foreach (var one in collection)
            {
                if (RemoveNodeRecursive(node, one.SubNodes))
                    return true;
            }

            return false;
        }

        private bool RemoveInvalidRepositoriesRecursive(List<RepositoryNode> collection)
        {
            bool changed = false;

            for (int i = collection.Count - 1; i >= 0; i--)
            {
                var node = collection[i];
                if (node.IsInvalid)
                {
                    collection.RemoveAt(i);
                    changed = true;
                }
                else if (!node.IsRepository)
                {
                    changed |= RemoveInvalidRepositoriesRecursive(node.SubNodes);
                }
            }

            return changed;
        }

        #region Private Fields
        private static Preferences _instance = null;
        private bool _isLoading = true;
        private bool _isReadonly = true;
        private string _locale = "en_US";
        private string _theme = "Default";
        private string _themeOverrides = string.Empty;
        private string _defaultFontFamily = string.Empty;
        private string _monospaceFontFamily = string.Empty;
        private bool _onlyUseMonoFontInEditor = false;
        private double _defaultFontSize = 13;
        private double _editorFontSize = 13;
        private int _editorTabWidth = 4;
        private LayoutInfo _layout = new LayoutInfo();

        private int _maxHistoryCommits = 20000;
        private int _subjectGuideLength = 50;
        private bool _useFixedTabWidth = true;
        private bool _showAuthorTimeInGraph = false;
        private bool _showChildren = false;

        private bool _check4UpdatesOnStartup = true;
        private double _lastCheckUpdateTime = 0;
        private string _ignoreUpdateTag = string.Empty;

        private bool _showTagsInGraph = true;
        private bool _useTwoColumnsLayoutInHistories = false;
        private bool _displayTimeAsPeriodInHistories = false;
        private bool _useSideBySideDiff = false;
        private bool _ignoreWhitespaceChangesInDiff = false;
        private bool _useSyntaxHighlighting = false;
        private bool _enableDiffViewWordWrap = false;
        private bool _showHiddenSymbolsInDiffView = false;
        private bool _useFullTextDiff = false;
        private bool _useBlockNavigationInDiffView = false;

        private Models.ChangeViewMode _unstagedChangeViewMode = Models.ChangeViewMode.List;
        private Models.ChangeViewMode _stagedChangeViewMode = Models.ChangeViewMode.List;
        private Models.ChangeViewMode _commitChangeViewMode = Models.ChangeViewMode.List;

        private string _gitDefaultCloneDir = string.Empty;

        private int _shellOrTerminal = -1;
        private int _externalMergeToolType = 0;
        private string _externalMergeToolPath = string.Empty;

        private uint _statisticsSampleColor = 0xFF00FF00;

        private string _defaultUser = string.Empty;
        private string _defaultEmail = string.Empty;
        private Models.CRLFMode _crlfMode = null;
        private bool _enablePruneOnFetch = false;
        private string _gitVersion = string.Empty;
        private bool _showGitVersionWarning = false;
        private bool _enableGPGCommitSigning = false;
        private bool _enableGPGTagSigning = false;
        private Models.GPGFormat _gpgFormat = null;
        private string _gpgExecutableFile = string.Empty;
        private string _gpgUserKey = string.Empty;
        private bool _enableHTTPSSLVerify = true;
        private Models.OpenAIService _selectedOpenAIService = null;
        private Models.CustomAction _selectedCustomAction = null;
        #endregion
    }

    public static class VMIntConverters
    {
        public static readonly FuncValueConverter<int, bool> IsSubjectLengthBad =
    new FuncValueConverter<int, bool>(v => v > ViewModels.Preferences.Instance.SubjectGuideLength);

        public static readonly FuncValueConverter<int, bool> IsSubjectLengthGood =
            new FuncValueConverter<int, bool>(v => v <= ViewModels.Preferences.Instance.SubjectGuideLength);
    }
}
