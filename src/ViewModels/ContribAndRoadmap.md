
# Contributing and Roadmap

# Code of Conduct


Embrace disscusing in Comments
Comments should explain unobvious logic, and reasons behind design choices. Even if  those reasons are lazyness or lack of knowledge or time.
So some common arguments against comments are :
- Code should be self-explanatory instead
 -- It's hard to do it in a way that it's self-explanatory. And no one like to discuss how to make e.g. method names self-explanatory and up-to-date. So there are tradeoffs always o make it self-explanatory. So all that tradeoffs should be alleviated by comments.
- Comments are outdated
-- Think about method names. They can become outdated  to. Or they can constrain your thinking hindering good changes. So comments should be treated seariously and updated and changed accordingly
- Comments are a code smell
-- That's right, it better to smell bad traits of code than to face their consequences in runtime. And yes: they sometimes should be deleted. And yes: it's an Art to write it in a way they are not smelly, i.e. not misleading or pulling too much attention, and at the same time concise, usefull, and not over-explaining. And this art is that same important universal skill of communication!
- Disgusting work with code with such a small
-- We should work on our communication issues with text. It's considered cringe and waste of time to disscuss our communiction issues. But it's in the world which fails communication in a miserable way.



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
