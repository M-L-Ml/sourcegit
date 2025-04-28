# Issue: ViewModel Instantiation of View Controls Violates MVVM in Avalonia

## Problem Statement

In several places within the codebase (see the list below), ViewModels directly instantiate or reference View controls (e.g., `new Views.NameHighlightedTextBlock(...)`). This practice violates the MVVM (Model-View-ViewModel) pattern, which is fundamental to Avalonia and similar UI frameworks. MVVM dictates that ViewModels should not know about or manipulate View/UI elements directly.

## Why This is a Problem

- **Breaks Separation of Concerns:** ViewModels should only expose data and commands. UI logic and control instantiation must reside in the View layer.
- **Reduces Testability:** ViewModels that depend on UI elements are difficult to unit test.
- **Hinders Maintainability:** Mixing UI and logic makes the codebase harder to refactor, extend, or debug.
- **Limits Reusability:** ViewModels coupled to specific controls cannot be reused in other contexts or platforms.

## Official Guidance (Avalonia)

- [Avalonia Documentation: MVVM](https://docs.avaloniaui.net/docs/concepts/mvvm)
- [Microsoft Docs: MVVM pattern](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-8.0#the-model-view-viewmodel-pattern) (applies to Avalonia as well)

**Key Principle:**
> "The ViewModel should not directly reference any View or UI elements. All UI logic should be handled in the View, with the ViewModel exposing only data and commands."

## Instances of the Violation

The following ViewModel files instantiate or reference View controls:

### src/ViewModels/DeleteBranch.cs
- `DeleteTrackingRemoteTip = new Views.NameHighlightedTextBlock(...)`

### src/ViewModels/WorkingCopy.cs
- `App.OpenDialog(new Views.AssumeUnchangedManager())` (line 326)
- `useTheirs.Header = new Views.NameHighlightedTextBlock(...)` (lines 631, 636, 641, 646, 990, 995, 1000, 1005)
- `useMine.Header = new Views.NameHighlightedTextBlock(...)` (lines 632, 637, 642, 647, 991, 996, 1001, 1006)
- `var window = new Views.FileHistories() { DataContext = new FileHistories(...) }` (lines 729, 1196)
- `var dialog = new Views.AIAssistant(...)` (lines 1096, 1111, 1493, 1506)
- `item.Header = new Views.NameHighlightedTextBlock(...)` (line 1409)
- `gitTemplateItem.Header = new Views.NameHighlightedTextBlock(...)` (line 1431)
- `App.OpenDialog(new Views.ConfirmCommit())` (line 1708)
- `App.OpenDialog(new Views.ConfirmEmptyCommit())` (line 1723)

### src/ViewModels/Repository.cs
- `var dialog = new Views.LFSLocks() { DataContext = new LFSLocks(...) }` (lines 1408, 1422)
- `dateOrder.SetValue(Views.MenuItemExtension.CommandProperty, ...)` (line 1520)
- `topoOrder.SetValue(Views.MenuItemExtension.CommandProperty, ...)` (line 1536)
- `push.Header = new Views.NameHighlightedTextBlock(...)` (line 1566)
- `fastForward.Header = new Views.NameHighlightedTextBlock(...)` (lines 1597, 1650)
- `pull.Header = new Views.NameHighlightedTextBlock(...)` (line 1613)
- `checkout.Header = new Views.NameHighlightedTextBlock(...)` (lines 1634, 1939)
- `fetchInto.Header = new Views.NameHighlightedTextBlock(...)` (line 1661)
- `merge.Header = new Views.NameHighlightedTextBlock(...)` (lines 1681, 1962)
- `rebase.Header = new Views.NameHighlightedTextBlock(...)` (lines 1691, 1972)
- `App.OpenDialog(new Views.BranchCompare())` (lines 1709, 1992)
- `finish.Header = new Views.NameHighlightedTextBlock(...)` (line 1743)
- `rename.Header = new Views.NameHighlightedTextBlock(...)` (line 1757)
- `delete.Header = new Views.NameHighlightedTextBlock(...)` (lines 1767, 2020)
- `pull.Header = new Views.NameHighlightedTextBlock(...)` (line 1952)
- `pushTag.Header = new Views.NameHighlightedTextBlock(...)` (line 2094)

(*Note: This is a partial list. More instances may exist; see search results for `new Views.` in ViewModels.*)

## Suggested Refactoring

- Move all UI control instantiation to the View layer (e.g., XAML or code-behind).
- Use DataTemplates, DataTriggers, or Converters to bind ViewModel data to Views.
- Expose only pure data and commands from the ViewModel.
- If custom visualizations are needed, expose data in a format the View can bind to and render appropriately.

## References
- [Avalonia MVVM Best Practices](https://docs.avaloniaui.net/docs/concepts/mvvm)
- [Microsoft Docs: MVVM pattern](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-8.0#the-model-view-viewmodel-pattern)

---

**Please consider prioritizing this refactor to ensure long-term maintainability and testability of the codebase.**
