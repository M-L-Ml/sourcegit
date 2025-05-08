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

## 3rd Issue: Static Singleton Pattern in ViewModels Violates MVVM and DI Principles

### Problem: Singleton ViewModels with Static Instance Properties

In the codebase, several ViewModels (most notably `Preferences`) use a static singleton pattern:

```csharp
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
```

This pattern, while convenient for global access, violates MVVM principles and modern dependency injection practices.

### Why This is Problematic

1. **Violates Single Responsibility Principle**: ViewModels should focus on providing data and commands to Views, not managing their own lifecycle.

2. **Tight Coupling**: Other components directly reference `Preferences.Instance`, creating tight coupling throughout the codebase.

3. **Difficult Testing**: Singletons are notoriously difficult to mock or replace in unit tests.

4. **Hidden Dependencies**: Dependencies are hidden rather than explicitly declared, making code harder to understand and maintain.

5. **Initialization Order Issues**: Singletons can cause subtle bugs related to initialization order, especially in complex applications.

### Best Practice Solution: Dependency Injection

The CommunityToolkit.MVVM package works well with Microsoft's dependency injection framework. The recommended approach is:

1. **Register ViewModels in a DI Container**:
   ```csharp
   // In App.xaml.cs
   private static IServiceProvider ConfigureServices()
   {
       var services = new ServiceCollection();
       
       // Register services
       services.AddSingleton<IFilesService, FilesService>();
       
       // Register ViewModels
       services.AddSingleton<ViewModels.Preferences>();
       services.AddTransient<ViewModels.WorkingCopy>();
       
       return services.BuildServiceProvider();
   }
   ```

2. **Inject Dependencies into ViewModels**:
   ```csharp
   public class Preferences : ObservableObject
   {
       private readonly IFilesService _filesService;
       
       public Preferences(IFilesService filesService)
       {
           _filesService = filesService;
           // Initialize properties and commands
       }
       
       // Rest of the class...
   }
   ```

3. **Resolve ViewModels in Views**:
   ```csharp
   public partial class PreferencesView : UserControl
   {
       public PreferencesView()
       {
           InitializeComponent();
           DataContext = App.Current.Services.GetService<ViewModels.Preferences>();
       }
   }
   ```

### Migration Strategy

This is a significant architectural change that should be approached carefully:

1. **Start with New Components**: Apply DI to new ViewModels and Views first.

2. **Gradual Refactoring**: Refactor existing components one at a time, starting with those that have fewer dependencies.

3. **Hybrid Approach During Transition**: Temporarily maintain both patterns if needed, gradually phasing out the singleton pattern.

4. **Update References**: Replace all direct references to `Preferences.Instance` with injected instances.

### References

- [Microsoft Docs: Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [CommunityToolkit.MVVM IoC Documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/ioc)

---

**Note**: This refactoring is not urgent but should be considered for future architectural improvements to enhance maintainability, testability, and adherence to MVVM principles.

## 4th Issue: Direct Static ViewModel Instance Access

### Problem Description

Some View classes directly access static ViewModel instances instead of using proper DataContext binding. This creates tight coupling between the View and a specific ViewModel instance, violating proper MVVM principles.

### Specific Examples

From `src/Views/Preferences.axaml.cs`:

```csharp
// Direct access to static ViewModel instance
public partial class Preferences : ChromelessWindow
{
    private void OnAddOpenAIService(object sender, RoutedEventArgs e)
    {
        var service = new Models.OpenAIService() { Name = "Unnamed Service" };
        ViewModels.Preferences.Instance.OpenAIServices.Add(service);
        ViewModels.Preferences.Instance.SelectedOpenAIService = service;
    }
}
```

### Why This Matters

1. **Tight Coupling**: The View is tightly coupled to a specific ViewModel implementation
2. **Testability**: Makes it difficult to test Views with mock ViewModels
3. **Flexibility**: Prevents reusing Views with different ViewModels
4. **Dependency Injection**: Blocks proper dependency injection patterns

### Current Status

In progress. Seems fixed in Preferences.axaml.cs. Refactoring these views to use DataContext properly.

### Suggested Approach

1. Access the ViewModel through DataContext:

```csharp
// Use a property to access the ViewModel through DataContext
public partial class Preferences : ChromelessWindow
{
    // Properly access ViewModel through DataContext
    public ViewModels.Preferences ViewModel => (ViewModels.Preferences)DataContext;
    
    private void OnAddOpenAIService(object sender, RoutedEventArgs e)
    {
        // Use the ViewModel property instead of static instance
        var service = new Models.OpenAIService() { Name = "Unnamed Service" };
        ViewModel.OpenAIServices.Add(service);
        ViewModel.SelectedOpenAIService = service;
    }
}
```

2. Use commands in the ViewModel for event handling
3. Set up bindings in XAML to use the DataContext directly

## Official MVVM Guidelines

1. Views should only handle UI concerns
2. ViewModels should handle business logic and data preparation
3. Views should communicate with ViewModels through data binding and commands
4. Views should access ViewModels only through DataContext, not through static instances
5. ViewModel properties should be used for binding, not direct property access on Views

## References

- [Microsoft Docs: MVVM Pattern](https://docs.microsoft.com/en-us/windows/uwp/data-binding/data-binding-and-mvvm)
- [MVVM Light Toolkit](http://www.mvvmlight.net/)
- [Prism Library](https://github.com/PrismLibrary/Prism)
