# Acrux: Metro 2030 Focus surfaces

Native Windows 11 / WinUI 3. Run .\Start-Acrux.ps1 with .NET 10 SDK installed. ARM64 is validated; x64 is configured.

## Focus, Explore, Context

Lowercase EVE-style destination headings use restrained scale and weight. Capture, Write, Connect and Review remain visible as neighbouring places. No underlined tabs or permanent notebook, note-list or inspector columns appear in Write. Explore summons the library through the header or Ctrl+O; selecting a note returns to Write. Context is temporary. Notebook overview/live tiles remain reachable through Explore.

Write uses a continuous native RichEditBox and a readable measure on the same background as the app. The document has no panel, card, divider or book frame. Capture opens to a borderless writing surface, with a light destination cue and a Keep in inbox action. Capture type/filing controls are summoned from Change. Drafts persist locally until filed; explicit filing creates an ordinary Markdown note in Inbox or Web clippings. Web clipping stores the supplied excerpt/link and does not download a web page.

Connect is a full working map, with search and zoom in the surface. Note context opens temporarily from a selected node. Review retains actionable Inbox, task and revisit queues on a continuous background. Capture/Write were the priority of this structural rebuild.

## Data and editor boundaries

Markdown files remain the source of truth. Unknown frontmatter is retained; tags, references and review flags use frontmatter; canvas layouts use sidecars. File saving checks for external changes. Native context commands include formatting; Ctrl+F finds within the note; Ctrl+P opens commands; Ctrl+E switches source styling.

The native editor is still an initial Markdown implementation, not complete Obsidian parity. Syntax markers remain visible; live styling supports a subset; advanced table/embed rendering and long-document optimisation remain unfinished. Reconstructing source/layout editors resets local undo history. Cloud collaboration, inbound sharing, browser clipping extensions and radial pen commands are not claimed.

## Validation

--smoke-test uses isolated files and writes smoke-test-results.json. --design-proof renders real WinUI surfaces for writing, capture, notebook overview, connect, review and 960/400px writing. These are actual native layouts, not generated mockups. Manual accessibility/IME/multi-monitor testing remains necessary.

## Home and proximity controls

Startup opens all notebook live tiles. The always-visible acrux. / home action returns there. Real note/section counts and modified dates refresh periodically while home is open.

The writing toolbar and top-right commands reveal within an 80px approach region, fade to a faint discoverable hint farther away and do not change layout during reveal. Keyboard focus/navigation, touch and open formatting menus keep commands visible. Home keeps its commands visible. Windows reduced-motion settings disable fades. Home/Explore and destination navigation remain visible.
