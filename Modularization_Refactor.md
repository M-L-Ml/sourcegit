# Refactor: Modularize the Project

## Problem
The current codebase is monolithic, making it difficult to maintain, test, and extend. As the project grows, this structure limits flexibility and increases technical debt.

## Proposal
- Split the codebase into several projects/libraries 

-  1nd proposal:
-  
```  
src/App/sourcegit.csproj
src/Commands/Commands.csproj
src/Converters/Converters.csproj
src/Models/Models.csproj
src/Models_and_Native/Models_and_Native.csproj
src/Utils/Utils.csproj
src/VCS_Access/VCS_Access.csproj
src/Resources/Resources.csproj
src/ViewModels/ViewModels.csproj
src/Views/Views.csproj
src/Tests/Tests.csproj
```
- 2st prposal:
-  Core, UI, Plugins, VCS_Access,.
- Clearly define interfaces and contracts between modules.
- Ensure each module is independently testable and reusable.

## Benefits
- **Improved Testability:** Smaller, focused modules are easier to unit test.
- **Better Maintainability:** Isolated changes reduce risk of regressions.
- **Enhanced Extensibility:** New features (e.g., plugin support, alternate UIs) are easier to implement.
- **Future-Proofing:** Enables support for multiple frontends (desktop, browser, etc.) and VCS integrations.

## Steps
1. Identify logical boundaries in the current codebase.
2. Create separate projects for each major concern.
3. Refactor code to remove cross-project dependencies.
4. Add automated tests for each module.

---

**References:**
- [.NET Modularization Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/modularity/)
