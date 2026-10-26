# Acrux usability audit — 26 October 2026

Research and source-code audit; proposed specification, not implemented changes. No app code changed in this research pass.

The priority is one continuous Markdown editor and a responsive writing shell. Left answers where notes live; centre is writing; optional right pane contains note details. Typography supplies hierarchy; native WinUI controls supply predictable interaction.

## Every control must earn its place

| Control | Actual behaviour | Decision |
|---|---|---|
| Capture | Currently notebook landing | Keep big Capture heading. Implement quick idea, pasted link/clipping and attachment capture into Inbox, with filing later. |
| Write | Writing workspace | Default destination. Keep large typography-led heading and continuous editor. |
| Connect | Relationship list/graph | Keep big Connect heading; graph-led destination showing real links, search and selection. |
| Review | Recent notes and unfinished task links | Keep big Review heading. Process Inbox, complete tasks and resurface explicitly marked notes. |
| Search | Opens filtered note drawer | Keep; distinguish Ctrl+F document search from Ctrl+O note search. |
| New note | Creates local note | Keep; destination clear, focus immediate. |
| Notebook | Selector and four small tiles | Keep selector; large live tiles belong on landing. Remove four-notebook discovery limit. |
| Section | Folder filtering capped at six | Replace with complete nested folder list/tree. |
| Free writing/Structured | Flowing page/freeform canvas | Rename Document/Canvas; move to note view menu. |
| Daily note | Creates/opens today's note | Creation shortcut/template, not a layout mode. |
| Markdown/Plain text | Formatted editing/raw Markdown | Rename Live preview/Markdown source; both are Markdown. |
| Focus levels | Hide one or both rails | Replace with Focus writing and independent panel toggles. |
| Writing style | Font choice | Settings > Editor. Same editing/reading typography. |
| Theme swatches | Palette preference | Settings > Appearance; not permanently visible. |
| Linked notes | Incoming/outgoing wiki-link matches, limited to five | Keep optional; separate backlinks/outgoing, resolve paths properly, expose full list. |
| Link another note | Inserts wiki-link | Ctrl+K, [[ suggestions and context menu; remove duplicated permanent action. |
| Tags | Frontmatter; chip click removes tag | Keep; click filters, explicit x removes with undo. |
| Sources | Frontmatter reference records; URLs open externally | Rename References; add edit/remove/copy/insert link. |
| Properties | Modified time/path/tags/links | Collapse under Details; remove duplicated counts. |
| Insert block | Formatting/insertion actions | Slash menu and contextual toolbar. |
| Arrange | Mixed graph/backlinks/split/focus/commands | Remove misleading heading; distribute to Connections and View. |
| Split view | Read-only raw source pane | Name precisely; only available with sufficient space. |
| Related notes | Explicit links plus arbitrary fallback notes | Remove fallback. Show real connections or an honest empty state. |
| Footer slogans | Decoration | Remove from writing; onboarding can explain portability. |
| Save | Explicit save plus autosave | Keep Ctrl+S/menu; compact visible save/error/conflict status. |
| Open source | Toggles Markdown source | Rename; separate Open containing folder from source view. |
| Paragraph handles | Separate paragraph controls | Remove from prose; container controls only in Canvas. |

## Adding a source

Currently AddSource stores title, optional HTTP(S) URL and detail in source_links frontmatter. It reloads the canvas and schedules saving. Inspector entries open URLs externally. No Markdown link, citation, bibliography or embed is inserted. Entries without a URL are metadata only. Edit/remove are missing.

Proposed Add reference dialog explains 'Saved with this note. Not inserted into its text.' Default Save reference; explicit optional 'Also insert link at cursor' creates ordinary Markdown links. Preserve selection and undo through the dialog. Embed is a separate Insert action, with actual rendered content and local attachments. Do not silently iframe arbitrary URLs. Obsidian embeds display content inline and use distinct syntax such as ![[Note]] [1].

## Seamless editor

The current StackPanel of preview buttons and individual TextBoxes cannot provide document-wide selection/caret/IME/undo merely by hiding borders. Metadata, font or mode changes can rebuild it. Replace with one Markdown buffer, transaction-based edits/selection, document-wide undo, incremental syntax decoration and stable scroll anchors. Live preview reveals relevant syntax near the caret without replacing the editing surface. Click places the caret where clicked. Formatting acts on the current selection. Mode changes preserve viewport/history.

Borrow Obsidian's Markdown-first editing and links; Notion's slash discovery, not its paragraph focus boundaries; OneNote's spatial organisation in a distinct Canvas mode.

Keep the shell native. Prototype WinUI RichEditBox continuous editing with Markdown as the unchanged source of truth. It supplies document ranges/selection/undo, but is rich-text oriented, not an automatic Markdown live-preview engine [6]. Do not mutate source to hide syntax. CodeMirror 6 in WebView2 is an alternative if the native prototype fails; it offers a document/transaction model [7], but makes the editor web-rendered. That materially changes the pure-native constraint and should not be silently introduced.

## Responsive contract

Use window width in effective pixels, not physical monitor pixels [2]. Proposed app breakpoints:

| Available width | Layout |
|---|---|
| 1440+ | 240px navigation, centred prose about 760–840px maximum, optional 280px details. |
| 1008–1439 | Compact navigation; details closed by default/overlay; no squeezing prose. |
| 640–1007 | Document first; navigation/details as dismissible overlays; compact header. |
| 360–639 | Sidebar capture: notebook selector, title, continuous note, minimal status; no permanent rails. |

1920x1080 at 150% scale is approximately 1280 effective pixels. Half-screen at 100% is around 960px before borders. Both require a reduced layout. Height must also adapt for 768px screens and touch keyboard. Resizing preserves selection/scroll and restores panel preferences on expansion.

Current defect: navigation stays 292px below 1280 and can add a 235px drawer. Header retains 369px brand column, spaced oversized pivots and 300px commands. Hiding only the inspector is insufficient. F11 currently means focus, not full-screen. Optional always-on-top should be an explicit pin; normal narrow-window writing must work first.

## Motion and Metro 2030

Metro 2030 is our design direction, not an official Microsoft standard. Use typography, alignment, space, content-first hierarchy and purposeful motion rather than tile grids everywhere.

Destination heading: directional 24–40px sweep plus fade around 250ms; underline follows selection. Note opening: restrained 12–16px entrance around 167ms. Commands: native hover/pressed feedback, optional tiny .98 press scale. Drawers slide from their anchored edge. No typing reflow, repeated save pulses or animation that steals focus. Respect Windows reduced-motion settings. WinUI guidance favours connected, responsive motion and existing controls/resources [3].

Use Segoe UI Variable for shell, configurable consistent prose typography, 4px spacing system, meaningful accessible labels, contrast and visible keyboard focus. Theme belongs in Settings with System option. Use material sparingly in chrome, opaque readable editor. Preserve native caption buttons, Snap and resize behaviour.

## OneNote-style local commands

Windows 8 OneNote radial menus are a valid historical precedent [5]. Default: native TextCommandBarFlyout/CommandBarFlyout near selection/right-click, retaining cut/copy/paste/spelling and adding bold/italic/link with overflow. WinUI recommends contextual command bars [4]. Preserve selection; Escape dismisses; Shift+F10 and screen readers work; clamp near window edges.

Test optional radial menu for pen/touch: at most six labelled sectors, safe central dismissal, no mandatory nested rings, conventional alternatives. Measure speed/error against compact toolbar before claiming improvement. Avoid covering the sentence being edited.

## Other requirements

References/tags become buffer transactions, not editor reloads. Preserve Markdown, unknown frontmatter, canvas comments and sidecars. Expose save conflicts through persistent actionable InfoBar. Native folder/note lists, suggestion search, flyouts, pickers and sharing should replace custom controls where they improve keyboard/accessibility behaviour. Do not advertise unimplemented sync/share success.

Acceptance: cross-paragraph selection; Ctrl+A/C/X/V; IME/emoji/RTL; list continuation/indent; paste; caret/history preserved through metadata/context menus/mode switches; 1080p at 100/125/150%, 1366x768, 960/640/400px windows; screen reader, high contrast, text scaling, keyboard-only and reduced motion; disk failure/external conflict; long-note typing latency and large notebook performance. Existing 45 checks are regressions, not proof of usability.

Engineering dependency: editor foundation first, responsive simplified shell alongside integration, then contextual commands/reference management/motion, followed by complete workflow validation. This is dependency order, not another sequence of cosmetic concept variants.

## Primary research

[1] https://obsidian.md/help/embeds
[2] https://learn.microsoft.com/en-us/windows/apps/design/layout/screen-sizes-and-breakpoints-for-responsive-design
[3] https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/motion
[4] https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/command-bar-flyout
[5] https://support.microsoft.com/en-au/office/touch-gestures-and-keyboard-shortcuts-in-onenote-for-windows-8-1763d5fc-9ab6-43aa-96df-e42dd9c29d88
[6] https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/rich-edit-box
[7] https://codemirror.net/docs/guide/ (official indexed documentation; direct fetch blocked)
[8] https://www.notion.com/help/writing-and-editing-basics
[9] https://obsidian.md/help/settings

Source inspection: ConceptShell.cs, ConceptViews.cs, EditingCommands.cs, MainWindow.cs, NoteCanvas.cs, LiveMarkdownEditor.cs. Proposed generated visual is a design study, not a screenshot or implementation proof.

## User clarification: four big Metro headings stay

Capture / Write / Connect / Review are the enduring family navigation. Write opens by default. Do not replace Capture with Notebooks or flatten this into a generic compact toolbar. Notebook tiles remain a landing surface reachable through notebook navigation.

Capture saves quick text, links and user-provided excerpts/attachments to an Inbox immediately. It may offer Web clippings as a capture type/location. A pasted URL is only a saved link; a real web clip must preserve selected content plus URL, title and capture date. Do not claim full article clipping before implemented. User can choose notebook/folder, but filing is optional at capture time. Saving must be explicit and recoverable before dismissing the window.

Review has three actionable groups: Inbox to file/archive/delete; unfinished Markdown tasks to complete in their original note; notes explicitly marked for review to open, mark reviewed or schedule for later. Notes remain Markdown; review metadata belongs in frontmatter. Empty states are honest. Review is not an AI summary or arbitrary productivity dashboard. Do not mark a note reviewed just because it was displayed. Show backlog counts without adding fake urgency.

Connect opens an actual graph with directional relationship details, selected-node title/context, click to open, search, zoom/home and keyboard-accessible equivalent list. No fabricated connections. Keep the graph useful with local-neighbourhood focus rather than an unreadable whole-vault hairball.

All four headings use large type on wide windows, sweeping selected-state transitions and restrained command reaction. At half width use smaller but prominent typography and horizontally scrollable pivot strip if necessary. At sidebar width show the selected heading plus an accessible destination switcher; all four remain reachable without squeezing four giant labels across 400px. Restore wide typography on expansion. Reduced motion remains supported.

The generated initial visual study predates this clarification: its reduced navigation set is superseded. Its document-first compact layouts remain useful as an exploration, not the final specification.