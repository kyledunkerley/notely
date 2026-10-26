# Acrux: product research and UI decisions

Research date: 26 October 2026. Sources are official product documentation, not third-party comparisons. The design decisions below are proposals for Acrux, rather than claims that the three products behave identically.

## What to take from each product

| Product | Documented pattern | Decision for Acrux |
| --- | --- | --- |
| Obsidian | Editing and reading views; live-preview and source modes | Markdown stays authoritative. Offer live preview for ordinary writing and visible source when needed. Never require export to recover the notes. |
| OneNote | Containers have move and resize handles, grow with content, and can be merged | Let people spatially arrange thoughts without putting coordinates into their writing. Keep movement affordances quiet until needed. Container merging needs explicit undo before it ships. |
| Notion | Blocks, slash insertion, selection formatting, and block actions | Make content types discoverable through a toolbar, insert menu, slash commands, and per-block actions. Keyboard users and mouse users should reach the same actions. |

Sources: [Obsidian editing modes](https://obsidian.md/help/edit-and-read), [OneNote containers](https://support.microsoft.com/en-us/onenote/onenote-help-and-learning/work-with-note-containers), [Notion writing basics](https://www.notion.com/en-gb/help/writing-and-editing-basics), [Notion slash commands](https://www.notion.com/help/guides/using-slash-commands).

## Editing: useful detail from the research

Obsidian separates presentation from storage. Its formatting documentation covers headings, emphasis, lists, tasks, links, images, quotations, and fenced code. Acrux should preserve unfamiliar syntax, protect code fences when dividing content into editable blocks, and retain a source view. Full CommonMark parsing and cursor movement across blocks remain work for a mature editor engine. [Formatting reference](https://obsidian.md/help/syntax).

Obsidian supports both wiki-style note links and Markdown links, including folder paths and heading targets. Acrux currently resolves note names and notebook-relative paths, exposes outgoing links and a simple backlink list, and renders ordinary web links. Complete link migration on rename, alias handling, anchor navigation, and broken-link repair need a dedicated index and resolver. [Internal links](https://obsidian.md/help/links).

Obsidian uses YAML at the start of a note for properties. Keep structured properties in a future inspector, while retaining the original frontmatter. A renderer must not reinterpret unfamiliar YAML as decorative separators or silently discard it. Typed properties, schema validation, and indexed queries are deferred. [Properties](https://obsidian.md/help/properties).

Obsidian offers command and note discovery through its command palette and quick switcher. Acrux now has both patterns, with visible shortcut hints, native dialogs, filtering, and keyboard selection. [Command palette](https://obsidian.md/help/plugins/command-palette), [Quick switcher](https://obsidian.md/help/plugins/quick-switcher), [Hotkeys](https://obsidian.md/help/hotkeys).

Notion makes structural editing visible through its block handle and conversion actions. Acrux now exposes move up/down, duplicate, delete, turn-into, and insertion controls. Drag-reordering, multi-block selection, cross-block clipboard operations, and a persistent undo timeline are deferred. Do not imply a database, equation engine, or collaborative comment system merely by showing a matching button. [Writing basics](https://www.notion.com/en-gb/help/writing-and-editing-basics), [Keyboard shortcuts](https://www.notion.com/help/keyboard-shortcuts).

OneNote demonstrates the importance of familiar keyboard formatting, contextual tables, attachments, and automatic saving. Acrux now supplies common formatting commands, Markdown table insertion, local pictures/file attachment insertion, and delayed local saving. Table cells are not a spreadsheet editor, and attaching a file creates a copy rather than a synchronized reference. [Keyboard shortcuts](https://support.microsoft.com/en-us/accessibility/onenote/keyboard-shortcuts-in-onenote), [Basic tasks](https://support.microsoft.com/en-us/onenote/onenote-help-and-learning/basic-tasks-in-onenote-on-windows).

## Canvas and portability

Acrux's current note format is one Markdown file with optional HTML-comment container markers. A companion JSON file stores container identity, position, and width. Note text remains readable without this layout file. Canvas expansion currently works right and down; negative-coordinate navigation, multi-selection, alignment, grouping, and canvas undo are deferred.

Obsidian's canvas uses the open JSON Canvas format. The specification defines nodes and edges, including text and file nodes. A future canvas export should use file references into the notebook, rather than making an embedded second copy of the Markdown authoritative. An export is planned research, not a feature implemented in this release. [Obsidian canvas](https://obsidian.md/help/plugins/canvas), [JSON Canvas specification](https://jsoncanvas.org/spec/1.0/).

Obsidian has configurable attachment locations and copies dropped files into the vault. Acrux currently copies imported pictures and documents into an assets folder beside the note, then writes a relative Markdown reference. Remote images are not fetched automatically by the current renderer. Clipboard image paste and drag-and-drop still need work. [Attachments](https://obsidian.md/help/attachments).

Notion's Markdown/CSV export illustrates a portability boundary: non-database pages and databases export differently, and not every visual block has an equivalent Markdown representation. Acrux should keep databases, canvas layout, and writing separate in its data model rather than promise a perfect Markdown mapping for arbitrary widgets. [Export documentation](https://www.notion.com/help/export-your-content).

## Metro 2030 application design

These choices are specific to this project and its supplied concepts:

- Use lowercase 30px pivot navigation, a clear active underline, 42px list headers, a 46px note title, and a 64px notebook landing heading.
- Keep navigation hierarchical: all notebooks, notebook, section/folder, note, container, block.
- Give notebooks large square tiles with actual note/folder counts and the latest edited note. Refresh existing tile values rather than replacing focused controls.
- Use a restrained sage/blue/earth palette, flat surfaces, and thin separators. Keep Windows window chrome, Mica, native menus, pickers, scroll/zoom, text controls, and keyboard navigation.
- Use the contextual right pane for outline, insert commands, and links. Collapse it at narrower widths. Focus mode removes navigation while retaining the writing tools.
- Keep a visible save status. Refuse to overwrite a file changed outside the app. Keep the draft available if saving fails.

## Delivery status and next gates

| Area | Current pass | Next gate |
| --- | --- | --- |
| Stability | Fixed shared-parent preview crash; UI-thread smoke runner; diagnostic log | Full desktop interaction, accessibility, and recovery tests |
| Notebook landing | Live square tiles, counts, sections, recent edits, create/open | Pinning, ordering, notebook colors and custom covers |
| Navigation | Metro pivots, hierarchical sections, recent notes, quick switcher | Tabs, history, split panes, scalable indexed search |
| Editor | Toolbar, block actions, tasks, links, tables, code, slash insertion, source | Unified cursor model, full Markdown parser, mature undo |
| Canvas | Movable/resizable containers, zoom, layout persistence | Negative coordinates, grouping, selection, merge and canvas undo |
| Windows integration | Native controls, file/folder pickers, local image/file insertion, appearance setting | Inbound Share Target, Jump Lists, multi-window, pen/ink |
| Ownership | Ordinary Markdown/folders; relative assets; layout sidecar | External-file watcher, conflict UI, portable canvas export |

The release notes and validation report distinguish implemented behavior from planned capabilities. No full parity with Obsidian, OneNote, or Notion is claimed.

## Reactor assessment, 26 October 2026

Microsoft.UI.Reactor is a declarative, C#-first MVU layer over native WinUI controls. Microsoft's announcement explicitly says it is experimental, lacks a support policy and API guarantees, and does not deprecate XAML. [Microsoft's announcement](https://devblogs.microsoft.com/ifdef-windows/microsoft-ui-reactor-a-c-first-way-to-build-winui-apps/).

Acrux's current interface is already constructed mainly in C#, with XAML used for application resources and a small items-panel template. Reactor adoption would therefore be a component/state-management change. It would not automatically improve the editor's cursor model or resolve the visual design work. The current decision is to keep this pass on direct WinUI, then evaluate Reactor in an isolated shell screen before committing to a migration. No Reactor dependency has been added.
