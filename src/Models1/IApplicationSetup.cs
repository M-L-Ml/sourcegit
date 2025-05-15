using Avalonia;
using Avalonia.Controls;

namespace SourceGit.Models
{
    /// <summary>
    /// Interface for initializing and configuring application components
    /// </summary>
    public interface IApplicationSetup
    {
        /// <summary>
        /// Configures the Avalonia application builder
        /// </summary>
        /// <param name="builder">Avalonia application builder</param>
        void SetupApp(AppBuilder builder);

        /// <summary>
        /// Configures a window with platform-specific settings
        /// </summary>
        /// <param name="window">Window to configure</param>
        void SetupWindow(Window window);
    }
}
