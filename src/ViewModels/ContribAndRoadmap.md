# ViewModel Refactor Roadmap

## Temporary Naming Strategy for Menu Items

To minimize merge conflicts and ease the process of pulling new commits from the original SourceGit codebase, we are temporarily retaining the name `MenuItem` for our ViewModel-side plain data class (POCO) that describes menu actions. This is intentionally close to the original source code, even though it is not a UI control. This approach will be revisited in the future as we move towards a cleaner separation between ViewModel and View.

- **Rationale:** Using the same name (`MenuItem`) as the original Avalonia.Controls class reduces the likelihood of merge conflicts and simplifies automated or manual conflict resolution when syncing with upstream changes.
- **Note:** This is a transitional strategy. Once the codebase is more modular and the ViewModel/View separation matures, we may rename this class to something more semantically appropriate (e.g., `MenuAction`) and move it to a more neutral/shared location.

## Long-term Goal: View/ViewModel Separation

A key goal on the roadmap is to achieve a strict separation between ViewModel and View. ViewModels should not reference or instantiate any UI controls, and Views should construct UI elements based on data and commands exposed by ViewModels. This will:
- Improve testability
- Increase maintainability
- Enable code reuse and platform flexibility

**Current phase:** Incrementally remove Avalonia.Controls dependencies from ViewModels, starting with menu/context menu logic.

---

_Last updated: 2025-05-08_
