# Validation: Focus surfaces

Native ARM64 build targets .NET 10 / WinUI 3. Final status is recorded in artifacts/MetroFocus/smoke-test-results.json. The regression suite includes document-wide native selection, capture storage/collision protection, no permanent navigation at desktop and narrow widths, summoned Explore, dismissal back to Write, Markdown preservation, undo/redo, sidecars, rename and external-change protection.

Native visual-tree exports cover Write, Capture, Connect, Review, notebook overview and 960/400px writing. Write/Capture use one continuous app background without note dividers or panel fills. Destination headings are lowercase and moderately scaled in response to the user's final correction. No underlines remain.

The editor still exposes Markdown syntax and a limited styling subset; no complete Obsidian live-preview parity is claimed. Manual keyboard, IME, accessibility, multi-monitor and long-document performance checks remain necessary. x64 is configured but not validated on this ARM64 machine. Runtime artifacts are excluded from Git.

Home/proximity update: 59 isolated checks passed, including startup notebook tiles, approach geometry, and actual native toolbar fade/reveal states. Build: zero warnings and errors.
