# MVVM Violation Issue

## 1st Issue: ViewModel Instantiation of View Controls Violates MVVM in Avalonia

### Problem Statement

In several places within the codebase (see the list below), ViewModels directly instantiate or reference View controls (e.g., `new Views.NameHighlightedTextBlock(...)`). This practice violates the MVVM (Model-View-ViewModel) pattern, which is fundamental to Avalonia and similar UI frameworks. MVVM dictates that ViewModels should not know about or manipulate View/UI elements directly.

### Why This is a Problem

- **Breaks Separation of Concerns:** ViewModels should only expose data and commands. UI logic and control instantiation must reside in the View layer.
- **Reduces Testability:** ViewModels that depend on UI elements are difficult to unit test.
- **Hinders Maintainability:** Mixing UI and logic makes the codebase harder to refactor, extend, or debug.
- **Limits Reusability:** ViewModels coupled to specific controls cannot be reused in other contexts or platforms.

**Key Principle:**
> "The ViewModel should not directly reference any View or UI elements. All UI logic should be handled in the View, with the ViewModel exposing only data and commands."

### Status

Mostly solved. A few instances may still be present; search for `new Views.` in the ViewModels.* code to find them. Since ViewModels are in a separate project and will not have references to View-related libraries, it will not be possible to instantiate View controls from ViewModels directly.

### Suggested Refactoring

- Move all UI control instantiation to the View layer (e.g., XAML or code-behind).
- Use DataTemplates, DataTriggers, or Converters to bind ViewModel data to Views.
- Expose only pure data and commands from the ViewModel.
- If custom visualizations are needed, expose data in a format the View can bind to and render appropriately.

## Official Guidance (Avalonia)

- [Avalonia Documentation: MVVM](https://docs.avaloniaui.net/docs/concepts/mvvm)
- [Microsoft Docs: MVVM pattern](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-8.0#the-model-view-viewmodel-pattern) (applies to Avalonia as well)

### References

- [Avalonia MVVM Best Practices](https://docs.avaloniaui.net/docs/concepts/mvvm)
- [Microsoft Docs: MVVM pattern](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/?view=netdesktop-8.0#the-model-view-viewmodel-pattern)

---

**Please consider prioritizing this refactor to ensure long-term maintainability and testability of the codebase.**

## 2nd Issue: View Classes with ViewModel Responsibilities Violate MVVM

### Overview and Issue Description

Several View classes in the codebase contain properties, logic, and responsibilities that should be in their corresponding ViewModels. This is another form of MVVM violation where the separation of concerns between View and ViewModel is blurred. A prime example is the `Preferences` view class.

### Identified Issues in Preferences.axaml.cs

1. **Properties defined in View rather than ViewModel:**
   - Basic data properties like `DefaultUser`, `DefaultEmail`, `CRLFMode`, etc. belong in the ViewModel
   - `AvaloniaProperty` registrations for properties like `GitVersionProperty`, `ShowGitVersionWarningProperty`, etc.

2. **Business logic in View:**
   - Constructor directly interacts with `Commands.Config` to read Git configuration
   - `OnPropertyChanged` and `OnClosing` contain logic that manipulates application state
   - `UpdateGitVersion` method contains logic that should be in ViewModel

3. **Direct interaction with Models and Commands:**
   - Direct instantiation of `Commands.Config` objects
   - Direct usage of Model classes like `Models.CRLFMode`, `Models.GPGFormat`, etc.

4. **Event handlers contain business logic:**
   - Methods like `SelectThemeOverrideFile`, `SelectGitExecutable`, etc. contain logic that should be delegated to the ViewModel

### Impact of These Violations

- **Tight Coupling:** Views are tightly coupled to specific Models and Commands, making them difficult to test or reuse
- **Code Duplication:** Business logic is duplicated across Views instead of being centralized in ViewModels
- **Testability Issues:** Views with logic are harder to test than clean ViewModels
- **Maintainability Issues:** Changes to the data model require changes in multiple places

### Suggested Refactoring Plan

1. **Move Properties to ViewModel:**
   - Migrate all data properties from the View to the corresponding ViewModel
   - Convert AvaloniaProperty registrations to standard ViewModel properties

2. **Create Commands in ViewModel:**
   - Replace event handlers in the View with Command properties in the ViewModel
   - Implement ICommand interface for all actions that can be triggered from the UI

3. **Move Business Logic to ViewModel:**
   - Extract all business logic from the View to the ViewModel
   - Make the View simply bind to ViewModel properties and commands

4. **Proper Data Binding in XAML:**
   - Use proper data binding in XAML for all UI elements
   - Avoid code-behind for setting or manipulating data

5. **Add Code Comments:**
   - Add comments in the refactored code explaining the MVVM pattern implementation
   - Document any complex binding scenarios or command implementations

### Implementation Details for Preferences View

1. Move properties like `DefaultUser`, `DefaultEmail`, etc. to `ViewModels.Preferences`
2. Convert `GitVersionProperty`, `ShowGitVersionWarningProperty`, etc. to standard properties in the ViewModel
3. Create commands in the ViewModel for actions like selecting files, adding services, etc.
4. Move the Git configuration logic from the View constructor to the ViewModel
5. Refactor event handlers to use commands from the ViewModel
6. Update XAML bindings to reference the new ViewModel properties and commands

### Additional Resources

- [Avalonia MVVM Documentation](https://docs.avaloniaui.net/docs/concepts/mvvm)
- [CommunityToolkit.Mvvm Library](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) (already used in the project)

**This refactoring will significantly improve the maintainability, testability, and adherence to MVVM principles in the codebase.**
