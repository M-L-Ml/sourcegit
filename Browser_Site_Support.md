# Feature: Browser/Site Support

## Problem
Currently, the application only runs as a desktop app. Supporting browser-based/site deployment would increase reach and flexibility.

## Proposal
- Refactor the architecture to support running as a web app (e.g., Avalonia WebAssembly, Blazor, or similar technologies).
- Abstract platform-specific code to allow reuse of core logic.
- Ensure UI and plugin system are compatible with browser sandboxing and security.

## Benefits
- **Increased Accessibility:** Users can access the app from any device with a browser.
- **Unified Codebase:** Core logic shared between desktop and web.
- **Future Expansion:** Enables SaaS, cloud, and collaborative scenarios.

## Steps
1. Evaluate Avalonia WebAssembly and other .NET web UI frameworks.
2. Refactor code for platform abstraction.
3. Implement and test web frontend.

---

**References:**
- [Avalonia WebAssembly](https://avaloniaui.net/blog/avalonia-webassembly-preview/)
