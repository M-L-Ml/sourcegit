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

        // OpenAI Services management
        [JsonIgnore]
        public AvaloniaList<Models.OpenAIService> OpenAIServices { get; private set; } = new AvaloniaList<Models.OpenAIService>();

        // Custom Actions management
        [JsonIgnore]
        public AvaloniaList<Models.CustomAction> CustomActions { get; private set; } = new AvaloniaList<Models.CustomAction>();

        // Workspace management
        [JsonIgnore]
        public Avalonia.Collections.AvaloniaList<Workspace> Workspaces { get; private set; } = new Avalonia.Collections.AvaloniaList<Workspace>();

        // Git install path
        [JsonIgnore]
        public string GitInstallPath
        {
            get => _gitInstallPath;
            set => SetProperty(ref _gitInstallPath, value);
        }

        // Shell or Terminal path
        [JsonIgnore]
        public string ShellOrTerminalPath
        {
            get => _shellOrTerminalPath;
            set => SetProperty(ref _shellOrTerminalPath, value);
        }

        // External merge tool path
        [JsonIgnore]
        public string ExternalMergeToolPath
        {
            get => _externalMergeToolPath;
            set => SetProperty(ref _externalMergeToolPath, value);
        }

        // Default clone directory
        [JsonIgnore]
        public string GitDefaultCloneDir
        {
            get => _gitDefaultCloneDir;
            set => SetProperty(ref _gitDefaultCloneDir, value);
        }

        // Commands
        public IAsyncRelayCommand SelectThemeOverrideFileCommand { get; }
        public IAsyncRelayCommand SelectGitExecutableCommand { get; }
        public IAsyncRelayCommand SelectDefaultCloneDirCommand { get; }
        public IAsyncRelayCommand SelectGPGExecutableCommand { get; }
        public IAsyncRelayCommand SelectShellOrTerminalCommand { get; }
        public IAsyncRelayCommand SelectExternalMergeToolCommand { get; }
        public IRelayCommand<object?> UseNativeWindowFrameChangedCommand { get; }
        public IRelayCommand AddOpenAIServiceCommand { get; }
        public IRelayCommand RemoveSelectedOpenAIServiceCommand { get; }
        public IRelayCommand AddCustomActionCommand { get; }
        public IAsyncRelayCommand<object> SelectExecutableForCustomActionCommand { get; }
        public IRelayCommand RemoveSelectedCustomActionCommand { get; }

        // === THEME & FONT PROPERTIES ===
        [JsonIgnore]
        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        [JsonIgnore]
        public string ThemeOverrides
        {
            get => _themeOverrides;
            set => SetProperty(ref _themeOverrides, value);
        }

        [JsonIgnore]
        public string DefaultFontFamily
        {
            get => _defaultFontFamily;
            set => SetProperty(ref _defaultFontFamily, value);
        }

        [JsonIgnore]
        public string MonospaceFontFamily
        {
            get => _monospaceFontFamily;
            set => SetProperty(ref _monospaceFontFamily, value);
        }

        [JsonIgnore]
        public bool OnlyUseMonoFontInEditor
        {
            get => _onlyUseMonoFontInEditor;
            set => SetProperty(ref _onlyUseMonoFontInEditor, value);
        }

        // === LOCALE PROPERTY (for App.axaml.cs compatibility) ===
        [JsonIgnore]
        public string Locale
        {
            get => _locale;
            set
            {
                if (SetProperty(ref _locale, value) && !_isLoading)
                {
                    // You may want to call App.SetLocale(value) here if needed
                }
            }
        }
        private string _locale = "en-US";

        // === LAYOUT PROPERTY ===
        [JsonIgnore]
        public LayoutInfo Layout
        {
            get => _layout;
            set => SetProperty(ref _layout, value);
        }

        // === SUBJECT GUIDE LENGTH ===
        [JsonIgnore]
        public int SubjectGuideLength
        {
            get => _subjectGuideLength;
            set => SetProperty(ref _subjectGuideLength, value);
        }

        // === UPDATE & IGNORE TAG ===
        [JsonIgnore]
        public bool ShouldCheck4UpdateOnStartup
        {
            get => _check4UpdatesOnStartup;
            set => SetProperty(ref _check4UpdatesOnStartup, value);
        }

        [JsonIgnore]
        public string IgnoreUpdateTag
        {
            get => _ignoreUpdateTag;
            set => SetProperty(ref _ignoreUpdateTag, value);
        }

        // === EXTERNAL MERGE TOOL TYPE ===
        [JsonIgnore]
        public int ExternalMergeToolType
        {
            get => _externalMergeToolType;
            set => SetProperty(ref _externalMergeToolType, value);
        }

        // === SHOW CHILDREN ===
        [JsonIgnore]
        public bool ShowChildren
        {
            get => _showChildren;
            set => SetProperty(ref _showChildren, value);
        }

        // === MAX HISTORY COMMITS ===
        [JsonIgnore]
        public int MaxHistoryCommits
        {
            get => _maxHistoryCommits;
            set => SetProperty(ref _maxHistoryCommits, value);
        }

        // === DIFF PROPERTIES ===
        [JsonIgnore]
        public bool UseFullTextDiff
        {
            get => _useFullTextDiff;
            set => SetProperty(ref _useFullTextDiff, value);
        }

        [JsonIgnore]
        public bool IgnoreWhitespaceChangesInDiff
        {
            get => _ignoreWhitespaceChangesInDiff;
            set => SetProperty(ref _ignoreWhitespaceChangesInDiff, value);
        }

        // === SYSTEM WINDOW FRAME PROPERTY ===
        [JsonIgnore]
        public bool UseSystemWindowFrame
        {
            get => _useSystemWindowFrame;
            set => SetProperty(ref _useSystemWindowFrame, value);
        }
        private bool _useSystemWindowFrame = false;

        // === REPOSITORY NODES ===
        [JsonIgnore]
        public List<RepositoryNode> RepositoryNodes { get; private set; } = new List<RepositoryNode>();

        // === WORKSPACE METHODS ===
        public Workspace? GetActiveWorkspace()
        {
            if (Workspaces != null && Workspaces.Count > 0)
                return Workspaces[0];
            return null;
        }

        // === NODE MANAGEMENT ===
        public RepositoryNode FindNode(string id)
        {
            return FindNodeRecursive(RepositoryNodes, id);
        }

        private RepositoryNode FindNodeRecursive(IEnumerable<RepositoryNode> nodes, string id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return node;
                var found = FindNodeRecursive(node.SubNodes, id);
                if (found != null) return found;
            }
            return null;
        }

        // === SonarQube: Make FindOrAddNodeByRepositoryPath static ===
        public static RepositoryNode FindOrAddNodeByRepositoryPath(List<RepositoryNode> nodes, string repositoryPath)
        {
            var node = nodes.FirstOrDefault(n => n.RepositoryPath == repositoryPath);
            if (node == null)
            {
                node = new RepositoryNode { RepositoryPath = repositoryPath };
                nodes.Add(node);
            }
            return node;
        }

        public void AddNode(RepositoryNode node, RepositoryNode parent = null)
        {
            if (parent == null)
                RepositoryNodes.Add(node);
            else
                parent.SubNodes.Add(node);
        }

        public void RemoveNode(RepositoryNode node, RepositoryNode parent = null)
        {
            if (parent == null)
                RepositoryNodes.Remove(node);
            else
                parent.SubNodes.Remove(node);
        }

        public void MoveNode(RepositoryNode from, RepositoryNode to, bool asChild)
        {
            RemoveNode(from);
            if (asChild)
                AddNode(from, to);
            else
                RepositoryNodes.Add(from);
        }

        public void SortByRenamedNode(RepositoryNode node)
        {
            // Dummy implementation (should be replaced with real logic)
        }

        public void SetCanModify()
        {
            _isReadonly = false;
        }

        // Update Git version information
        public void UpdateGitVersion()
        {
            GitVersion = Native.OS.GitVersionString;
            ShowGitVersionWarning = !string.IsNullOrEmpty(GitVersion) && Native.OS.GitVersion < Models.GitVersions.MINIMAL;
        }

        // Save changes to Git configuration
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
            SetIfChanged(config, "gpg.format", GPGFormat?.Value, "openpgp");

            if (GPGFormat != null && !GPGFormat.Value.Equals("ssh", StringComparison.Ordinal))
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

            if (GPGFormat?.Value == "openpgp" && config.TryGetValue("gpg.program", out var openpgp))
                GPGExecutableFile = openpgp;
            else if (GPGFormat != null && config.TryGetValue($"gpg.{GPGFormat.Value}.program", out var gpgProgram))
                GPGExecutableFile = gpgProgram;

            if (config.TryGetValue("http.sslverify", out var sslVerify))
                EnableHTTPSSLVerify = sslVerify == "true";
            else
                EnableHTTPSSLVerify = true;

            UpdateGitVersion();
        }

        // Helper to check if Git is configured
        public bool IsGitConfigured()
        {
            // Example logic: check if Git executable is set and exists
            var gitPath = Native.OS.GitExecutable;
            return !string.IsNullOrEmpty(gitPath) && File.Exists(gitPath);
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
            UseNativeWindowFrameChangedCommand = new RelayCommand<object?>(OnUseNativeWindowFrameChanged);
            AddOpenAIServiceCommand = new RelayCommand(AddOpenAIService);
            RemoveSelectedOpenAIServiceCommand = new RelayCommand(RemoveOpenAIService);
            AddCustomActionCommand = new RelayCommand(AddCustomAction);
            SelectExecutableForCustomActionCommand = new AsyncRelayCommand<object>(SelectExecutableForCustomActionAsync);
            RemoveSelectedCustomActionCommand = new RelayCommand(RemoveCustomAction);

            // Initialize default values
            _gpgFormat = Models.GPGFormat.Supported[0];
        }

        public async Task SelectThemeOverrideFileAsync()
        {
            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Theme Override File",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>
                    {
                        new("JSON Files") { Patterns = new[] { "*.json" } },
                        new("All Files") { Patterns = new[] { "*" } },
                    }
                });

                if (file != null && file.Count > 0)
                {
                    ThemeOverrides = file[0].Path.LocalPath;
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select theme override file");
            }
        }

        public async Task SelectGitExecutableAsync()
        {
            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Git Executable",
                    AllowMultiple = false,
                });

                if (file != null && file.Count > 0)
                {
                    GitInstallPath = file[0].Path.LocalPath;
                    UpdateGitVersion();
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select Git executable");
            }
        }

        public async Task SelectDefaultCloneDirAsync()
        {
            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Default Clone Directory",
                    AllowMultiple = false,
                });

                if (folder != null && folder.Count > 0)
                {
                    GitDefaultCloneDir = folder[0].Path.LocalPath;
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select default clone directory");
            }
        }

        public async Task SelectGPGExecutableAsync()
        {
            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select GPG Executable",
                    AllowMultiple = false,
                });

                if (file != null && file.Count > 0)
                {
                    GPGExecutableFile = file[0].Path.LocalPath;
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select GPG executable");
            }
        }

        public async Task SelectShellOrTerminalAsync()
        {
            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Shell Or Terminal",
                    AllowMultiple = false,
                });

                if (file != null && file.Count > 0)
                {
                    ShellOrTerminalPath = file[0].Path.LocalPath;
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select shell or terminal");
            }
        }

        public async Task SelectExternalMergeToolAsync()
        {
            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select External Merge Tool",
                    AllowMultiple = false,
                });

                if (file != null && file.Count > 0)
                {
                    ExternalMergeToolPath = file[0].Path.LocalPath;
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select external merge tool");
            }
        }

        public void OnUseNativeWindowFrameChanged(object? parameter)
        {
            if (!_isLoading)
            {
                UseSystemWindowFrame = !UseSystemWindowFrame;
                // TODO: Implement App.RaiseNotification or replace with appropriate notification logic
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

        public async Task SelectExecutableForCustomActionAsync(object? parameter)
        {
            if (SelectedCustomAction == null) return;

            try
            {
                // TODO: Use DI/service locator to get IStorageProvider
                // var storageProvider = App.GetService<IStorageProvider>();
                IStorageProvider storageProvider = null; // Replace with actual implementation
                if (storageProvider == null)
                    return;
                    
                var file = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Executable",
                    AllowMultiple = false,
                });

                if (file != null && file.Count > 0)
                {
                    SelectedCustomAction.Executor = file[0].Path.LocalPath;
                    Save();
                }
            }
            catch (Exception)
            {
                // TODO: Implement App.RaiseException or replace with appropriate notification logic
                // App.RaiseException(string.Empty, $"Failed to select executable");
            }
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

        #region Private Fields
        private static Preferences _instance = null;
        private bool _isLoading = true;
        private bool _isReadonly = true;
        // === REMOVE UNUSED FIELDS FOR LINT COMPLIANCE ===
        // private double _defaultFontSize = 14;
        // private double _editorFontSize = 14;
        // private int _editorTabWidth = 4;
        // private bool _useFixedTabWidth = true;
        // private bool _showAuthorTimeInGraph = false;
        // private bool _showTagsAsTree = false;
        // private bool _showTagsInGraph = false;
        // private bool _useTwoColumnsLayoutInHistories = false;
        // private bool _displayTimeAsPeriodInHistories = false;
        // private bool _useSideBySideDiff = false;
        // private bool _useSyntaxHighlighting = false;
        // private bool _enableDiffViewWordWrap = false;
        // private bool _showHiddenSymbolsInDiffView = false;
        // private bool _useBlockNavigationInDiffView = false;
        // private uint _statisticsSampleColor = 0;
        // private double _lastCheckUpdateTime = 0;
        private string _theme = "Default";
        private string _themeOverrides = string.Empty;
        private string _defaultFontFamily = string.Empty;
        private string _monospaceFontFamily = string.Empty;
        private bool _onlyUseMonoFontInEditor = false;
        private LayoutInfo _layout = new LayoutInfo();
        private int _subjectGuideLength = 72;
        private bool _check4UpdatesOnStartup = true;
        private string _ignoreUpdateTag = string.Empty;
        private int _externalMergeToolType = 0;
        private bool _showChildren = true;
        private int _maxHistoryCommits = 1000;
        private bool _useFullTextDiff = false;
        private bool _ignoreWhitespaceChangesInDiff = false;
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
        private string _gitInstallPath = string.Empty;
        private string _shellOrTerminalPath = string.Empty;
        private string _externalMergeToolPath = string.Empty;
        private string _gitDefaultCloneDir = string.Empty;
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
