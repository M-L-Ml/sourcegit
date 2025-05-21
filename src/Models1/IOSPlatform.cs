

namespace Sausa
{
    /// <summary>
    /// Primary platform abstraction interface that aggregates all OS-specific operations.
    /// </summary>
    public interface IOSPlatform : IApplicationSetup, IFileOpener, IExternalTools, IProcessLauncher
    {
        // This interface serves as a facade for all platform-specific operations
    }
}
