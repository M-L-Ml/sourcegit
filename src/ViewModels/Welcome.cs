using System;
using System.Collections.Generic;
using System.IO;

using Avalonia.Collections;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.ViewModels
{
    public class Welcome : ObservableObject
    {
        public static Welcome Instance => _instance;

        public AvaloniaList<RepositoryNode> Rows
        {
            get;
            private set;
        } = [];

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                    Refresh();
            }
        }

        public Welcome()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (string.IsNullOrWhiteSpace(_searchFilter))
            {
                foreach (var node in Preferences.Instance.RepositoryNodes)
                    ResetVisibility(node);
            }
            else
            {
                foreach (var node in Preferences.Instance.RepositoryNodes)
                    SetVisibilityBySearch(node);
            }

            var rows = new List<RepositoryNode>();
            MakeTreeRows(rows, Preferences.Instance.RepositoryNodes);
            Rows.Clear();
            Rows.AddRange(rows);
        }

        public void ToggleNodeIsExpanded(RepositoryNode node)
        {
            node.IsExpanded = !node.IsExpanded;

            var depth = node.Depth;
            var idx = Rows.IndexOf(node);
            if (idx == -1)
                return;

            if (node.IsExpanded)
            {
                var subrows = new List<RepositoryNode>();
                MakeTreeRows(subrows, node.SubNodes, depth + 1);
                Rows.InsertRange(idx + 1, subrows);
            }
            else
            {
                var removeCount = 0;
                for (int i = idx + 1; i < Rows.Count; i++)
                {
                    var row = Rows[i];
                    if (row.Depth <= depth)
                        break;

                    removeCount++;
                }
                Rows.RemoveRange(idx + 1, removeCount);
            }
        }

        public void OpenOrInitRepository(string path, RepositoryNode parent, bool bMoveExistedNode)
        {
            if (!Directory.Exists(path))
            {
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                else
                    return;
            }

            var isBare = new Commands.IsBareRepository(path).Result();
            var repoRoot = path;
            if (!isBare)
            {
                var test = new Commands.QueryRepositoryRootPath(path).ReadToEnd();
                if (!test.IsSuccess || string.IsNullOrEmpty(test.StdOut))
                {
                    InitRepository(path, parent, test.StdErr);
                    return;
                }

                repoRoot = test.StdOut.Trim();
            }

            var node = Preferences.Instance.FindOrAddNodeByRepositoryPath(repoRoot, parent, bMoveExistedNode);
            Refresh();

            var launcher = App.GetLauncer();
            launcher?.OpenRepositoryInTab(node, launcher.ActivePage);
        }

        public void InitRepository(string path, RepositoryNode parent, string reason)
        {
            if (!Preferences.Instance.IsGitConfigured())
            {
                App.RaiseException(string.Empty, App.Text("NotConfigured"));
                return;
            }

            var activePage = App.GetLauncer().ActivePage;
            if (activePage != null && activePage.CanCreatePopup())
                activePage.Popup = new Init(activePage.Node.Id, path, parent, reason);
        }

        public void Clone()
        {
            if (!Preferences.Instance.IsGitConfigured())
            {
                App.RaiseException(string.Empty, App.Text("NotConfigured"));
                return;
            }

            var activePage = App.GetLauncer().ActivePage;
            if (activePage != null && activePage.CanCreatePopup())
                activePage.Popup = new Clone(activePage.Node.Id);
        }

        public void OpenTerminal()
        {
            if (!Preferences.Instance.IsGitConfigured())
                App.RaiseException(string.Empty, App.Text("NotConfigured"));
            else
                Native.OS.OpenTerminal(null);
        }

        public void ScanDefaultCloneDir()
        {
            var defaultCloneDir = Preferences.Instance.GitDefaultCloneDir;
            if (string.IsNullOrEmpty(defaultCloneDir))
            {
                App.RaiseException(string.Empty, "The default clone directory hasn't been configured!");
                return;
            }

            if (!Directory.Exists(defaultCloneDir))
            {
                App.RaiseException(string.Empty, $"The default clone directory '{defaultCloneDir}' does not exist!");
                return;
            }

            var activePage = App.GetLauncer().ActivePage;
            if (activePage != null && activePage.CanCreatePopup())
                activePage.StartPopup(new ScanRepositories(defaultCloneDir));
        }

        public void ClearSearchFilter()
        {
            SearchFilter = string.Empty;
        }

        public void AddRootNode()
        {
            var activePage = App.GetLauncer().ActivePage;
            if (activePage != null && activePage.CanCreatePopup())
                activePage.Popup = new CreateGroup(null);
        }

        public void MoveNode(RepositoryNode from, RepositoryNode to)
        {
            Preferences.Instance.MoveNode(from, to, true);
            Refresh();
        }

        public ContextMenuModel CreateContextMenuModel(RepositoryNode node)
        {
            var menu = new ContextMenuModel();

            if (!node.IsRepository && node.SubNodes.Count > 0)
            {
                menu.Items.Add(new MenuItemModel
                {
                    Header = App.ResText("Welcome.OpenAllInNode"),
                    IconKey = App.MenuIconKey("Icons.Folder.Open"),
                    Command = new RelayCommand(() => OpenAllInNode(App.GetLauncer(), node))
                });
                menu.Items.Add(MenuModel.Separator());
            }

            if (node.IsRepository)
            {
                menu.Items.Add(new MenuItemModel
                {
                    Header = App.ResText("Welcome.OpenOrInit"),
                    IconKey = App.MenuIconKey("Icons.Folder.Open"),
                    Command = new RelayCommand(() => App.GetLauncer().OpenRepositoryInTab(node, null))
                });
                menu.Items.Add(MenuModel.Separator());
                menu.Items.Add(new MenuItemModel
                {
                    Header = App.ResText("Repository.Explore"),
                    IconKey = App.MenuIconKey("Icons.Explore"),
                    Command = new RelayCommand(() => node.OpenInFileManager())
                });
                menu.Items.Add(new MenuItemModel
                {
                    Header = App.ResText("Repository.Terminal"),
                    IconKey = App.MenuIconKey("Icons.Terminal"),
                    Command = new RelayCommand(() => node.OpenTerminal())
                });
                menu.Items.Add(MenuModel.Separator());
            }
            else
            {
                menu.Items.Add(new MenuItemModel
                {
                    Header = App.ResText("Welcome.AddSubFolder"),
                    IconKey = App.MenuIconKey("Icons.Folder.Add"),
                    Command = new RelayCommand(() => node.AddSubFolder())
                });
            }

            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("Welcome.Edit"),
                IconKey = App.MenuIconKey("Icons.Edit"),
                Command = new RelayCommand(() => node.Edit())
            });
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("Welcome.Move"),
                IconKey = App.MenuIconKey("Icons.MoveToAnotherGroup"),
                Command = new RelayCommand(() =>
                {
                    var activePage = App.GetLauncer().ActivePage;
                    if (activePage != null && activePage.CanCreatePopup())
                        activePage.Popup = new MoveRepositoryNode(node);
                })
            });
            menu.Items.Add(MenuModel.Separator());
            menu.Items.Add(new MenuItemModel
            {
                Header = App.ResText("Welcome.Delete"),
                IconKey = App.MenuIconKey("Icons.Clear"),
                Command = new RelayCommand(() => node.Delete())
            });

            return menu;
        }

        private void ResetVisibility(RepositoryNode node)
        {
            node.IsVisible = true;
            foreach (var subNode in node.SubNodes)
                ResetVisibility(subNode);
        }

        private void SetVisibilityBySearch(RepositoryNode node)
        {
            if (!node.IsRepository)
            {
                if (node.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    node.IsVisible = true;
                    foreach (var subNode in node.SubNodes)
                        ResetVisibility(subNode);
                }
                else
                {
                    bool hasVisibleSubNode = false;
                    foreach (var subNode in node.SubNodes)
                    {
                        SetVisibilityBySearch(subNode);
                        hasVisibleSubNode |= subNode.IsVisible;
                    }
                    node.IsVisible = hasVisibleSubNode;
                }
            }
            else
            {
                node.IsVisible = node.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    node.Id.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void MakeTreeRows(List<RepositoryNode> rows, List<RepositoryNode> nodes, int depth = 0)
        {
            foreach (var node in nodes)
            {
                if (!node.IsVisible)
                    continue;

                node.Depth = depth;
                rows.Add(node);

                if (node.IsRepository || !node.IsExpanded)
                    continue;

                MakeTreeRows(rows, node.SubNodes, depth + 1);
            }
        }

        private void OpenAllInNode(Launcher launcher, RepositoryNode node)
        {
            foreach (var subNode in node.SubNodes)
            {
                if (subNode.IsRepository)
                    launcher.OpenRepositoryInTab(subNode, null);
                else if (subNode.SubNodes.Count > 0)
                    OpenAllInNode(launcher, subNode);
            }
        }

        private static Welcome _instance = new Welcome();
        private string _searchFilter = string.Empty;
    }
}
