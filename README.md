# Acrux Metro 2030 build

Native Windows 11 WinUI 3 application. Run .\Start-Acrux.ps1 with the .NET 10 SDK installed; the script detects ARM64/x64 and compiles into a fresh folder.

## Four activities

Capture saves ideas, links and pasted web excerpts into Inbox or Web clippings as Markdown files. Titles are optional, duplicate filenames get numbered, and a source address is retained. It does not download web pages. Write is the default: notebook/folder/note navigation, a flat document and optional details pane. Connect opens a graph of actual wiki-links. Review files captured notes, completes Markdown task checkboxes and clears explicit review flags.

The typography-led shell retains large capture/write/connect/review headings. At narrow widths the brand disappears, headers shrink, commands move below the headings and navigation becomes a toggled page. Both rails are hidden by default in a sidebar window. Theme and document font choices live in Settings. Canvas, source mode, inserts, daily notes and notebook tiles are available from the overflow menu.

The editor now uses one native RichEditBox document per canvas container. Selection crosses paragraphs, with document-level undo/redo, simple heading/bold styling and a native text context toolbar. Markdown is the source of truth; typing does not swap paragraph controls. Source mode shows Markdown without styling. The formatting layer is an initial native implementation, not full Obsidian parity: Markdown syntax remains visible, advanced embeds/tables are not rendered inline, styling is limited, source/layout switches rebuild the editor and reset its history. Test long documents and IME before production use.

References are metadata, not embeds. Add reference explains this. Right-click a reference to edit, remove or insert its link into the note. Metadata-only changes preserve the editor when its body is unchanged. Ctrl+O opens a note switcher; Ctrl+P opens commands; Ctrl+S saves; Ctrl+E switches Markdown styling; F11 toggles focus.

Notes remain ordinary .md files, tags/references/review flags use frontmatter, and canvas positions use .md.acrux.json sidecars. Existing files are preserved and save operations check external changes. No cloud collaboration, browser clipper extension, inbound Windows share target or radial pen menu is claimed in this build.

## Validation tools

Acrux.exe --smoke-test exercises isolated storage/editing/navigation/layout cases and writes smoke-test-results.json. Acrux.exe --design-proof exports the real WinUI visual tree for writing, capture, notebooks, graph, review and 960/400 effective-pixel writing layouts. These are native renders, not HTML prototypes.

## Metro column layout update

Write now has independent notebooks, notes and document columns. Details are closed by default and available on request; smaller windows offer a dedicated details surface. Notebook navigation collapses below 1280 effective pixels, the note list below 900; the navigation toggle restores them. At sidebar widths the list/navigation occupy their own view until a note is selected.

Capture has type selection, quick-entry and recent-capture columns. Review has Inbox, unfinished tasks and Revisit columns. Connect has searchable notes, zoomable graph and selected-note context; selecting a graph node previews it before opening. The graph shows at most 80 matching nodes, with an explicit count; the searchable list remains available. Neighbourhood filtering focuses on the selected note and its actual links.

Activity columns switch to a native column selector below 1050 effective pixels. Each desktop column scrolls independently. Typography, whitespace and accent-edge selection establish hierarchy, with fewer borders and a quieter light palette. Ortrion's native source layout was consulted for family conventions; no Ortrion files were changed.
