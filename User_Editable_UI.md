# Feature: User-Editable UI (AXAML)

## Problem
Users currently cannot easily modify or customize the UI. Enabling user-editable UI (e.g., via AXAML or similar) would greatly increase flexibility and user empowerment.

## Practical Considerations

While editing AXAML (Avalonia XAML) files can be challenging for non-technical users, a thoughtfully designed customization system can make the process safe, intuitive, and rewarding:

- **Safety:** With proper validation and sandboxing, user edits can be protected from breaking the application. The system can automatically check for errors and revert or warn about invalid changes, ensuring stability.
- **Community Sharing:** Users can benefit from AXAML templates and layouts shared by experienced community members or professionals. Trusted contributors could provide signed AXAML files or plugins, and the app could support easy switching between them. Prism's robust plugin architecture can facilitate secure distribution and management of these shared resources.
- **Intuitive Customization:** Even without deep AXAML knowledge, users can personalize aspects of the UI—such as colors, labels, or layouts—by editing clearly commented sections. Well-documented customization points and helper tools can make small tweaks accessible to everyone.
- **Not Critical, But Valuable:** While full AXAML editing is not an immediate priority, offering limited, safe, and well-documented customization empowers users and encourages engagement. Even simple changes can make the app feel more personal and adaptable.

AXAML's declarative nature makes it relatively approachable for making targeted adjustments. By focusing on well-commented and isolated customization areas, we can ensure the UI remains robust for beginners while offering flexibility for power users.

## Theming, Color Customization, and Plugin Support

Changing colors, fonts, and other visual properties is a common and intuitive way to let users personalize their experience. This can be implemented as:

- **Themes:** Provide several pre-defined or user-editable AXAML theme files that control colors, fonts, and styles. Users can switch between themes or tweak them to their liking.
- **Theme Plugins:** For more advanced users or scenarios, allow themes to be loaded as plugins, enabling even more flexibility and community sharing.
- **Maybe Prism Framework Advantage:** Migrating to the Prism framework is maybe beneficial here. Prism's modularity and region management make it easier to implement theme/plugin systems and support runtime switching or extension of UI elements. This also lays the groundwork for broader plugin support in the future, making the application more extensible and adaptable. Although, from what I understand, dynamic axaml switching is supported by Avalonia, right out the box. So Prism seems isn't so necessary.

By enabling both simple theme switching and more advanced plugin-based customization, we can cater to a wide range of user needs—from basic personalization to deep UI extension.

## Proposal
- Integrate support for user-editable UI definitions (AXAML (Avalonia XAML), or their alternative UI schemas).
- Provide a UI hints or documentation for setting up user edited AXAML layouts.
- Implement hot-reloading for UI changes while enforcing safety constraints to prevent execution of unsafe code.
- Include clear disclaimers in the UI about limitations of responsibility for user-modified interfaces.

## Benefits
- **User Empowerment:** Users can tailor the UI to their workflows.
- **Rapid Prototyping:** Developers and users can experiment with UI changes quickly.
- **Community Sharing:** Users can share custom layouts and themes.

## Steps
1. Research AXAML (Avalonia XAML) runtime editing and loading in Avalonia/Prism.
2. Implement user-editable AXAML loading and validation.
3. Provide documentation and/or an in-app helpers for editing AXAML.

---

**References:**
- [Avalonia AXAML Introduction](https://docs.avaloniaui.net/docs/basics/user-interface/introduction-to-xaml)
- [Avalonia XAML Runtime Loading](https://docs.avaloniaui.net/docs/guides/xaml/xaml-loader)
