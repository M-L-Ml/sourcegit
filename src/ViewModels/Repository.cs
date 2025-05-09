using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.ViewModels
{
    public class Repository : ObservableObject, Models.IRepository
    {
        public bool IsBare
        {
            get;
        }

        public string FullPath
        {
            get => _fullpath;
            set
            {
                if (value != null)
                {
                    var normalized = value.Replace('\\', '/');
                    SetProperty(ref _fullpath, normalized);
                }
                else
                {
                    SetProperty(ref _fullpath, null);
                }
            }
        }

        public string GitDir
        {
            get => _gitDir;
            set => SetProperty(ref _gitDir, value);
        }

        public Models.RepositorySettings Settings
        {
            get => _settings;
        }

        public Models.FilterMode HistoriesFilterMode
        {
            get => _historiesFilterMode;
            private set => SetProperty(ref _historiesFilterMode, value);
        }

        public bool HasAllowedSignersFile
        {
            get => _hasAllowedSignersFile;
        }

        public int SelectedViewIndex
        {
            get => _selectedViewIndex;
            set
            {
                if (SetProperty(ref _selectedViewIndex, value))
                {
                    switch (value)
                    {
                        case 1:
                            SelectedView = _workingCopy;
                            break;
                        case 2:
                            SelectedView = _stashesPage;
                            break;
                        default:
                            SelectedView = _histories;
                            break;
                    }
                }
            }
        }

        public object SelectedView
        {
            get => _selectedView;
            set => SetProperty(ref _selectedView, value);
        }

        public bool EnableReflog
        {
            get => _settings.EnableReflog;
            set
            {
                if (value != _settings.EnableReflog)
                {
                    _settings.EnableReflog = value;
                    OnPropertyChanged();
                    Task.Run(RefreshCommits);
                }
            }
        }

        public bool EnableFirstParentInHistories
        {
            get => _settings.EnableFirstParentInHistories;
            set
            {
                if (value != _settings.EnableFirstParentInHistories)
                {
                    _settings.EnableFirstParentInHistories = value;
                    OnPropertyChanged();
                    Task.Run(RefreshCommits);
                }
            }
        }

        public bool OnlyHighlightCurrentBranchInHistories
        {
            get => _settings.OnlyHighlighCurrentBranchInHistories;
            set
            {
                if (value != _settings.OnlyHighlighCurrentBranchInHistories)
                {
                    _settings.OnlyHighlighCurrentBranchInHistories = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                {
                    var builder = BuildBranchTree(_branches, _remotes);
                    LocalBranchTrees = builder.Locals;
                    RemoteBranchTrees = builder.Remotes;
                    VisibleTags = BuildVisibleTags();
                    VisibleSubmodules = BuildVisibleSubmodules();
                }
            }
        }

        public List<Models.Remote> Remotes
        {
            get => _remotes;
            private set => SetProperty(ref _remotes, value);
        }

        public List<Models.Branch> Branches
        {
            get => _branches;
            private set => SetProperty(ref _branches, value);
        }

        public Models.Branch CurrentBranch
        {
            get => _currentBranch;
            private set
            {
                var oldHead = _currentBranch?.Head;
                if (SetProperty(ref _currentBranch, value))
                {
                    if (oldHead != _currentBranch.Head && _workingCopy is { UseAmend: true })
                        _workingCopy.UseAmend = false;
                }
            }
        }

        public List<BranchTreeNode> LocalBranchTrees
        {
            get => _localBranchTrees;
            private set => SetProperty(ref _localBranchTrees, value);
        }

        public List<BranchTreeNode> RemoteBranchTrees
        {
            get => _remoteBranchTrees;
            private set => SetProperty(ref _remoteBranchTrees, value);
        }

        public List<Models.Worktree> Worktrees
        {
            get => _worktrees;
            private set => SetProperty(ref _worktrees, value);
        }

        public List<Models.Tag> Tags
        {
            get => _tags;
            private set => SetProperty(ref _tags, value);
        }

        public List<Models.Tag> VisibleTags
        {
            get => _visibleTags;
            private set => SetProperty(ref _visibleTags, value);
        }

        public List<Models.Submodule> Submodules
        {
            get => _submodules;
            private set => SetProperty(ref _submodules, value);
        }

        public List<Models.Submodule> VisibleSubmodules
        {
            get => _visibleSubmodules;
            private set => SetProperty(ref _visibleSubmodules, value);
        }

        public int LocalChangesCount
        {
            get => _localChangesCount;
            private set => SetProperty(ref _localChangesCount, value);
        }

        public int StashesCount
        {
            get => _stashesCount;
            private set => SetProperty(ref _stashesCount, value);
        }

        public bool IncludeUntracked
        {
            get => _settings.IncludeUntrackedInLocalChanges;
            set
            {
                if (value != _settings.IncludeUntrackedInLocalChanges)
                {
                    _settings.IncludeUntrackedInLocalChanges = value;
                    OnPropertyChanged();
                    Task.Run(RefreshWorkingCopyChanges);
                }
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (SetProperty(ref _isSearching, value))
                {
                    if (value)
                    {
                        SelectedViewIndex = 0;
                        CalcWorktreeFilesForSearching();
                    }
                    else
                    {
                        SearchedCommits = new List<Models.Commit>();
                        SelectedSearchedCommit = null;
                        SearchCommitFilter = string.Empty;
                        MatchedFilesForSearching = null;
                        _worktreeFiles = null;
                    }
                }
            }
        }

        public bool IsSearchLoadingVisible
        {
            get => _isSearchLoadingVisible;
            private set => SetProperty(ref _isSearchLoadingVisible, value);
        }

        public bool OnlySearchCommitsInCurrentBranch
        {
            get => _onlySearchCommitsInCurrentBranch;
            set
            {
                if (SetProperty(ref _onlySearchCommitsInCurrentBranch, value) && !string.IsNullOrEmpty(_searchCommitFilter))
                    StartSearchCommits();
            }
        }

        public int SearchCommitFilterType
        {
            get => _searchCommitFilterType;
            set
            {
                if (SetProperty(ref _searchCommitFilterType, value))
                {
                    CalcWorktreeFilesForSearching();
                    if (!string.IsNullOrEmpty(_searchCommitFilter))
                        StartSearchCommits();
                }
            }
        }

        public string SearchCommitFilter
        {
            get => _searchCommitFilter;
            set
            {
                if (SetProperty(ref _searchCommitFilter, value) && IsSearchingCommitsByFilePath())
                    CalcMatchedFilesForSearching();
            }
        }

        public List<string> MatchedFilesForSearching
        {
            get => _matchedFilesForSearching;
            private set => SetProperty(ref _matchedFilesForSearching, value);
        }

        public List<Models.Commit> SearchedCommits
        {
            get => _searchedCommits;
            set => SetProperty(ref _searchedCommits, value);
        }

        public Models.Commit SelectedSearchedCommit
        {
            get => _selectedSearchedCommit;
            set
            {
                if (SetProperty(ref _selectedSearchedCommit, value) && value != null)
                    NavigateToCommit(value.SHA);
            }
        }

        public bool IsLocalBranchGroupExpanded
        {
            get => _settings.IsLocalBranchesExpandedInSideBar;
            set
            {
                if (value != _settings.IsLocalBranchesExpandedInSideBar)
                {
                    _settings.IsLocalBranchesExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRemoteGroupExpanded
        {
            get => _settings.IsRemotesExpandedInSideBar;
            set
            {
                if (value != _settings.IsRemotesExpandedInSideBar)
                {
                    _settings.IsRemotesExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTagGroupExpanded
        {
            get => _settings.IsTagsExpandedInSideBar;
            set
            {
                if (value != _settings.IsTagsExpandedInSideBar)
                {
                    _settings.IsTagsExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSubmoduleGroupExpanded
        {
            get => _settings.IsSubmodulesExpandedInSideBar;
            set
            {
                if (value != _settings.IsSubmodulesExpandedInSideBar)
                {
                    _settings.IsSubmodulesExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsWorktreeGroupExpanded
        {
            get => _settings.IsWorktreeExpandedInSideBar;
            set
            {
                if (value != _settings.IsWorktreeExpandedInSideBar)
                {
                    _settings.IsWorktreeExpandedInSideBar = value;
                    OnPropertyChanged();
                }
            }
        }

        public InProgressContext InProgressContext
        {
            get => _workingCopy?.InProgressContext;
        }

        public Models.BisectState BisectState
        {
            get => _bisectState;
            private set => SetProperty(ref _bisectState, value);
        }

        public bool IsBisectCommandRunning
        {
            get => _isBisectCommandRunning;
            private set => SetProperty(ref _isBisectCommandRunning, value);
        }

        public bool IsAutoFetching
        {
            get => _isAutoFetching;
            private set => SetProperty(ref _isAutoFetching, value);
        }

        public int CommitDetailActivePageIndex
        {
            get;
            set;
        } = 0;

        public AvaloniaList<CommandLog> Logs
        {
            get;
            private set;
        } = new AvaloniaList<CommandLog>();

        public Repository(bool isBare, string path, string gitDir)
        {
            IsBare = isBare;
            FullPath = path;
            GitDir = gitDir;
            InitializeGitFlowCommands();
        }

        public Repository()
        {
            InitializeGitFlowCommands();
        }

           private void InitializeGitFlowCommands()
        {
            StartFeatureCommand = new RelayCommand(() => GitFlowStartFeature());
            StartReleaseCommand = new RelayCommand(() => GitFlowStartRelease());
            StartHotfixCommand = new RelayCommand(() => GitFlowStartHotfix());
            InitGitFlowCommand = new RelayCommand(() => GitFlowInit());
        }

        public void Open()
        {
            var settingsFile = Path.Combine(_gitDir, "sourcegit.settings");
            if (File.Exists(settingsFile))
            {
                try
                {
                    _settings = JsonSerializer.Deserialize(File.ReadAllText(settingsFile), Models.JsonCodeGen.Default.RepositorySettings);
                }
                catch
                {
                    _settings = new Models.RepositorySettings();
                }
            }
            else
            {
                _settings = new Models.RepositorySettings();
            }

            try
            {
                // For worktrees, we need to watch the $GIT_COMMON_DIR instead of the $GIT_DIR.
                var gitDirForWatcher = _gitDir;
                if (_gitDir.Replace("\\", "/").IndexOf("/worktrees/", StringComparison.Ordinal) > 0)
                {
                    var commonDir = new Commands.QueryGitCommonDir(_fullpath).Result();
                    if (!string.IsNullOrEmpty(commonDir))
                        gitDirForWatcher = commonDir;
                }

                _watcher = new Models.Watcher(this, _fullpath, gitDirForWatcher);
            }
            catch (Exception ex)
            {
                App.RaiseException(string.Empty, $"Failed to start watcher for repository: '{_fullpath}'. You may need to press 'F5' to refresh repository manually!\n\nReason: {ex.Message}");
            }

            if (_settings.HistoriesFilters.Count > 0)
                _historiesFilterMode = _settings.HistoriesFilters[0].Mode;
            else
                _historiesFilterMode = Models.FilterMode.None;

            _histories = new Histories(this);
            _workingCopy = new WorkingCopy(this);
            _stashesPage = new StashesPage(this);
            _selectedView = _histories;
            _selectedViewIndex = 0;

            _workingCopy.CommitMessage = _settings.LastCommitMessage;
            _autoFetchTimer = new Timer(AutoFetchImpl, null, 5000, 5000);
            RefreshAll();
        }

        public void Close()
        {
            SelectedView = null; // Do NOT modify. Used to remove exists widgets for GC.Collect
            Logs.Clear();

            _settings.LastCommitMessage = _workingCopy.CommitMessage;

            var settingsSerialized = JsonSerializer.Serialize(_settings, Models.JsonCodeGen.Default.RepositorySettings);
            try
            {
                File.WriteAllText(Path.Combine(_gitDir, "sourcegit.settings"), settingsSerialized);
            }
            catch
            {
                // Ignore
            }
            _autoFetchTimer.Dispose();
            _autoFetchTimer = null;

            _settings = null;
            _historiesFilterMode = Models.FilterMode.None;

            _watcher?.Dispose();
            _histories.Cleanup();
            _workingCopy.Cleanup();
            _stashesPage.Cleanup();

            _watcher = null;
            _histories = null;
            _workingCopy = null;
            _stashesPage = null;

            _localChangesCount = 0;
            _stashesCount = 0;

            _remotes.Clear();
            _branches.Clear();
            _localBranchTrees.Clear();
            _remoteBranchTrees.Clear();
            _tags.Clear();
            _visibleTags.Clear();
            _submodules.Clear();
            _visibleSubmodules.Clear();
            _searchedCommits.Clear();
            _selectedSearchedCommit = null;

            _worktreeFiles = null;
            _matchedFilesForSearching = null;
        }


        public bool CanCreatePopup()
        {
            var page = GetOwnerPage();
            if (page == null)
            {
                Debug.Assert(false);
                return false;
            }
            return !_isAutoFetching && page.CanCreatePopup();
        }
        public void ShowPopup(Popup popup)
        {
            var page = GetOwnerPage();
            if (page == null)
            {
                Debug.Assert(false);
                return;
            }
            page.Popup = popup;
        }

        public void ShowAndStartPopup(Popup popup)
        {
            GetOwnerPage()?.StartPopup(popup);
        }

        public CommandLog CreateLog(string name)
        {
            var log = new CommandLog(name);
            Logs.Insert(0, log);
            return log;
        }

        public void RefreshAll()
        {
            Task.Run(() =>
            {
                var allowedSignersFile = new Commands.Config(_fullpath).Get("gpg.ssh.allowedSignersFile");
                _hasAllowedSignersFile = !string.IsNullOrEmpty(allowedSignersFile);
            });

            Task.Run(RefreshBranches);
            Task.Run(RefreshTags);
            Task.Run(RefreshCommits);
            Task.Run(RefreshSubmodules);
            Task.Run(RefreshWorktrees);
            Task.Run(RefreshWorkingCopyChanges);
            Task.Run(RefreshStashes);
        }

        public void OpenInFileManager()
        {
            Native.OS.OpenInFileManager(_fullpath);
        }

        public void OpenInTerminal()
        {
            Native.OS.OpenTerminal(_fullpath);
        }

        public ContextMenu CreateContextMenuForExternalTools()
        {
            var menu = new ContextMenu();
            menu.Placement = PlacementMode.BottomEdgeAlignedLeft;

            RenderOptions.SetBitmapInterpolationMode(menu, BitmapInterpolationMode.HighQuality);
            RenderOptions.SetEdgeMode(menu, EdgeMode.Antialias);
            RenderOptions.SetTextRenderingMode(menu, TextRenderingMode.Antialias);

            var explore = new MenuItem();
            explore.Header = App.ResText("Repository.Explore");
            explore.Icon = App.CreateMenuIcon("Icons.Explore");
            explore.Click += (_, e) =>
            {
                Native.OS.OpenInFileManager(_fullpath);
                e.Handled = true;
            };

            var terminal = new MenuItem();
            terminal.Header = App.ResText("Repository.Terminal");
            terminal.Icon = App.CreateMenuIcon("Icons.Terminal");
            terminal.Click += (_, e) =>
            {
                Native.OS.OpenTerminal(_fullpath);
                e.Handled = true;
            };

            menu.Items.Add(explore);
            menu.Items.Add(terminal);

            var tools = Native.OS.ExternalTools;
            if (tools.Count > 0)
            {
                menu.Items.Add(new MenuItem() { Header = "-" });

                foreach (var tool in Native.OS.ExternalTools)
                {
                    var dupTool = tool;

                    var item = new MenuItem();
                    item.Header = App.ResText("Repository.OpenIn", dupTool.Name);

                    try
                    {
                        var asset = Avalonia.Platform.AssetLoader.Open(new Uri($"avares://Resources/Images/ExternalToolIcons/{dupTool.IconName}.png", UriKind.RelativeOrAbsolute));
                        item.Icon = new Image { Width = 16, Height = 16, Source = new Bitmap(asset) };
                    }
                    catch
                    {
                        // If icon not found, show no icon or a default one
                        item.Icon = null;
                    }

                    item.Click += (_, e) =>
                    {
                        dupTool.Open(_fullpath);
                        e.Handled = true;
                    };

                    menu.Items.Add(item);
                }
            }
            else
            {
                Debug.Assert(false, "Check this, No available external editors found!");
            }

            var urls = new Dictionary<string, string>();
            foreach (var r in _remotes)
            {
                if (r.TryGetVisitURL(out var visit))
                    urls.Add(r.Name, visit);
            }

            if (urls.Count > 0)
            {
                menu.Items.Add(new MenuItem() { Header = "-" });

                foreach (var url in urls)
                {
                    var name = url.Key;
                    var addr = url.Value;

                    var item = new MenuItem();
                    item.Header = App.ResText("Repository.Visit", name);
                    item.Icon = App.CreateMenuIcon("Icons.Remotes");
                    item.Click += (_, e) =>
                    {
                        Native.OS.OpenBrowser(addr);
                        e.Handled = true;
                    };

                    menu.Items.Add(item);
                }
            }

            return menu;
        }

        public void Fetch(bool autoStart)
        {
            if (!CanCreatePopup())
                return;

            if (_remotes.Count == 0)
            {
                App.RaiseException(_fullpath, "No remotes added to this repository!!!");
                return;
            }

            if (autoStart)
                ShowAndStartPopup(new Fetch(this));
            else
                ShowPopup(new Fetch(this));
        }

        public void Pull(bool autoStart)
        {
            if (!CanCreatePopup())
                return;

            if (_remotes.Count == 0)
            {
                App.RaiseException(_fullpath, "No remotes added to this repository!!!");
                return;
            }

            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Can NOT found current branch!!!");
                return;
            }

            var pull = new Pull(this, null);
            if (autoStart && pull.SelectedBranch != null)
                ShowAndStartPopup(pull);
            else
                ShowPopup(pull);
        }

        public void Push(bool autoStart)
        {
            if (!CanCreatePopup())
                return;

            if (_remotes.Count == 0)
            {
                App.RaiseException(_fullpath, "No remotes added to this repository!!!");
                return;
            }

            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Can NOT found current branch!!!");
                return;
            }

            if (autoStart)
                ShowAndStartPopup(new Push(this, null));
            else
                ShowPopup(new Push(this, null));
        }

        public void ApplyPatch()
        {
            if (!CanCreatePopup())
                return;
            ShowPopup(new Apply(this));
        }

        public void Cleanup()
        {
            if (!CanCreatePopup())
                return;
            ShowAndStartPopup(new Cleanup(this));
        }

        public void ClearFilter()
        {
            Filter = string.Empty;
        }

        public void ClearSearchCommitFilter()
        {
            SearchCommitFilter = string.Empty;
        }

        public void ClearMatchedFilesForSearching()
        {
            MatchedFilesForSearching = null;
        }

        public void StartSearchCommits()
        {
            if (_histories == null)
                return;

            IsSearchLoadingVisible = true;
            SelectedSearchedCommit = null;
            MatchedFilesForSearching = null;

            Task.Run(() =>
            {
                var visible = null as List<Models.Commit>;
                var method = (Models.CommitSearchMethod)_searchCommitFilterType;

                if (method == Models.CommitSearchMethod.BySHA)
                {
                    var commit = new Commands.QuerySingleCommit(_fullpath, _searchCommitFilter).Result();
                    visible = commit == null ? [] : [commit];
                }
                else
                {
                    visible = new Commands.QueryCommits(_fullpath, _searchCommitFilter, method, _onlySearchCommitsInCurrentBranch).Result();
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    SearchedCommits = visible;
                    IsSearchLoadingVisible = false;
                });
            });
        }

        public void SetWatcherEnabled(bool enabled)
        {
            _watcher?.SetEnabled(enabled);
        }

        public void MarkBranchesDirtyManually()
        {
            if (_watcher == null)
            {
                Task.Run(RefreshBranches);
                Task.Run(RefreshCommits);
                Task.Run(RefreshWorkingCopyChanges);
                Task.Run(RefreshWorktrees);
            }
            else
            {
                _watcher.MarkBranchDirtyManually();
            }
        }

        public void MarkTagsDirtyManually()
        {
            if (_watcher == null)
            {
                Task.Run(RefreshTags);
                Task.Run(RefreshCommits);
            }
            else
            {
                _watcher.MarkTagDirtyManually();
            }
        }

        public void MarkWorkingCopyDirtyManually()
        {
            if (_watcher == null)
                Task.Run(RefreshWorkingCopyChanges);
            else
                _watcher.MarkWorkingCopyDirtyManually();
        }

        public void MarkFetched()
        {
            _lastFetchTime = DateTime.Now;
        }

        public void NavigateToCommit(string sha)
        {
            if (_histories != null)
            {
                SelectedViewIndex = 0;
                _histories.NavigateTo(sha);
            }
        }

        public void NavigateToCurrentHead()
        {
            if (_currentBranch != null)
                NavigateToCommit(_currentBranch.Head);
        }

        public void NavigateToBranchDelayed(string branch)
        {
            _navigateToBranchDelayed = branch;
        }

        public void ClearHistoriesFilter()
        {
            _settings.HistoriesFilters.Clear();
            HistoriesFilterMode = Models.FilterMode.None;

            ResetBranchTreeFilterMode(LocalBranchTrees);
            ResetBranchTreeFilterMode(RemoteBranchTrees);
            ResetTagFilterMode();
            Task.Run(RefreshCommits);
        }

        public void RemoveHistoriesFilter(Models.Filter filter)
        {
            if (_settings.HistoriesFilters.Remove(filter))
            {
                HistoriesFilterMode = _settings.HistoriesFilters.Count > 0 ? _settings.HistoriesFilters[0].Mode : Models.FilterMode.None;
                RefreshHistoriesFilters(true);
            }
        }

        public void UpdateBranchNodeIsExpanded(BranchTreeNode node)
        {
            if (_settings == null || !string.IsNullOrWhiteSpace(_filter))
                return;

            if (node.IsExpanded)
            {
                if (!_settings.ExpandedBranchNodesInSideBar.Contains(node.Path))
                    _settings.ExpandedBranchNodesInSideBar.Add(node.Path);
            }
            else
            {
                _settings.ExpandedBranchNodesInSideBar.Remove(node.Path);
            }
        }

        public void SetTagFilterMode(Models.Tag tag, Models.FilterMode mode)
        {
            var changed = _settings.UpdateHistoriesFilter(tag.Name, Models.FilterType.Tag, mode);
            if (changed)
                RefreshHistoriesFilters(true);
        }

        public void SetBranchFilterMode(Models.Branch branch, Models.FilterMode mode, bool clearExists, bool refresh)
        {
            var node = FindBranchNode(branch.IsLocal ? _localBranchTrees : _remoteBranchTrees, branch.FullName);
            if (node != null)
                SetBranchFilterMode(node, mode, clearExists, refresh);
        }

        public void SetBranchFilterMode(BranchTreeNode node, Models.FilterMode mode, bool clearExists, bool refresh)
        {
            var isLocal = node.Path.StartsWith("refs/heads/", StringComparison.Ordinal);
            var tree = isLocal ? _localBranchTrees : _remoteBranchTrees;

            if (clearExists)
            {
                _settings.HistoriesFilters.Clear();
                HistoriesFilterMode = Models.FilterMode.None;
            }

            if (node.Backend is Models.Branch branch)
            {
                var type = isLocal ? Models.FilterType.LocalBranch : Models.FilterType.RemoteBranch;
                var changed = _settings.UpdateHistoriesFilter(node.Path, type, mode);
                if (!changed)
                    return;

                if (isLocal && !string.IsNullOrEmpty(branch.Upstream))
                    _settings.UpdateHistoriesFilter(branch.Upstream, Models.FilterType.RemoteBranch, mode);
            }
            else
            {
                var type = isLocal ? Models.FilterType.LocalBranchFolder : Models.FilterType.RemoteBranchFolder;
                var changed = _settings.UpdateHistoriesFilter(node.Path, type, mode);
                if (!changed)
                    return;

                _settings.RemoveChildrenBranchFilters(node.Path);
            }

            var parentType = isLocal ? Models.FilterType.LocalBranchFolder : Models.FilterType.RemoteBranchFolder;
            var cur = node;
            do
            {
                var lastSepIdx = cur.Path.LastIndexOf('/');
                if (lastSepIdx <= 0)
                    break;

                var parentPath = cur.Path.Substring(0, lastSepIdx);
                var parent = FindBranchNode(tree, parentPath);
                if (parent == null)
                    break;

                _settings.UpdateHistoriesFilter(parent.Path, parentType, Models.FilterMode.None);
                cur = parent;
            } while (true);

            RefreshHistoriesFilters(refresh);
        }

        public void StashAll(bool autoStart)
        {
            _workingCopy?.StashAll(autoStart);
        }

        public void SkipMerge()
        {
            _workingCopy?.SkipMerge();
        }

        public void AbortMerge()
        {
            _workingCopy?.AbortMerge();
        }

        public List<Models.CustomAction> GetCustomActions(Models.CustomActionScope scope)
        {
            var actions = new List<Models.CustomAction>();

            foreach (var act in Preferences.Instance.CustomActions)
            {
                if (act.Scope == scope)
                    actions.Add(act);
            }

            foreach (var act in _settings.CustomActions)
            {
                if (act.Scope == scope)
                    actions.Add(act);
            }

            return actions;
        }

        public void Bisect(string subcmd)
        {
            IsBisectCommandRunning = true;
            SetWatcherEnabled(false);

            var log = CreateLog($"Bisect({subcmd})");
            Task.Run(() =>
            {
                var succ = new Commands.Bisect(_fullpath, subcmd).Use(log).Exec();
                log.Complete();

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (!succ)
                        App.RaiseException(_fullpath, log.Content.Substring(log.Content.IndexOf('\n')).Trim());
                    else if (log.Content.Contains("is the first bad commit"))
                        App.SendNotification(_fullpath, log.Content.Substring(log.Content.IndexOf('\n')).Trim());

                    MarkBranchesDirtyManually();
                    SetWatcherEnabled(true);
                    IsBisectCommandRunning = false;
                });
            });
        }

        public void RefreshBranches()
        {
            var branches = new Commands.QueryBranches(_fullpath).Result();
            var remotes = new Commands.QueryRemotes(_fullpath).Result();
            var builder = BuildBranchTree(branches, remotes);

            Dispatcher.UIThread.Invoke(() =>
            {
                lock (_lockRemotes)
                    Remotes = remotes;

                Branches = branches;
                CurrentBranch = branches.Find(x => x.IsCurrent);
                LocalBranchTrees = builder.Locals;
                RemoteBranchTrees = builder.Remotes;

                if (_workingCopy != null)
                    _workingCopy.HasRemotes = remotes.Count > 0;

                GetOwnerPage()?.ChangeDirtyState(Models.DirtyState.HasPendingPullOrPush, !CurrentBranch.TrackStatus.IsVisible);
            });
        }

        public void RefreshWorktrees()
        {
            var worktrees = new Commands.Worktree(_fullpath).List();
            var cleaned = new List<Models.Worktree>();

            foreach (var worktree in worktrees)
            {
                if (worktree.IsBare || worktree.FullPath.Equals(_fullpath))
                    continue;

                cleaned.Add(worktree);
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                Worktrees = cleaned;
            });
        }

        public void RefreshTags()
        {
            var tags = new Commands.QueryTags(_fullpath).Result();
            Dispatcher.UIThread.Invoke(() =>
            {
                Tags = tags;
                VisibleTags = BuildVisibleTags();
            });
        }

        public void RefreshCommits()
        {
            Dispatcher.UIThread.Invoke(() => _histories.IsLoading = true);

            var builder = new StringBuilder();
            builder.Append($"-{Preferences.Instance.MaxHistoryCommits} ");

            if (_settings.EnableTopoOrderInHistories)
                builder.Append("--topo-order ");
            else
                builder.Append("--date-order ");

            if (_settings.EnableReflog)
                builder.Append("--reflog ");
            if (_settings.EnableFirstParentInHistories)
                builder.Append("--first-parent ");

            var filters = _settings.BuildHistoriesFilter();
            if (string.IsNullOrEmpty(filters))
                builder.Append("--branches --remotes --tags HEAD");
            else
                builder.Append(filters);

            var commits = new Commands.QueryCommits(_fullpath, builder.ToString()).Result();
            var graph = Models.CommitGraph.Parse(commits, _settings.EnableFirstParentInHistories);

            Dispatcher.UIThread.Invoke(() =>
            {
                if (_histories != null)
                {
                    _histories.IsLoading = false;
                    _histories.Commits = commits;
                    _histories.Graph = graph;

                    BisectState = _histories.UpdateBisectInfo();

                    if (!string.IsNullOrEmpty(_navigateToBranchDelayed))
                    {
                        var branch = _branches.Find(x => x.FullName == _navigateToBranchDelayed);
                        if (branch != null)
                            NavigateToCommit(branch.Head);
                    }
                }

                _navigateToBranchDelayed = string.Empty;
            });
        }

        public void RefreshSubmodules()
        {
            var submodules = new Commands.QuerySubmodules(_fullpath).Result();
            _watcher?.SetSubmodules(submodules);

            Dispatcher.UIThread.Invoke(() =>
            {
                Submodules = submodules;
                VisibleSubmodules = BuildVisibleSubmodules();
            });
        }

        public void RefreshWorkingCopyChanges()
        {
            if (IsBare)
                return;

            var changes = new Commands.QueryLocalChanges(_fullpath, _settings.IncludeUntrackedInLocalChanges).Result();
            if (_workingCopy == null)
                return;

            _workingCopy.SetData(changes);

            Dispatcher.UIThread.Invoke(() =>
            {
                LocalChangesCount = changes.Count;
                OnPropertyChanged(nameof(InProgressContext));
                GetOwnerPage()?.ChangeDirtyState(Models.DirtyState.HasLocalChanges, changes.Count == 0);
            });
        }

        public void RefreshStashes()
        {
            if (IsBare)
                return;

            var stashes = new Commands.QueryStashes(_fullpath).Result();
            Dispatcher.UIThread.Invoke(() =>
            {
                if (_stashesPage != null)
                    _stashesPage.Stashes = stashes;

                StashesCount = stashes.Count;
            });
        }

        public void CreateNewBranch()
        {
            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Git do not hold any branch until you do first commit.");
                return;
            }

            if (CanCreatePopup())
                ShowPopup(new CreateBranch(this, _currentBranch));
        }

        public void CheckoutBranch(Models.Branch branch)
        {
            if (branch.IsLocal)
            {
                var worktree = _worktrees.Find(x => x.Branch == branch.FullName);
                if (worktree != null)
                {
                    OpenWorktree(worktree);
                    return;
                }
            }

            if (IsBare)
                return;

            if (!CanCreatePopup())
                return;

            if (branch.IsLocal)
            {
                if (_localChangesCount > 0 || _submodules.Count > 0)
                    ShowPopup(new Checkout(this, branch.Name));
                else
                    ShowAndStartPopup(new Checkout(this, branch.Name));
            }
            else
            {
                foreach (var b in _branches)
                {
                    if (b.IsLocal && b.Upstream == branch.FullName)
                    {
                        if (!b.IsCurrent)
                            CheckoutBranch(b);

                        return;
                    }
                }

                ShowPopup(new CreateBranch(this, branch));
            }
        }

        public void DeleteMultipleBranches(List<Models.Branch> branches, bool isLocal)
        {
            if (CanCreatePopup())
                ShowPopup(new DeleteMultipleBranches(this, branches, isLocal));
        }

        public void MergeMultipleBranches(List<Models.Branch> branches)
        {
            if (CanCreatePopup())
                ShowPopup(new MergeMultiple(this, branches));
        }

        public void CreateNewTag()
        {
            if (_currentBranch == null)
            {
                App.RaiseException(_fullpath, "Git do not hold any branch until you do first commit.");
                return;
            }

            if (CanCreatePopup())
                ShowPopup(new CreateTag(this, _currentBranch));
        }

        public void AddRemote()
        {
            if (CanCreatePopup())
                ShowPopup(new AddRemote(this));
        }

        public void AddSubmodule()
        {
            if (CanCreatePopup())
                ShowPopup(new AddSubmodule(this));
        }

        public void UpdateSubmodules()
        {
            if (CanCreatePopup())
                ShowPopup(new UpdateSubmodules(this));
        }

        public void OpenSubmodule(string submodule)
        {
            var selfPage = GetOwnerPage();
            if (selfPage == null)
                return;

            var root = Path.GetFullPath(Path.Combine(_fullpath, submodule));
            var normalizedPath = root.Replace("\\", "/");

            var node = Preferences.Instance.FindNode(normalizedPath);
            if (node == null)
            {
                node = new RepositoryNode()
                {
                    Id = normalizedPath,
                    Name = Path.GetFileName(normalizedPath),
                    Bookmark = selfPage.Node.Bookmark,
                    IsRepository = true,
                };
            }

            App.GetLauncer().OpenRepositoryInTab(node, null);
        }

        public void AddWorktree()
        {
            if (CanCreatePopup())
                ShowPopup(new AddWorktree(this));
        }

        public void PruneWorktrees()
        {
            if (CanCreatePopup())
                ShowAndStartPopup(new PruneWorktrees(this));
        }

        public void OpenWorktree(Models.Worktree worktree)
        {
            var node = Preferences.Instance.FindNode(worktree.FullPath);
            if (node == null)
            {
                node = new RepositoryNode()
                {
                    Id = worktree.FullPath,
                    Name = Path.GetFileName(worktree.FullPath),
                    Bookmark = 0,
                    IsRepository = true,
                };
            }

            App.GetLauncer().OpenRepositoryInTab(node, null);
        }

        public List<Models.OpenAIService> GetPreferedOpenAIServices()
        {
            var services = Preferences.Instance.OpenAIServices;
            if (services == null || services.Count == 0)
                return [];

            if (services.Count == 1)
                return [services[0]];

            var prefered = _settings.PreferedOpenAIService;
            var all = new List<Models.OpenAIService>();
            foreach (var service in services)
            {
                if (service.Name.Equals(prefered, StringComparison.Ordinal))
                    return [service];

                all.Add(service);
            }

            return all;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForGitFlow()
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            // TODO: apply menu.Placement = PlacementMode.BottomEdgeAlignedLeft;
            menu.ViewToDo = new ViewModelInfo(new()
            {
                ( "Placement" , "PlacementMode.BottomEdgeAlignedLeft" )
            }
             );
            var isGitFlowEnabled = Commands.GitFlow.IsEnabled(_fullpath, _branches);
            if (isGitFlowEnabled)
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitFlow.StartFeature"),
                    IconKey = App.MenuIconKey("Icons.GitFlow.Feature"),
                    Command = StartFeatureCommand,
                    IsEnabled = true
                });
                //  StartFeatureCommand = new ViewModels.DelegateCommand( );
                //  var startFeature =
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitFlow.StartRelease"),
                    IconKey = App.MenuIconKey("Icons.GitFlow.Release"),
                    Command = StartReleaseCommand,
                    IsEnabled = true
                });
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitFlow.StartHotfix"),
                    IconKey = App.MenuIconKey("Icons.GitFlow.Hotfix"),
                    Command = StartHotfixCommand,
                    IsEnabled = true
                });
            }
            else
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitFlow.Init"),
                    IconKey = App.MenuIconKey("Icons.Init"),
                    Command = InitGitFlowCommand,
                    IsEnabled = true
                });
            }
            return menu;
        }

        // GitFlow command properties and implementations for menu items
        public RelayCommand StartFeatureCommand { get; private set; }
        public RelayCommand StartReleaseCommand { get; private set; }
        public RelayCommand StartHotfixCommand { get; private set; }
        public RelayCommand InitGitFlowCommand { get; private set; }

        private void GitFlowStartFeature()
        {
            if (CanCreatePopup())
                ShowPopup(new GitFlowStart(this, "feature"));
        }
        private void GitFlowStartRelease()
        {
            if (CanCreatePopup())
                ShowPopup(new GitFlowStart(this, "release"));
        }
        private void GitFlowStartHotfix()
        {
            if (CanCreatePopup())
                ShowPopup(new GitFlowStart(this, "hotfix"));
        }
        private void GitFlowInit()
        {
            if (CanCreatePopup())
                ShowPopup(new InitGitFlow(this));
        }


        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForGitLFS()
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            var lfs = new Commands.LFS(_fullpath);
            if (lfs.IsEnabled())
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitLFS.AddTrackPattern"),
                    IconKey = App.MenuIconKey("Icons.File.Add"),
                    Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new LFSTrackCustomPattern(this)); })
                });
                items.Add(new MenuItemModel { Header = "-" });

                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitLFS.Fetch"),
                    IconKey = App.MenuIconKey("Icons.Fetch"),
                    IsEnabled = _remotes.Count > 0,
                    Command = new RelayCommand(() =>
                    {
                        if (CanCreatePopup())
                        {
                            if (_remotes.Count == 1)
                                ShowAndStartPopup(new LFSFetch(this));
                            else
                                ShowPopup(new LFSFetch(this));
                        }
                    })
                });
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitLFS.Pull"),
                    IconKey = App.MenuIconKey("Icons.Pull"),
                    IsEnabled = _remotes.Count > 0,
                    Command = new RelayCommand(() =>
                    {
                        if (CanCreatePopup())
                        {
                            if (_remotes.Count == 1)
                                ShowAndStartPopup(new LFSPull(this));
                            else
                                ShowPopup(new LFSPull(this));
                        }
                    })
                });
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitLFS.Push"),
                    IconKey = App.MenuIconKey("Icons.Push"),
                    IsEnabled = _remotes.Count > 0,
                    Command = new RelayCommand(() =>
                    {
                        if (CanCreatePopup())
                        {
                            if (_remotes.Count == 1)
                                ShowAndStartPopup(new LFSPush(this));
                            else
                                ShowPopup(new LFSPush(this));
                        }
                    })
                });
                items.Add(new MenuItemModel { Header = "-" });
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitLFS.Prune"),
                    IconKey = App.MenuIconKey("Icons.Clean"),
                    Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new LFSPrune(this)); })
                });
                // Use MenuModel for submenus (locks menu)
                var locks = new MenuModel
                {
                    Header = App.ResText("GitLFS.Locks"),
                    IconKey = App.MenuIconKey("Icons.Lock"),
                    IsEnabled = _remotes.Count > 0
                };
                if (_remotes.Count == 1)
                {
                    locks.Command = new RelayCommand(() =>
                    App.ShowWindow(new LFSLocks(this, _remotes[0].Name), true))
                   ;
                }
                else
                {

                    foreach (var remote in _remotes)
                    {
                        var remoteName = remote.Name;
                        locks.Items.Add(new MenuItemModel
                        {
                            Header = remoteName,
                            IconKey = App.MenuIconKey("Icons.Lock"),
                            Command = new RelayCommand(() => App.ShowWindow(new LFSLocks(this, remoteName), true))
                        });
                    }

                }
                items.Add(new MenuItemModel { Header = "-" });
                items.Add(locks);
            }
            else
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("GitLFS.Install"),
                    IconKey = App.MenuIconKey("Icons.Init"),
                    Command = new RelayCommand(() =>
                    {
                        var log = CreateLog("Install LFS");
                        var succ = new Commands.LFS(_fullpath).Install(log);
                        if (succ)
                            App.SendNotification(_fullpath, $"LFS enabled successfully!");
                        log.Complete();
                    })
                });
            }
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForCustomAction()
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            var actions = GetCustomActions(Models.CustomActionScope.Repository);
            if (actions.Count > 0)
            {
                foreach (var action in actions)
                {
                    var dup = action;
                    items.Add(new MenuItemModel
                    {
                        Header = dup.Name,
                        IconKey = App.MenuIconKey("Icons.Action"),
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new ExecuteCustomAction(this, dup)); })
                    });
                }
            }
            else
            {
                items.Add(new MenuItemModel { Header = App.ResText("Repository.CustomActions.Empty") });
            }
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForHistoriesPage()
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            var isHorizontal = Preferences.Instance.UseTwoColumnsLayoutInHistories;
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Repository.HistoriesLayout"),
                IsEnabled = false
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Repository.HistoriesLayout.Horizontal"),
                IconKey = isHorizontal ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => Preferences.Instance.UseTwoColumnsLayoutInHistories = true)
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Repository.HistoriesLayout.Vertical"),
                IconKey = !isHorizontal ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => Preferences.Instance.UseTwoColumnsLayoutInHistories = false)
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Repository.HistoriesOrder"),
                IsEnabled = false
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Repository.HistoriesOrder.ByDate"),
                IconKey = !_settings.EnableTopoOrderInHistories ? App.MenuIconKey("Icons.Check") : null,
                ViewToDo = new ViewModelInfo(new() { ("Views.MenuItemExtension.CommandProperty", "--date-order") }),
                Command = new RelayCommand(() =>
                {
                    if (_settings.EnableTopoOrderInHistories)
                    {
                        _settings.EnableTopoOrderInHistories = false;
                        Task.Run(RefreshCommits);
                    }
                })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Repository.HistoriesOrder.Topo"),
                IconKey = _settings.EnableTopoOrderInHistories ? App.MenuIconKey("Icons.Check") : null,
                ViewToDo = new ViewModelInfo(new() { ("Views.MenuItemExtension.CommandProperty", "--top-order") }),
                Command = new RelayCommand(() =>
                {
                    if (!_settings.EnableTopoOrderInHistories)
                    {
                        _settings.EnableTopoOrderInHistories = true;
                        Task.Run(RefreshCommits);
                    }
                })
            });
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForLocalBranch(Models.Branch branch)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            var push = new MenuItemModel
            {
                Header = App.ResText("BranchCM.Push", branch.Name),
                IconKey = App.MenuIconKey("Icons.Push"),
                IsEnabled = _remotes.Count > 0,
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Push(this, branch)); })
            };

            if (branch.IsCurrent)
            {
                if (!IsBare)
                {
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.DiscardAll"),
                        IconKey = App.MenuIconKey("Icons.Undo"),
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Discard(this)); })
                    });
                    items.Add(new MenuItemModel { Header = "-" });
                    if (!string.IsNullOrEmpty(branch.Upstream))
                    {
                        var upstream = branch.Upstream.Substring(13);
                        items.Add(new MenuItemModel
                        {
                            Header = App.ResText("BranchCM.FastForward", upstream),
                            IconKey = App.MenuIconKey("Icons.FastForward"),
                            IsEnabled = branch.TrackStatus.Ahead.Count == 0,
                            Command = new RelayCommand(() =>
                            {
                                var b = _branches.Find(x => x.FriendlyName == upstream);
                                if (b == null)
                                    return;
                                if (CanCreatePopup())
                                    ShowAndStartPopup(new Merge(this, b, branch.Name, true));
                            })
                        });
                        items.Add(new MenuItemModel
                        {
                            Header = App.ResText("BranchCM.Pull", upstream),
                            IconKey = App.MenuIconKey("Icons.Pull"),
                            Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Pull(this, null)); })
                        });
                    }
                }
                items.Add(push);
            }
            else
            {
                if (!IsBare)
                {
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.Checkout", branch.Name),
                        IconKey = App.MenuIconKey("Icons.Check"),
                        Command = new RelayCommand(() => CheckoutBranch(branch))
                    });
                    items.Add(new MenuItemModel { Header = "-" });
                }
                var worktree = _worktrees.Find(x => x.Branch == branch.FullName);
                var upstream = _branches.Find(x => x.FullName == branch.Upstream);
                if (upstream != null && worktree == null)
                {
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.FastForward", upstream.FriendlyName),
                        IconKey = App.MenuIconKey("Icons.FastForward"),
                        IsEnabled = branch.TrackStatus.Ahead.Count == 0,
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new FastForwardWithoutCheckout(this, branch, upstream)); })
                    });
                    items.Add(new MenuItemModel { Header = "-" });
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.FetchInto", upstream.FriendlyName, branch.Name),
                        IconKey = App.MenuIconKey("Icons.Fetch"),
                        IsEnabled = branch.TrackStatus.Ahead.Count == 0,
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new FetchInto(this, branch, upstream)); })
                    });
                }
                items.Add(push);
                if (!IsBare)
                {
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.Merge", branch.Name, _currentBranch.Name),
                        IconKey = App.MenuIconKey("Icons.Merge"),
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Merge(this, branch, _currentBranch.Name, false)); })
                    });
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.Rebase", _currentBranch.Name, branch.Name),
                        IconKey = App.MenuIconKey("Icons.Rebase"),
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Rebase(this, _currentBranch, branch)); })
                    });
                }
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("BranchCM.CompareWithHead"),
                    IconKey = App.MenuIconKey("Icons.Compare"),
                    Command = new RelayCommand(() => App.ShowWindow(new BranchCompare(_fullpath, branch, _currentBranch), false))
                });
                items.Add(new MenuItemModel { Header = "-" });
                if (_localChangesCount > 0)
                {
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.CompareWithWorktree"),
                        IconKey = App.MenuIconKey("Icons.Compare"),
                        Command = new RelayCommand(() =>
                        {
                            SelectedSearchedCommit = null;
                            if (_histories != null)
                            {
                                var target = new Commands.QuerySingleCommit(_fullpath, branch.Head).Result();
                                _histories.AutoSelectedCommit = null;
                                _histories.DetailContext = new RevisionCompare(_fullpath, target, null);
                            }
                        })
                    });
                }
            }
            if (!IsBare)
            {
                var detect = Commands.GitFlow.DetectType(_fullpath, _branches, branch.Name);
                if (detect.IsGitFlowBranch)
                {
                    items.Add(new MenuItemModel { Header = "-" });
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.Finish", branch.Name),
                        IconKey = App.MenuIconKey("Icons.GitFlow"),
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new GitFlowFinish(this, branch, detect.Type, detect.Prefix)); })
                    });
                }
            }
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.Rename", branch.Name),
                IconKey = App.MenuIconKey("Icons.Rename"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new RenameBranch(this, branch)); })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.Delete", branch.Name),
                IconKey = App.MenuIconKey("Icons.Clear"),
                IsEnabled = !branch.IsCurrent,
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new DeleteBranch(this, branch)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("CreateBranch"),
                IconKey = App.MenuIconKey("Icons.Branch.Add"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new CreateBranch(this, branch)); })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("CreateTag"),
                IconKey = App.MenuIconKey("Icons.Tag.Add"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new CreateTag(this, branch)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            // Custom actions submenu
            TryToAddCustomActionsToBranchContextMenu(menu, branch);
            if (!IsBare)
            {
                var remoteBranches = new List<Models.Branch>();
                foreach (var b in _branches)
                {
                    if (!b.IsLocal)
                        remoteBranches.Add(b);
                }
                if (remoteBranches.Count > 0)
                {
                    items.Add(new MenuItemModel
                    {
                        Header = App.ResText("BranchCM.Tracking"),
                        IconKey = App.MenuIconKey("Icons.Track"),
                        Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new SetUpstream(this, branch, remoteBranches)); })
                    });
                }
            }
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Archive"),
                IconKey = App.MenuIconKey("Icons.Archive"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Archive(this, branch)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.CopyName"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(branch.Name))
            });
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForRemote(Models.Remote remote)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            if (remote.TryGetVisitURL(out string visitURL))
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("RemoteCM.OpenInBrowser"),
                    IconKey = App.MenuIconKey("Icons.OpenWith"),
                    Command = new RelayCommand(() => Native.OS.OpenBrowser(visitURL))
                });
                items.Add(new MenuItemModel { Header = "-" });
            }
            items.Add(new MenuItemModel
            {
                Header = App.ResText("RemoteCM.Fetch"),
                IconKey = App.MenuIconKey("Icons.Fetch"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new Fetch(this, remote)); })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("RemoteCM.Prune"),
                IconKey = App.MenuIconKey("Icons.Clean"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new PruneRemote(this, remote)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("RemoteCM.Edit"),
                IconKey = App.MenuIconKey("Icons.Edit"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new EditRemote(this, remote)); })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("RemoteCM.Delete"),
                IconKey = App.MenuIconKey("Icons.Clear"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new DeleteRemote(this, remote)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("RemoteCM.CopyURL"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(remote.URL))
            });
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForRemoteBranch(Models.Branch branch)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            var name = branch.FriendlyName;
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.Checkout", name),
                IconKey = App.MenuIconKey("Icons.Check"),
                Command = new RelayCommand(() => CheckoutBranch(branch))
            });
            items.Add(new MenuItemModel { Header = "-" });
            if (_currentBranch != null)
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("BranchCM.PullInto", name, _currentBranch.Name),
                    IconKey = App.MenuIconKey("Icons.Pull"),
                    Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Pull(this, branch)); })
                });
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("BranchCM.Merge", name, _currentBranch.Name),
                    IconKey = App.MenuIconKey("Icons.Merge"),
                    Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Merge(this, branch, _currentBranch.Name, false)); })
                });
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("BranchCM.Rebase", _currentBranch.Name, name),
                    IconKey = App.MenuIconKey("Icons.Rebase"),
                    Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Rebase(this, _currentBranch, branch)); })
                });
                items.Add(new MenuItemModel { Header = "-" });
            }
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.CompareWithHead"),
                IconKey = App.MenuIconKey("Icons.Compare"),
                Command = new RelayCommand(() => App.ShowWindow(new BranchCompare(_fullpath, branch, _currentBranch), false))
            });
            if (_localChangesCount > 0)
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("BranchCM.CompareWithWorktree"),
                    IconKey = App.MenuIconKey("Icons.Compare"),
                    Command = new RelayCommand(() =>
                    {
                        SelectedSearchedCommit = null;
                        if (_histories != null)
                        {
                            var target = new Commands.QuerySingleCommit(_fullpath, branch.Head).Result();
                            _histories.AutoSelectedCommit = null;
                            _histories.DetailContext = new RevisionCompare(_fullpath, target, null);
                        }
                    })
                });
            }
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.Delete", name),
                IconKey = App.MenuIconKey("Icons.Clear"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new DeleteBranch(this, branch)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("CreateBranch"),
                IconKey = App.MenuIconKey("Icons.Branch.Add"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new CreateBranch(this, branch)); })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("CreateTag"),
                IconKey = App.MenuIconKey("Icons.Tag.Add"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new CreateTag(this, branch)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Archive"),
                IconKey = App.MenuIconKey("Icons.Archive"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Archive(this, branch)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            TryToAddCustomActionsToBranchContextMenu(menu, branch);
            items.Add(new MenuItemModel
            {
                Header = App.ResText("BranchCM.CopyName"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(name))
            });
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForTag(Models.Tag tag)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            // [MenuItemModel] usage: Header via App.Text, IconKey via App.MenuIconKey, Command via RelayCommand
            items.Add(new MenuItemModel
            {
                Header = App.ResText("CreateBranch"),
                IconKey = App.MenuIconKey("Icons.Branch.Add"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new CreateBranch(this, tag)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("TagCM.Push", tag.Name),
                IconKey = App.MenuIconKey("Icons.Push"),
                IsEnabled = _remotes.Count > 0,
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new PushTag(this, tag)); })
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("TagCM.Delete", tag.Name),
                IconKey = App.MenuIconKey("Icons.Clear"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new DeleteTag(this, tag)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("Archive"),
                IconKey = App.MenuIconKey("Icons.Archive"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new Archive(this, tag)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("TagCM.Copy"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(tag.Name))
            });
            items.Add(new MenuItemModel
            {
                Header = App.ResText("TagCM.CopyMessage"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                IsEnabled = !string.IsNullOrEmpty(tag.Message),
                Command = new RelayCommand(() => App.CopyText(tag.Message))
            });
            return menu;
        }

        public ContextMenuModel CreateContextMenuForBranchSortMode(bool local)
        {
            var mode = local ? _settings.LocalBranchSortMode : _settings.RemoteBranchSortMode;
            var changeMode = new Action<Models.BranchSortMode>(m =>
            {
                if (local)
                    _settings.LocalBranchSortMode = m;
                else
                    _settings.RemoteBranchSortMode = m;

                var builder = BuildBranchTree(_branches, _remotes);
                LocalBranchTrees = builder.Locals;
                RemoteBranchTrees = builder.Remotes;
            });

            var menu = new ContextMenuModel();
            var items = menu.Items;
            items.Add(new MenuItemModel {
                Header = App.ResText("Repository.BranchSort.ByName"),
                IconKey = mode == Models.BranchSortMode.Name ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => { if (mode != Models.BranchSortMode.Name) changeMode(Models.BranchSortMode.Name); })
            });
            items.Add(new MenuItemModel {
                Header = App.ResText("Repository.BranchSort.ByCommitterDate"),
                IconKey = mode == Models.BranchSortMode.CommitterDate ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => { if (mode != Models.BranchSortMode.CommitterDate) changeMode(Models.BranchSortMode.CommitterDate); })
            });
            return menu;
        }

        public ContextMenuModel CreateContextMenuForTagSortMode()
        {
            var mode = _settings.TagSortMode;
            var changeMode = new Action<Models.TagSortMode>((m) =>
            {
                if (_settings.TagSortMode != m)
                {
                    _settings.TagSortMode = m;
                    VisibleTags = BuildVisibleTags();
                }
            });

            var menu = new ContextMenuModel();
            var items = menu.Items;
            items.Add(new MenuItemModel {
                Header = App.ResText("Repository.Tags.OrderByCreatorDate"),
                IconKey = mode == Models.TagSortMode.CreatorDate ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => changeMode(Models.TagSortMode.CreatorDate))
            });
            items.Add(new MenuItemModel {
                Header = App.ResText("Repository.Tags.OrderByNameAsc"),
                IconKey = mode == Models.TagSortMode.NameInAscending ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => changeMode(Models.TagSortMode.NameInAscending))
            });
            items.Add(new MenuItemModel {
                Header = App.ResText("Repository.Tags.OrderByNameDes"),
                IconKey = mode == Models.TagSortMode.NameInDescending ? App.MenuIconKey("Icons.Check") : null,
                Command = new RelayCommand(() => changeMode(Models.TagSortMode.NameInDescending))
            });
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForSubmodule(string submodule)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            items.Add(new MenuItemModel {
                Header = App.ResText("Submodule.Open"),
                IconKey = App.MenuIconKey("Icons.Folder.Open"),
                Command = new RelayCommand(() => OpenSubmodule(submodule))
            });
            items.Add(new MenuItemModel {
                Header = App.ResText("Submodule.CopyPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(submodule))
            });
            items.Add(new MenuItemModel {
                Header = App.ResText("Submodule.Remove"),
                IconKey = App.MenuIconKey("Icons.Clear"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new DeleteSubmodule(this, submodule)); })
            });
            return menu;
        }

        // Refactored from Avalonia.Controls.ContextMenu/MenuItem usage to ViewModel POCO MenuItem for MVVM compliance
        public ContextMenuModel CreateContextMenuForWorktree(Models.Worktree worktree)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            if (worktree.IsLocked)
            {
                items.Add(new MenuItemModel {
                    Header = App.ResText("Worktree.Unlock"),
                    IconKey = App.MenuIconKey("Icons.Unlock"),
                    Command = new RelayCommand(() => {
                        SetWatcherEnabled(false);
                        var log = CreateLog("Unlock Worktree");
                        var succ = new Commands.Worktree(_fullpath).Use(log).Unlock(worktree.FullPath);
                        if (succ)
                            worktree.IsLocked = false;
                        log.Complete();
                        SetWatcherEnabled(true);
                    })
                });
            }
            else
            {
                items.Add(new MenuItemModel {
                    Header = App.ResText("Worktree.Lock"),
                    IconKey = App.MenuIconKey("Icons.Lock"),
                    Command = new RelayCommand(() => {
                        SetWatcherEnabled(false);
                        var log = CreateLog("Lock Worktree");
                        var succ = new Commands.Worktree(_fullpath).Use(log).Lock(worktree.FullPath);
                        if (succ)
                            worktree.IsLocked = true;
                        log.Complete();
                        SetWatcherEnabled(true);
                    })
                });
            }
            items.Add(new MenuItemModel {
                Header = App.ResText("Worktree.Remove"),
                IconKey = App.MenuIconKey("Icons.Clear"),
                Command = new RelayCommand(() => { if (CanCreatePopup()) ShowPopup(new RemoveWorktree(this, worktree)); })
            });
            items.Add(new MenuItemModel { Header = "-" });
            items.Add(new MenuItemModel {
                Header = App.ResText("Worktree.CopyPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(worktree.FullPath))
            });
            return menu;
        }

        private LauncherPage GetOwnerPage()
        {
            var launcher = App.GetLauncer();
            if (launcher == null)
            {
                Debug.Assert(false, "Launcher not available?");
                return null;
            }

            foreach (var page in launcher.Pages)
            {
                if (page.Node.Id.Equals(_fullpath))
                    return page;
            }

            return null;
        }

        private BranchTreeNode.Builder BuildBranchTree(List<Models.Branch> branches, List<Models.Remote> remotes)
        {
            var builder = new BranchTreeNode.Builder(_settings.LocalBranchSortMode, _settings.RemoteBranchSortMode);
            if (string.IsNullOrEmpty(_filter))
            {
                builder.SetExpandedNodes(_settings.ExpandedBranchNodesInSideBar);
                builder.Run(branches, remotes, false);

                foreach (var invalid in builder.InvalidExpandedNodes)
                    _settings.ExpandedBranchNodesInSideBar.Remove(invalid);
            }
            else
            {
                var visibles = new List<Models.Branch>();
                foreach (var b in branches)
                {
                    if (b.FullName.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        visibles.Add(b);
                }

                builder.Run(visibles, remotes, true);
            }

            var historiesFilters = _settings.CollectHistoriesFilters();
            UpdateBranchTreeFilterMode(builder.Locals, historiesFilters);
            UpdateBranchTreeFilterMode(builder.Remotes, historiesFilters);
            return builder;
        }

        private List<Models.Tag> BuildVisibleTags()
        {
            switch (_settings.TagSortMode)
            {
                case Models.TagSortMode.CreatorDate:
                    _tags.Sort((l, r) => r.CreatorDate.CompareTo(l.CreatorDate));
                    break;
                case Models.TagSortMode.NameInAscending:
                    _tags.Sort((l, r) => Models.NumericSort.Compare(l.Name, r.Name));
                    break;
                default:
                    _tags.Sort((l, r) => Models.NumericSort.Compare(r.Name, l.Name));
                    break;
            }

            var visible = new List<Models.Tag>();
            if (string.IsNullOrEmpty(_filter))
            {
                visible.AddRange(_tags);
            }
            else
            {
                foreach (var t in _tags)
                {
                    if (t.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(t);
                }
            }

            var historiesFilters = _settings.CollectHistoriesFilters();
            UpdateTagFilterMode(historiesFilters);
            return visible;
        }

        private List<Models.Submodule> BuildVisibleSubmodules()
        {
            var visible = new List<Models.Submodule>();
            if (string.IsNullOrEmpty(_filter))
            {
                visible.AddRange(_submodules);
            }
            else
            {
                foreach (var s in _submodules)
                {
                    if (s.Path.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(s);
                }
            }
            return visible;
        }

        private void RefreshHistoriesFilters(bool refresh)
        {
            if (_settings.HistoriesFilters.Count > 0)
                HistoriesFilterMode = _settings.HistoriesFilters[0].Mode;
            else
                HistoriesFilterMode = Models.FilterMode.None;

            if (!refresh)
                return;

            var filters = _settings.CollectHistoriesFilters();
            UpdateBranchTreeFilterMode(LocalBranchTrees, filters);
            UpdateBranchTreeFilterMode(RemoteBranchTrees, filters);
            UpdateTagFilterMode(filters);

            Task.Run(RefreshCommits);
        }

        private void UpdateBranchTreeFilterMode(List<BranchTreeNode> nodes, Dictionary<string, Models.FilterMode> filters)
        {
            foreach (var node in nodes)
            {
                if (filters.TryGetValue(node.Path, out var value))
                    node.FilterMode = value;
                else
                    node.FilterMode = Models.FilterMode.None;

                if (!node.IsBranch)
                    UpdateBranchTreeFilterMode(node.Children, filters);
            }
        }

        private void UpdateTagFilterMode(Dictionary<string, Models.FilterMode> filters)
        {
            foreach (var tag in _tags)
            {
                if (filters.TryGetValue(tag.Name, out var value))
                    tag.FilterMode = value;
                else
                    tag.FilterMode = Models.FilterMode.None;
            }
        }

        private void ResetBranchTreeFilterMode(List<BranchTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.FilterMode = Models.FilterMode.None;
                if (!node.IsBranch)
                    ResetBranchTreeFilterMode(node.Children);
            }
        }

        private void ResetTagFilterMode()
        {
            foreach (var tag in _tags)
                tag.FilterMode = Models.FilterMode.None;
        }

        private BranchTreeNode FindBranchNode(List<BranchTreeNode> nodes, string path)
        {
            foreach (var node in nodes)
            {
                if (node.Path.Equals(path, StringComparison.Ordinal))
                    return node;

                if (path!.StartsWith(node.Path, StringComparison.Ordinal))
                {
                    var founded = FindBranchNode(node.Children, path);
                    if (founded != null)
                        return founded;
                }
            }

            return null;
        }

        private void TryToAddCustomActionsToBranchContextMenu(ContextMenuModel menu, Models.Branch branch)
{
    var actions = GetCustomActions(Models.CustomActionScope.Branch);
    if (actions.Count == 0)
        return;

    menu.Items.Add(new MenuItemModel {
        Header = App.ResText("BranchCM.CustomAction"),
        IconKey = App.MenuIconKey("Icons.Action"),
        IsEnabled = false
    });

    foreach (var action in actions)
    {
        var dup = action;
        menu.Items.Add(new MenuItemModel {
            Header = dup.Name,
            IconKey = App.MenuIconKey("Icons.Action"),
            Command = new RelayCommand(() => { if (CanCreatePopup()) ShowAndStartPopup(new ExecuteCustomAction(this, dup, branch)); })
        });
    }

    menu.Items.Add(new MenuItemModel { Header = "-" });
}


        private bool IsSearchingCommitsByFilePath()
        {
            return _isSearching && _searchCommitFilterType == (int)Models.CommitSearchMethod.ByFile;
        }

        private void CalcWorktreeFilesForSearching()
        {
            if (!IsSearchingCommitsByFilePath())
            {
                _worktreeFiles = null;
                MatchedFilesForSearching = null;
                GC.Collect();
                return;
            }

            Task.Run(() =>
            {
                _worktreeFiles = new Commands.QueryRevisionFileNames(_fullpath, "HEAD").Result();
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (IsSearchingCommitsByFilePath())
                        CalcMatchedFilesForSearching();
                });
            });
        }

        private void CalcMatchedFilesForSearching()
        {
            if (_worktreeFiles == null || _worktreeFiles.Count == 0 || _searchCommitFilter.Length < 3)
            {
                MatchedFilesForSearching = null;
                return;
            }

            var matched = new List<string>();
            foreach (var file in _worktreeFiles)
            {
                if (file.Contains(_searchCommitFilter, StringComparison.OrdinalIgnoreCase) && file.Length != _searchCommitFilter.Length)
                {
                    matched.Add(file);
                    if (matched.Count > 100)
                        break;
                }
            }

            MatchedFilesForSearching = matched;
        }

        private void AutoFetchImpl(object sender)
        {
            try
            {
                if (!_settings.EnableAutoFetch || _isAutoFetching)
                    return;

                var lockFile = Path.Combine(_gitDir, "index.lock");
                if (File.Exists(lockFile))
                    return;

                var now = DateTime.Now;
                var desire = _lastFetchTime.AddMinutes(_settings.AutoFetchInterval);
                if (desire > now)
                    return;

                var remotes = new List<string>();
                lock (_lockRemotes)
                {
                    foreach (var remote in _remotes)
                        remotes.Add(remote.Name);
                }

                Dispatcher.UIThread.Invoke(() => IsAutoFetching = true);
                foreach (var remote in remotes)
                    new Commands.Fetch(_fullpath, remote, false, false) { RaiseError = false }.Exec();
                _lastFetchTime = DateTime.Now;
                Dispatcher.UIThread.Invoke(() => IsAutoFetching = false);
            }
            catch
            {
                // DO nothing, but prevent `System.AggregateException`
            }
        }

        private string _fullpath = string.Empty;
        private string _gitDir = string.Empty;
        private Models.RepositorySettings _settings = null;
        private Models.FilterMode _historiesFilterMode = Models.FilterMode.None;
        private bool _hasAllowedSignersFile = false;

        private Models.Watcher _watcher = null;
        private Histories _histories = null;
        private WorkingCopy _workingCopy = null;
        private StashesPage _stashesPage = null;
        private int _selectedViewIndex = 0;
        private object _selectedView = null;

        private int _localChangesCount = 0;
        private int _stashesCount = 0;

        private bool _isSearching = false;
        private bool _isSearchLoadingVisible = false;
        private int _searchCommitFilterType = (int)Models.CommitSearchMethod.ByMessage;
        private bool _onlySearchCommitsInCurrentBranch = false;
        private string _searchCommitFilter = string.Empty;
        private List<Models.Commit> _searchedCommits = new List<Models.Commit>();
        private Models.Commit _selectedSearchedCommit = null;
        private List<string> _worktreeFiles = null;
        private List<string> _matchedFilesForSearching = null;

        private string _filter = string.Empty;
        private object _lockRemotes = new object();
        private List<Models.Remote> _remotes = new List<Models.Remote>();
        private List<Models.Branch> _branches = new List<Models.Branch>();
        private Models.Branch _currentBranch = null;
        private List<BranchTreeNode> _localBranchTrees = new List<BranchTreeNode>();
        private List<BranchTreeNode> _remoteBranchTrees = new List<BranchTreeNode>();
        private List<Models.Worktree> _worktrees = new List<Models.Worktree>();
        private List<Models.Tag> _tags = new List<Models.Tag>();
        private List<Models.Tag> _visibleTags = new List<Models.Tag>();
        private List<Models.Submodule> _submodules = new List<Models.Submodule>();
        private List<Models.Submodule> _visibleSubmodules = new List<Models.Submodule>();

        private bool _isAutoFetching = false;
        private Timer _autoFetchTimer = null;
        private DateTime _lastFetchTime = DateTime.MinValue;

        private Models.BisectState _bisectState = Models.BisectState.None;
        private bool _isBisectCommandRunning = false;

        private string _navigateToBranchDelayed = string.Empty;
    }

}
