namespace Sausa
{
    /// <summary>
    /// Interface for initializing and configuring application components
    /// </summary>
    public interface IApplicationSetup
    {
        /// <summary>
        /// Configures the application builder
        /// </summary>
        /// <param name="builder">Application builder object</param>
        void SetupApp(object builder);

        /// <summary>
        /// Configures a window with platform-specific settings
        /// </summary>
        /// <param name="window">Window to configure</param>
        void SetupWindow(object window);
    }
}
