using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.ViewModels
{
    public partial class CommitDetail : ObservableObject
    {
        public int ActivePageIndex
        {
            get => _repo.CommitDetailActivePageIndex;
            set
            {
                if (_repo.CommitDetailActivePageIndex != value)
                {
                    _repo.CommitDetailActivePageIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public Models.Commit Commit
        {
            get => _commit;
            set
            {
                if (SetProperty(ref _commit, value))
                    Refresh();
            }
        }

        public Models.CommitFullMessage FullMessage
        {
            get => _fullMessage;
            private set => SetProperty(ref _fullMessage, value);
        }

        public Models.CommitSignInfo SignInfo
        {
            get => _signInfo;
            private set => SetProperty(ref _signInfo, value);
        }

        public List<Models.CommitLink> WebLinks
        {
            get;
            private set;
        } = [];

        public List<string> Children
        {
            get => _children;
            private set => SetProperty(ref _children, value);
        }

        public List<Models.Change> Changes
        {
            get => _changes;
            set => SetProperty(ref _changes, value);
        }

        public List<Models.Change> VisibleChanges
        {
            get => _visibleChanges;
            set => SetProperty(ref _visibleChanges, value);
        }

        public List<Models.Change> SelectedChanges
        {
            get => _selectedChanges;
            set
            {
                if (SetProperty(ref _selectedChanges, value))
                {
                    if (value == null || value.Count != 1)
                        DiffContext = null;
                    else
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption(_commit, value[0]), _diffContext);
                }
            }
        }

        public DiffContext DiffContext
        {
            get => _diffContext;
            private set => SetProperty(ref _diffContext, value);
        }

        public string SearchChangeFilter
        {
            get => _searchChangeFilter;
            set
            {
                if (SetProperty(ref _searchChangeFilter, value))
                {
                    RefreshVisibleChanges();
                }
            }
        }

        public object ViewRevisionFileContent
        {
            get => _viewRevisionFileContent;
            set => SetProperty(ref _viewRevisionFileContent, value);
        }

        public string RevisionFileSearchFilter
        {
            get => _revisionFileSearchFilter;
            set
            {
                if (SetProperty(ref _revisionFileSearchFilter, value))
                    RefreshRevisionSearchSuggestion();
            }
        }

        public List<string> RevisionFileSearchSuggestion
        {
            get => _revisionFileSearchSuggestion;
            private set => SetProperty(ref _revisionFileSearchSuggestion, value);
        }

        public CommitDetail(Repository repo)
        {
            _repo = repo;
            WebLinks = Models.CommitLink.Get(repo.Remotes);
        }

        public void Cleanup()
        {
            _repo = null;
            _commit = null;
            _changes = null;
            _visibleChanges = null;
            _selectedChanges = null;
            _signInfo = null;
            _searchChangeFilter = null;
            _diffContext = null;
            _viewRevisionFileContent = null;
            _cancellationSource = null;
            _revisionFiles = null;
            _revisionFileSearchSuggestion = null;
        }

        public void NavigateTo(string commitSHA)
        {
            _repo?.NavigateToCommit(commitSHA);
        }

        public List<Models.Decorator> GetRefsContainsThisCommit()
        {
            return new Commands.QueryRefsContainsCommit(_repo.FullPath, _commit.SHA).Result();
        }

        public void ClearSearchChangeFilter()
        {
            SearchChangeFilter = string.Empty;
        }

        public void ClearRevisionFileSearchFilter()
        {
            RevisionFileSearchFilter = string.Empty;
        }

        public void CancelRevisionFileSuggestions()
        {
            RevisionFileSearchSuggestion = null;
        }

        public Models.Commit GetParent(string sha)
        {
            return new Commands.QuerySingleCommit(_repo.FullPath, sha).Result();
        }

        public List<Models.Object> GetRevisionFilesUnderFolder(string parentFolder)
        {
            return new Commands.QueryRevisionObjects(_repo.FullPath, _commit.SHA, parentFolder).Result();
        }

        public void ViewRevisionFile(Models.Object file)
        {
            if (file == null)
            {
                ViewRevisionFileContent = null;
                return;
            }

            switch (file.Type)
            {
                case Models.ObjectType.Blob:
                    Task.Run(() =>
                    {
                        var isBinary = new Commands.IsBinary(_repo.FullPath, _commit.SHA, file.Path).Result();
                        if (isBinary)
                        {
                            var ext = Path.GetExtension(file.Path);
                            if (IMG_EXTS.Contains(ext))
                            {
                                var stream = Commands.QueryFileContent.Run(_repo.FullPath, _commit.SHA, file.Path);
                                var fileSize = stream.Length;
                                var bitmap = fileSize > 0 ? new Bitmap(stream) : null;
                                var imageType = ext!.Substring(1).ToUpper(CultureInfo.CurrentCulture);
                                var image = new Models.RevisionImageFile() { Image = bitmap, FileSize = fileSize, ImageType = imageType };
                                Dispatcher.UIThread.Invoke(() => ViewRevisionFileContent = image);
                            }
                            else
                            {
                                var size = new Commands.QueryFileSize(_repo.FullPath, file.Path, _commit.SHA).Result();
                                var binary = new Models.RevisionBinaryFile() { Size = size };
                                Dispatcher.UIThread.Invoke(() => ViewRevisionFileContent = binary);
                            }

                            return;
                        }

                        var contentStream = Commands.QueryFileContent.Run(_repo.FullPath, _commit.SHA, file.Path);
                        var content = new StreamReader(contentStream).ReadToEnd();
                        var matchLFS = REG_LFS_FORMAT().Match(content);
                        if (matchLFS.Success)
                        {
                            var obj = new Models.RevisionLFSObject() { Object = new Models.LFSObject() };
                            obj.Object.Oid = matchLFS.Groups[1].Value;
                            obj.Object.Size = long.Parse(matchLFS.Groups[2].Value);
                            Dispatcher.UIThread.Invoke(() => ViewRevisionFileContent = obj);
                        }
                        else
                        {
                            var txt = new Models.RevisionTextFile() { FileName = file.Path, Content = content };
                            Dispatcher.UIThread.Invoke(() => ViewRevisionFileContent = txt);
                        }
                    });
                    break;
                case Models.ObjectType.Commit:
                    Task.Run(() =>
                    {
                        var submoduleRoot = Path.Combine(_repo.FullPath, file.Path);
                        var commit = new Commands.QuerySingleCommit(submoduleRoot, file.SHA).Result();
                        if (commit != null)
                        {
                            var body = new Commands.QueryCommitFullMessage(submoduleRoot, file.SHA).Result();
                            var submodule = new Models.RevisionSubmodule()
                            {
                                Commit = commit,
                                FullMessage = new Models.CommitFullMessage { Message = body }
                            };

                            Dispatcher.UIThread.Invoke(() => ViewRevisionFileContent = submodule);
                        }
                        else
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                ViewRevisionFileContent = new Models.RevisionSubmodule()
                                {
                                    Commit = new Models.Commit() { SHA = file.SHA },
                                    FullMessage = null,
                                };
                            });
                        }
                    });
                    break;
                default:
                    ViewRevisionFileContent = null;
                    break;
            }
        }

        public ContextMenuModel CreateChangeContextMenu(Models.Change change)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;

            items.Add(new MenuItemModel
            {
                Header = App.ResText("DiffWithMerger"),
                IconKey = App.MenuIconKey("Icons.OpenWith"),
                Command = new RelayCommand(() =>
                {
                    var toolType = Preferences.Instance.ExternalMergeToolType;
                    var toolPath = Preferences.Instance.ExternalMergeToolPath;
                    var opt = new Models.DiffOption(_commit, change);
                    Task.Run(() => Commands.MergeTool.OpenForDiff(_repo.FullPath, toolType, toolPath, opt));
                })
            });

            var fullPath = Native.OS.GetAbsPath(_repo.FullPath, change.Path);
            items.Add(new MenuItemModel
            {
                Header = App.ResText("RevealFile"),
                IconKey = App.MenuIconKey("Icons.Explore"),
                IsEnabled = File.Exists(fullPath),
                Command = new RelayCommand(() => Native.OS.OpenInFileManager(fullPath, true))
            });

            items.Add(MenuModel.Separator());

            items.Add(new MenuItemModel
            {
                Header = App.ResText("FileHistory"),
                IconKey = App.MenuIconKey("Icons.Histories"),
                Command = new RelayCommand(() => App.ShowWindow(new FileHistories(_repo, change.Path, _commit.SHA), false))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("Blame"),
                IconKey = App.MenuIconKey("Icons.Blame"),
                IsEnabled = change.Index != Models.ChangeState.Deleted,
                Command = new RelayCommand(() => App.ShowWindow(new Blame(_repo.FullPath, change.Path, _commit.SHA), false))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("FileCM.SaveAsPatch"),
                IconKey = App.MenuIconKey("Icons.Diff"),
                Command = new RelayCommand(async () =>
                {
                    var storageProvider = App.GetStorageProvider();
                    if (storageProvider == null)
                        return;
                    var options = new FilePickerSaveOptions
                    {
                        Title = App.Text("FileCM.SaveAsPatch"),
                        DefaultExtension = ".patch",
                        FileTypeChoices = [new FilePickerFileType("Patch File") { Patterns = ["*.patch"] }]
                    };
                    var baseRevision = _commit.Parents.Count == 0 ? "4b825dc642cb6eb9a060e54bf8d69288fbee4904" : _commit.Parents[0];
                    var storageFile = await storageProvider.SaveFilePickerAsync(options);
                    if (storageFile != null)
                    {
                        var saveTo = storageFile.Path.LocalPath;
                        var succ = await Task.Run(() => Commands.SaveChangesAsPatch.ProcessRevisionCompareChanges(_repo.FullPath, [change], baseRevision, _commit.SHA, saveTo));
                        if (succ)
                            App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
                    }
                })
            });

            items.Add(MenuModel.Separator());

            if (!_repo.IsBare)
            {
                items.Add(new MenuItemModel
                {
                    Header = App.ResText("ChangeCM.CheckoutThisRevision"),
                    IconKey = App.MenuIconKey("Icons.File.Checkout"),
                    Command = new AsyncRelayCommand(async () =>
                    {
                        await ResetToThisRevision(change.Path);
                    })
                });

                items.Add(new MenuItemModel
                {
                    Header = App.ResText("ChangeCM.CheckoutFirstParentRevision"),
                    IconKey = App.MenuIconKey("Icons.File.Checkout"),
                    IsEnabled = _commit.Parents.Count > 0,
                    Command = new AsyncRelayCommand(async () =>
                    {
                        await ResetToParentRevision(change);
                    })
                });

                items.Add(MenuModel.Separator());

                TryToAddContextMenuItemsForGitLFS(menu, fullPath, change.Path);
            }

            items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(change.Path))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyFullPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(fullPath))
            });

            return menu;
        }

        public ContextMenuModel CreateRevisionFileContextMenu(Models.Object file)
        {
            var menu = new ContextMenuModel();
            var items = menu.Items;
            var fullPath = Native.OS.GetAbsPath(_repo.FullPath, file.Path);

            items.Add(new MenuItemModel
            {
                Header = App.ResText("RevealFile"),
                IconKey = App.MenuIconKey("Icons.Explore"),
                IsEnabled = File.Exists(fullPath),
                Command = new RelayCommand(() => Native.OS.OpenInFileManager(fullPath, file.Type == Models.ObjectType.Blob))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("OpenWith"),
                IconKey = App.MenuIconKey("Icons.OpenWith"),
                Command = new RelayCommand(() =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(fullPath) ?? "";
                    var fileExt = Path.GetExtension(fullPath) ?? "";
                    var tmpFile = Path.Combine(Path.GetTempPath(), $"{fileName}~{_commit.SHA.Substring(0, 10)}{fileExt}");
                    Commands.SaveRevisionFile.Run(_repo.FullPath, _commit.SHA, file.Path, tmpFile);
                    Native.OS.OpenWithDefaultEditor(tmpFile);
                })
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("SaveAs"),
                IconKey = App.MenuIconKey("Icons.Save"),
                IsEnabled = file.Type == Models.ObjectType.Blob,
                Command = new RelayCommand(async () =>
                {
                    var storageProvider = App.GetStorageProvider();
                    if (storageProvider == null)
                        return;
                    var options = new FolderPickerOpenOptions { AllowMultiple = false };
                    try
                    {
                        var selected = await storageProvider.OpenFolderPickerAsync(options);
                        if (selected.Count == 1)
                        {
                            var saveTo = Path.Combine(selected[0].Path.LocalPath, Path.GetFileName(file.Path));
                            Commands.SaveRevisionFile.Run(_repo.FullPath, _commit.SHA, file.Path, saveTo);
                        }
                    }
                    catch (Exception e)
                    {
                        App.RaiseException(_repo.FullPath, $"Failed to save file: {e.Message}");
                    }
                })
            });

            items.Add(MenuModel.Separator());

            items.Add(new MenuItemModel
            {
                Header = App.ResText("FileHistory"),
                IconKey = App.MenuIconKey("Icons.Histories"),
                Command = new RelayCommand(() => App.ShowWindow(new FileHistories(_repo, file.Path, _commit.SHA), false))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("Blame"),
                IconKey = App.MenuIconKey("Icons.Blame"),
                IsEnabled = file.Type == Models.ObjectType.Blob,
                Command = new RelayCommand(() => App.ShowWindow(new Blame(_repo.FullPath, file.Path, _commit.SHA), false))
            });

            items.Add(MenuModel.Separator());

            if (!_repo.IsBare)
            {
                items.Add(new MenuItemModel
            {
                Header = App.ResText("ChangeCM.CheckoutThisRevision"),
                IconKey = App.MenuIconKey("Icons.File.Checkout"),
              
                Command = new RelayCommand(async () =>
                {
                    await ResetToThisRevision(file.Path);
                })
            });

                var change = _changes.Find(x => x.Path == file.Path) ?? new Models.Change() { Index = Models.ChangeState.None, Path = file.Path };

                items.Add(new MenuItemModel
            {
                Header = App.ResText("ChangeCM.CheckoutFirstParentRevision"),
                IconKey = App.MenuIconKey("Icons.File.Checkout"),
                Command = new AsyncRelayCommand(async () =>
                {

                    await ResetToParentRevision(change);
                })
            });

            items.Add(MenuModel.Separator());

                TryToAddContextMenuItemsForGitLFS(menu, fullPath, file.Path);
            }

            items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(file.Path))
            });

            items.Add(new MenuItemModel
            {
                Header = App.ResText("CopyFullPath"),
                IconKey = App.MenuIconKey("Icons.Copy"),
                Command = new RelayCommand(() => App.CopyText(fullPath))
            });

            return menu;
        }

        private void Refresh()
        {
            _changes = null;
            _revisionFiles = null;

            SignInfo = null;
            ViewRevisionFileContent = null;
            Children = null;
            RevisionFileSearchFilter = string.Empty;
            RevisionFileSearchSuggestion = null;

            if (_commit == null)
                return;

            if (_cancellationSource is { IsCancellationRequested: false })
                _cancellationSource.Cancel();

            _cancellationSource = new CancellationTokenSource();
            var token = _cancellationSource.Token;

            Task.Run(() =>
            {
                var message = new Commands.QueryCommitFullMessage(_repo.FullPath, _commit.SHA).Result();
                var inlines = ParseInlinesInMessage(message);

                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Invoke(() => FullMessage = new Models.CommitFullMessage { Message = message, Inlines = inlines });
            });

            Task.Run(() =>
            {
                var signInfo = new Commands.QueryCommitSignInfo(_repo.FullPath, _commit.SHA, !_repo.HasAllowedSignersFile).Result();
                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Invoke(() => SignInfo = signInfo);
            });

            if (Preferences.Instance.ShowChildren)
            {
                Task.Run(() =>
                {
                    var max = Preferences.Instance.MaxHistoryCommits;
                    var cmd = new Commands.QueryCommitChildren(_repo.FullPath, _commit.SHA, max) { CancellationToken = token };
                    var children = cmd.Result();
                    if (!token.IsCancellationRequested)
                        Dispatcher.UIThread.Post(() => Children = children);
                });
            }

            Task.Run(() =>
            {
                var parent = _commit.Parents.Count == 0 ? "4b825dc642cb6eb9a060e54bf8d69288fbee4904" : _commit.Parents[0];
                var cmd = new Commands.CompareRevisions(_repo.FullPath, parent, _commit.SHA) { CancellationToken = token };
                var changes = cmd.Result();
                var visible = changes;
                if (!string.IsNullOrWhiteSpace(_searchChangeFilter))
                {
                    visible = new List<Models.Change>();
                    foreach (var c in changes)
                    {
                        if (c.Path.Contains(_searchChangeFilter, StringComparison.OrdinalIgnoreCase))
                            visible.Add(c);
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Changes = changes;
                        VisibleChanges = visible;

                        if (visible.Count == 0)
                            SelectedChanges = null;
                    });
                }
            });
        }

        private List<Models.InlineElement> ParseInlinesInMessage(string message)
        {
            var inlines = new List<Models.InlineElement>();
            if (_repo.Settings.IssueTrackerRules is { Count: > 0 } rules)
            {
                foreach (var rule in rules)
                    rule.Matches(inlines, message);
            }

            var urlMatches = REG_URL_FORMAT().Matches(message);
            for (int i = 0; i < urlMatches.Count; i++)
            {
                var match = urlMatches[i];
                if (!match.Success)
                    continue;

                var start = match.Index;
                var len = match.Length;
                var intersect = false;
                foreach (var link in inlines)
                {
                    if (link.Intersect(start, len))
                    {
                        intersect = true;
                        break;
                    }
                }

                if (intersect)
                    continue;

                var url = message.Substring(start, len);
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    inlines.Add(new Models.InlineElement(Models.InlineElementType.Link, start, len, url));
            }

            var shaMatches = REG_SHA_FORMAT().Matches(message);
            for (int i = 0; i < shaMatches.Count; i++)
            {
                var match = shaMatches[i];
                if (!match.Success)
                    continue;

                var start = match.Index;
                var len = match.Length;
                var intersect = false;
                foreach (var link in inlines)
                {
                    if (link.Intersect(start, len))
                    {
                        intersect = true;
                        break;
                    }
                }

                if (intersect)
                    continue;

                var sha = match.Groups[1].Value;
                var isCommitSHA = new Commands.IsCommitSHA(_repo.FullPath, sha).Result();
                if (isCommitSHA)
                    inlines.Add(new Models.InlineElement(Models.InlineElementType.CommitSHA, start, len, sha));
            }

            if (inlines.Count > 0)
                inlines.Sort((l, r) => l.Start - r.Start);

            return inlines;
        }

        private void RefreshVisibleChanges()
        {
            if (_changes == null)
                return;

            if (string.IsNullOrEmpty(_searchChangeFilter))
            {
                VisibleChanges = _changes;
            }
            else
            {
                var visible = new List<Models.Change>();
                foreach (var c in _changes)
                {
                    if (c.Path.Contains(_searchChangeFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(c);
                }

                VisibleChanges = visible;
            }
        }

        private void TryToAddContextMenuItemsForGitLFS(ContextMenuModel menu, string fullPath, string path)
        {
            if (_repo.Remotes.Count == 0 || !File.Exists(fullPath))
                return;

            var lfsEnabled = new Commands.LFS(_repo.FullPath).IsEnabled();
            if (!lfsEnabled)
                return;

            var lfs = new MenuModel
            {
                Header = App.ResText("GitLFS"),
                IconKey = App.MenuIconKey("Icons.LFS")
            };

            // LFS Lock
            var lfsLock = new MenuModel
            {
                Header = App.ResText("GitLFS.Locks.Lock"),
                IconKey = App.MenuIconKey("Icons.Lock"),
            };
            if (_repo.Remotes.Count == 1)
            {
                lfsLock.Items.Add(new MenuItemModel
                {
                    Header = _repo.Remotes[0].Name,
                    Command = new RelayCommand(async () =>
                    {
                        var log = _repo.CreateLog("Lock LFS file");
                        var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Lock(_repo.Remotes[0].Name, path, log));
                        if (succ)
                            App.SendNotification(_repo.FullPath, $"Lock file \"{path}\" successfully!");
                        log.Complete();
                    }),
                    IconKey = App.MenuIconKey("Icons.Lock")
                });
            }
            else
            {
                foreach (var remote in _repo.Remotes)
                {
                    var remoteName = remote.Name;
                    lfsLock.Items.Add(new MenuItemModel
                    {
                        Header = remoteName,
                        Command = new RelayCommand(async () =>
                        {
                            var log = _repo.CreateLog("Lock LFS file");
                            var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Lock(remoteName, path, log));
                            if (succ)
                                App.SendNotification(_repo.FullPath, $"Lock file \"{path}\" successfully!");
                            log.Complete();
                        }),
                        IconKey = App.MenuIconKey("Icons.Lock")
                    });
                }
            }
            lfs.Items.Add(lfsLock);

            // LFS Unlock
            var lfsUnlock = new MenuModel
            {
                Header = App.ResText("GitLFS.Locks.Unlock"),
                IconKey = App.MenuIconKey("Icons.Unlock"),
            };
            if (_repo.Remotes.Count == 1)
            {
                lfsUnlock.Items.Add(new MenuItemModel
                {
                    Header = _repo.Remotes[0].Name,
                    Command = new RelayCommand(async () =>
                    {
                        var log = _repo.CreateLog("Unlock LFS file");
                        var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Unlock(_repo.Remotes[0].Name, path, false, log));
                        if (succ)
                            App.SendNotification(_repo.FullPath, $"Unlock file \"{path}\" successfully!");
                        log.Complete();
                    }),
                    IconKey = App.MenuIconKey("Icons.Unlock")
                });
            }
            else
            {
                foreach (var remote in _repo.Remotes)
                {
                    var remoteName = remote.Name;
                    lfsUnlock.Items.Add(new MenuItemModel
                    {
                        Header = remoteName,
                        Command = new RelayCommand(async () =>
                        {
                            var log = _repo.CreateLog("Unlock LFS file");
                            var succ = await Task.Run(() => new Commands.LFS(_repo.FullPath).Unlock(remoteName, path, false, log));
                            if (succ)
                                App.SendNotification(_repo.FullPath, $"Unlock file \"{path}\" successfully!");
                            log.Complete();
                        }),
                        IconKey = App.MenuIconKey("Icons.Unlock")
                    });
                }
            }
            lfs.Items.Add(lfsUnlock);

            menu.Items.Add(lfs);
            menu.Items.Add( MenuModel.Separator());
        }

        private void RefreshRevisionSearchSuggestion()
        {
            if (!string.IsNullOrEmpty(_revisionFileSearchFilter))
            {
                if (_revisionFiles == null)
                {
                    var sha = Commit.SHA;

                    Task.Run(() =>
                    {
                        var files = new Commands.QueryRevisionFileNames(_repo.FullPath, sha).Result();
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (sha == Commit.SHA)
                            {
                                _revisionFiles = files;
                                if (!string.IsNullOrEmpty(_revisionFileSearchFilter))
                                    CalcRevisionFileSearchSuggestion();
                            }
                        });
                    });
                }
                else
                {
                    CalcRevisionFileSearchSuggestion();
                }
            }
            else
            {
                RevisionFileSearchSuggestion = null;
                GC.Collect();
            }
        }

        private void CalcRevisionFileSearchSuggestion()
        {
            var suggestion = new List<string>();
            foreach (var file in _revisionFiles)
            {
                if (file.Contains(_revisionFileSearchFilter, StringComparison.OrdinalIgnoreCase) &&
                    file.Length != _revisionFileSearchFilter.Length)
                    suggestion.Add(file);

                if (suggestion.Count >= 100)
                    break;
            }

            RevisionFileSearchSuggestion = suggestion;
        }

        private Task ResetToThisRevision(string path)
        {
            var log = _repo.CreateLog($"Reset File to '{_commit.SHA}'");

            return Task.Run(() =>
            {
                new Commands.Checkout(_repo.FullPath).Use(log).FileWithRevision(path, $"{_commit.SHA}");
                log.Complete();
            });
        }

        private Task ResetToParentRevision(Models.Change change)
        {
            var log = _repo.CreateLog($"Reset File to '{_commit.SHA}~1'");

            return Task.Run(() =>
            {
                if (change.Index == Models.ChangeState.Renamed)
                    new Commands.Checkout(_repo.FullPath).Use(log).FileWithRevision(change.OriginalPath, $"{_commit.SHA}~1");

                new Commands.Checkout(_repo.FullPath).Use(log).FileWithRevision(change.Path, $"{_commit.SHA}~1");
                log.Complete();
            });
        }

        [GeneratedRegex(@"\b(https?://|ftp://)[\w\d\._/\-~%@()+:?&=#!]*[\w\d/]")]
        private static partial Regex REG_URL_FORMAT();

        [GeneratedRegex(@"\b([0-9a-fA-F]{6,40})\b")]
        private static partial Regex REG_SHA_FORMAT();

        [GeneratedRegex(@"^version https://git-lfs.github.com/spec/v\d+\r?\noid sha256:([0-9a-f]+)\r?\nsize (\d+)[\r\n]*$")]
        private static partial Regex REG_LFS_FORMAT();

        private static readonly HashSet<string> IMG_EXTS = new HashSet<string>()
        {
            ".ico", ".bmp", ".jpg", ".png", ".jpeg", ".webp"
        };

        private Repository _repo = null;
        private Models.Commit _commit = null;
        private Models.CommitFullMessage _fullMessage = null;
        private Models.CommitSignInfo _signInfo = null;
        private List<string> _children = null;
        private List<Models.Change> _changes = null;
        private List<Models.Change> _visibleChanges = null;
        private List<Models.Change> _selectedChanges = null;
        private string _searchChangeFilter = string.Empty;
        private DiffContext _diffContext = null;
        private object _viewRevisionFileContent = null;
        private CancellationTokenSource _cancellationSource = null;
        private List<string> _revisionFiles = null;
        private string _revisionFileSearchFilter = string.Empty;
        private List<string> _revisionFileSearchSuggestion = null;
    }
}
