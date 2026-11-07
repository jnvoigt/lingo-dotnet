# Lingo GUI Editor & MCP Server — Technical & Functional Specification

## 1. Executive Summary & Purpose

The **Lingo GUI Editor & MCP Server** is a modular, cross-platform localization toolkit built on top of the Lingo Foundation (`Lingo.Core`). The primary objective of the architecture is to maximize localization throughput, operational efficiency, and data integrity across all operating environments (Linux, macOS, Windows). It delivers this through dual interaction models:
1. **Interactive Visual Ergonomics:** A structured, high-utility Avalonia visual interface for human developers and translators.
2. **Autonomous Agent Automation:** A standalone Model Context Protocol (MCP) server that empowers AI agents to inspect, manipulate, synchronize, and validate localization keys via structured tools rather than wasteful and error-prone raw file parsing.

By decoupling format handling (`Lingo.Core`), workspace state management (`Lingo.Workspace` / shared model), user interface (`Lingo.UI`), and agent integration (`Lingo.Mcp`), the system eliminates redundant I/O, prevents file corruption, reduces cognitive and computational overhead, and accelerates multilingual release cycles.

---

## 2. Target Platforms & Technology Stack

To ensure the greatest reach, maximum execution efficiency, uniform cross-platform behavior, and low maintenance overhead:

- **Desktop Framework:** [Avalonia UI](https://avaloniaui.net/) (v12+ / cross-platform XAML)
- **Runtime:** .NET 10.0
- **Protocol & Integration:** Model Context Protocol (MCP) via JSON-RPC (Standard I/O and optional SSE/HTTP)
- **Architecture Pattern:** Decoupled layered architecture:
  - `Lingo.Core`: Low-level document models, format parsers/writers (XLIFF 1.2/2.0, PO, JSON), key hierarchy builders, synchronizers.
  - `Lingo.Workspace` (Shared Memory Model): In-memory workspace session, dirty tracking, selective file persister, and file system change tracker.
  - `Lingo.UI`: Desktop GUI using MVVM (`CommunityToolkit.Mvvm`).
  - `Lingo.Mcp`: Headless MCP server providing tool endpoints for AI agents and automated pipelines.
- **Target OS:**
  - Linux (X11 / Wayland)
  - macOS (Apple Silicon / Intel)
  - Windows (x64 / ARM64)

---

## 3. Core Functional Requirements

### 3.1 Project & File Management

- **Single File & Directory Mode:**
  - Open individual localization files (`.xlf`, `.xliff`, etc.).
  - Open project directories/workspaces using `Lingo.Core.Files.FileCrawler` to automatically discover and group related translation files.
- **Source & Target Pair Management:**
  - Auto-pairing of source and target localization files based on file naming conventions and language codes (`IHasCultureInfo`).
  - Ability to add new target languages with automated template scaffolding from the source document.
- **Synchronization Engine Integration:**
  - One-click and automated synchronization of target files against source updates via `DocumentSynchronizer` and `KeySynchronizer`.
  - Visual preview of structural changes (added, removed, or modified translation units) before committing sync operations to disk.
- **Selective Persistence (Dirty File Saving):**
  - **Minimal I/O Writing:** When a user or agent invokes `Save` or `Save All`, the persister compares the in-memory state against the initial loaded baseline (via SHA-256 content hashes and mutation flags) and writes **strictly to files whose contents have actually changed**.
  - **Metadata & Timestamp Preservation:** Untouched files remain unaltered on disk, preserving file modification timestamps, preventing spurious CI/CD triggers, and avoiding unnecessary git commit noise.
  - **Atomic File Replacement:** Dirty files are serialized to temporary staging files (`.tmp`) and atomically renamed/replaced to prevent partial writes during sudden power loss or application crashes.
- **External Disk Change Tracking & Concurrency Management:**
  - **Active File System Monitoring:** All opened workspace files are registered with a file system watcher (`FileWatcherService`) to detect external modifications in real time (e.g., changes from git branch switches, automated build pipelines, external editors, or agent edits).
  - **Intelligent Conflict Detection:**
    - *Clean File Changed on Disk:* When an unmodified in-memory file changes on disk, the editor automatically reloads the document seamlessly or notifies the user with a low-friction toast.
    - *Dirty File Changed on Disk:* When an externally modified file also contains unsaved in-memory edits, the system alerts the user/agent with a non-destructive conflict prompt offering options to:
      1. *Reload from Disk* (discarding in-memory edits),
      2. *Overwrite Disk* (preserving in-memory edits),
      3. *Inspect Visual Diff / 3-Way Merge* (resolving conflicting unit translations individually).
- **Crash Recovery & Drafts:**
  - Periodic background snapshots stored in an isolated cache directory, ensuring zero data loss in the event of unexpected system interruptions.

---

### 3.2 Key-Centric Workflow & Hierarchical Key Tree Navigation

To provide maximum productivity for developers and translators who think in terms of semantic keys rather than individual disconnected files:

- **Hierarchical Key Tree View:**
  - Delimiter-based key decomposition (default `.` delimiter):
    - Common key path segments act as directory/folder nodes in the hierarchy.
    - Example: `A.B.Key` and `A.C.Key2` are organized under root directory node `A`, branching into folder nodes `B` and `C`, terminating in leaf key nodes `Key` and `Key2`.
  - Seamless support for mixed parent/leaf nodes (e.g. `user` and `user.profile`).
  - Search and filter within the key tree with instantaneous auto-expansion of matching branches.
  - Subtree status badges showing translation completeness and missing key counts for all child keys under each directory node.
- **Key-Centric Multi-Locale Editing:**
  - Selecting any key in the tree displays a unified editing workspace presenting that key across **all open language files simultaneously** (e.g., `en-US` source, `de-DE`, `fr-FR`, `es-ES`).
  - Allows translators and localization engineers to inspect context and edit target values side-by-side without context-switching between separate document tabs.
- **Criteria-Based Key Queries & Triage Operations (`Lingo.Core.Query`):**
  - **Outdated Translations Retrieval:** Instantly query units whose source text or context has been updated upstream and require re-translation/adaptation (`TranslationState.NeedsAdaptation`), eliminating manual audit overhead.
  - **Untranslated Keys Retrieval:** Pinpoint all keys that lack completed translations—including completely missing units in target files, units in `TranslationState.NeedsTranslation`, and units with blank/whitespace target strings.
  - **Missing In Specific Locales:** Filter keys that exist in source files or sibling locales but have no entry in a specific designated target document.
  - **Hierarchical Tree Pruning:** Dynamic pruning of the key hierarchy tree based on active query criteria, preserving ancestor paths while hiding irrelevant branches so users focus strictly on actionable units.
  - **Key Pattern & Full-Text Search:** Wildcard matching (`*`, `?`), regex pattern queries, and bidirectional full-text search across Key ID, Source Text, and Target Translations.
- **Missing Key Detection & Format-Aware Synchronization:**
  - When a key exists in one language file but is missing in others (or when a user introduces a new key via the UI):
    - Missing keys are highlighted with a prominent visual warning indicator (`⚠️ Missing in this file`).
    - **Single-Key Sync:** One-click synchronization (`Sync This Key to Missing Languages`) creates the missing unit in target files with appropriate format-specific defaults (`TranslationState.NeedsTranslation`, copying source text or empty target according to format rules).
    - **Batch Project Sync:** Global sync command (`Sync All Missing Keys`) iterates across all loaded documents, detects all key discrepancies, and synchronizes them bidirectionally or from source to target.
    - **New Key Scaffolding:** Adding a new key immediately updates the hierarchical tree and scaffolds translation entries across all active languages.

---

### 3.3 Translation Workspace & Grid View (Alternative View Mode)

For bulk table-oriented tasks, the editor supports a full-table grid view alongside the Key Tree view:

- **Translation Unit Grid View:**
  - Columns:
    - Status / State icon (New, Needs Review, Translated, Signed-off, Obsolete)
    - Key / Unit Identifier (`unit.Id`)
    - Source Text (`unit.Source`)
    - Target Text (`unit.Target` / `unit.Value`)
    - Context / Notes / Comments
  - In-place grid editing and dedicated detail pane with multi-line text boxes and spell check support.
  - Side-by-side or stacked diff view when source text changes across revisions.
- **Filtering, Searching & Sorting:**
  - Real-time full-text search across Keys, Source, Target, and Comments.
  - Quick filters:
    - *Untranslated / Empty Target*
    - *Needs Review / Fuzzy*
    - *Modified / Unsaved*
    - *Missing Across Locales*
    - *Errors / Validation Warnings*
- **State & Workflow Management:**
  - Direct modification of translation state (`IHasTranslationState`).
  - Status batch operations (e.g., "Mark all filtered units as Translated").

---

### 3.4 Quality Assurance & Productivity Tools

To minimize human translation errors and streamline repetitive actions:

- **Validation & Sanity Checks:**
  - Missing variable / placeholder detection (e.g., `{0}`, `%s`, `$name`).
  - Trailing/leading whitespace and newline mismatch alerts.
  - Length constraint warnings.
  - Duplicate key detection (`DuplicateTranslationKeyException`).
- **Keyboard Ergonomics & Shortcuts:**
  - `Ctrl+S` / `Cmd+S`: Save active document(s) (dirty-only persistence).
  - `Ctrl+Shift+S` / `Cmd+Shift+S`: Save all dirty documents.
  - `Ctrl+Enter` / `Cmd+Enter`: Commit translation and navigate to the next untranslated entry.
  - `Ctrl+K` / `Cmd+K`: Copy source text directly into target input.
  - `Ctrl+F` / `Cmd+F`: Focus search filter.
  - `Ctrl+N` / `Cmd+N`: Add new translation key.
  - `Alt+Up` / `Alt+Down`: Move between previous/next translation unit.
- **Batch Operations:**
  - Batch search and replace with regex support.
  - Bulk copy source to target for untranslated strings.
  - Batch synchronization across all sibling translation files.

---

### 3.5 Model Context Protocol (MCP) Server & Autonomous Agent Tooling

To maximize agent productivity and eliminate the massive token waste and XML corruption risks associated with autonomous agents parsing raw localization files, the system exposes a high-utility **MCP Server** implemented in a dedicated project (`Lingo.Mcp`):

- **Headless & Decoupled Architecture:**
  - Operates independently of the Avalonia UI runtime via standard JSON-RPC over `stdio` or SSE/HTTP.
  - Backed by the shared workspace memory model (`Lingo.Workspace`), ensuring consistent business logic and validation rules across both GUI and AI agent interactions.
- **Granular Agent Operations (MCP Tools):**
  - `lingo_open_workspace`: Load a localization project directory into memory, auto-discovering files and building the key hierarchy.
  - `lingo_list_keys`: Query translation keys by prefix, pattern, or completion state, returning compact structural summaries.
  - `lingo_get_translation`: Fetch all source, target, state, and comment metadata for a key across all loaded locales.
  - `lingo_update_translation`: Update the translated value and state for a specific key and locale in memory.
  - `lingo_add_key`: Create a new translation key and scaffold it across all active locales.
  - `lingo_sync_keys`: Run missing key synchronization across locales (single key or whole workspace).
  - `lingo_validate_workspace`: Execute full QA checks (placeholders, formatting, missing keys) and return structured issue diagnostics.
  - `lingo_save_workspace`: Persist in-memory modifications selectively to dirty files on disk.
  - `lingo_get_status`: Inspect loaded files, locale coverage, dirty document list, and pending changes.

---

## 4. UI/UX Layout Specification

### 4.1 Key-Centric Tree View (Primary Mode)

```
+---------------------------------------------------------------------------------------------------------+
| Menu Bar (File, Edit, Sync, View, Tools, MCP Server, Help)                                              |
+---------------------------------------------------------------------------------------------------------+
| Toolbar: [📂 Open Project] [💾 Save (2 dirty)] [🔄 Sync All Missing Keys]            [🔀 Switch View]    |
+---------------------------------------------------------------------------------------------------------+
| [All] [⏳ Untranslated] [⚠️ Outdated] [❌ Missing] | [🔍 Filter: Search key/text...] | [➕ Add & Sync Key]|
+---------------------------+-----------------------------------------------------------------------------+
| Translation Key Hierarchy | Key Translation Workspace: A.B.Key                                          |
|                           | [⚡ Sync This Key to Missing Languages]  [💾 Save Translations]              |
| 📁 A (3 keys)             +-----------------------------------------------------------------------------+
|  ├── 📁 B                 | 🌐 en-US (Source) (App.en-US.xlf)               [State: Translated]         |
|  │    └── 🏷️ Key          | Source:      Save File                                                      |
|  ├── 📁 C                 | Translation: Save File                                                      |
|  │    └── 🏷️ Key2         +-----------------------------------------------------------------------------+
|  └── 📁 Settings          | 🌐 de-DE (App.de-DE.xlf *)                      [State: Translated] ● Dirty |
|       └── 🏷️ Theme        | Source:      Save File                                                      |
| 📁 General (2 keys)       | Translation: [Datei speichern                                             ] |
|  ├── 🏷️ Ok                +-----------------------------------------------------------------------------+
|  └── 🏷️ Cancel            | 🌐 fr-FR (App.fr-FR.xlf)                        [⚠️ Missing in this file]   |
|                           | Source:      Save File                                                      |
|                           | Translation: [Enregistrer le fichier                                      ] |
+---------------------------+-----------------------------------------------------------------------------+
| Status: Filter: Untranslated | 3 languages loaded | 2 dirty files | ⚡ MCP Server: Listening (stdio)    |
+---------------------------------------------------------------------------------------------------------+
```

### 4.2 External Disk Conflict Resolution Dialog

```
+-----------------------------------------------------------------------------------+
| ⚠️ External File Modification Detected                                            |
+-----------------------------------------------------------------------------------+
| The file 'App.de-DE.xlf' was modified on disk by an external process or git.     |
| You have unsaved in-memory changes in the editor.                                 |
|                                                                                   |
| In-Memory Hash:   a1b2c3d4... (3 keys modified)                                   |
| Disk File Hash:   e5f6g7h8... (Modified 10 seconds ago)                          |
|                                                                                   |
| Choose resolution strategy:                                                       |
|   [ 🔄 Reload from Disk ]  Discard in-memory edits and use disk version           |
|   [ 💾 Overwrite Disk   ]  Overwrite external changes with in-memory version       |
|   [ 🔍 Inspect & Merge  ]  Open side-by-side translation unit diff dialog         |
+-----------------------------------------------------------------------------------+
```

---

## 5. Architectural Design & Component Breakdown

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 PRESENTATION & CONSUMPTION                             │
│                                                                                        │
│   ┌──────────────────────────────────────────┐    ┌────────────────────────────────┐   │
│   │                 Lingo.UI                 │    │           Lingo.Mcp            │   │
│   │   (Avalonia Desktop GUI Application)     │    │   (Headless AI Agent Daemon)   │   │
│   │  ┌─────────────────┐ ┌────────────────┐  │    │  ┌───────────────────────────┐ │   │
│   │  │ Views (XAML)    │ │ ViewModels     │  │    │  │ MCP Tool Handlers / JSON  │ │   │
│   │  └─────────────────┘ └────────────────┘  │    │  └───────────────────────────┘ │   │
│   └────────────────────┬─────────────────────┘    └────────────────┬───────────────┘   │
└────────────────────────┼───────────────────────────────────────────┼───────────────────┘
                         │                                           │
                         ▼                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                              Lingo.Workspace (Shared Memory Model)                     │
│  ┌───────────────────────┐ ┌───────────────────────────┐ ┌──────────────────────────┐  │
│  │ WorkspaceSession      │ │ DirtyStateManager         │ │ FileWatcherService       │  │
│  │ (Multi-Doc In-Memory) │ │ (Selective Persister)     │ │ (Disk Change Tracker)    │  │
│  └───────────────────────┘ └───────────────────────────┘ └──────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────────────────────────┐  │
│  │ WorkspaceOperationsFacade (Key Queries, Batch Sync, QA Rules, Diff Merge)         │  │
│  └───────────────────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────┬───────────────────────────────────────────┘
                                             │ References
                                             ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                       Lingo.Core                                       │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌────────────────┐ ┌────────────────┐ │
│  │ Document Models     │ │ Format Parsers/Writ │ │ KeyHierarchy   │ │ KeySynchronizer│ │
│  │ (ILingoDocument)    │ │ (XLIFF 1.2 / 2.0)   │ │ (KeyNode/Tree) │ │ & DocSync      │ │
│  └─────────────────────┘ └─────────────────────┘ └────────────────┘ └────────────────┘ │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### 5.1 Project Decoupling & Responsibility Matrix

| Project | Responsibility / Role | Dependencies |
| :--- | :--- | :--- |
| **`Lingo.Core`** | Pure domain models, low-level document parsers/writers, format schemas (XLIFF, etc.), unit synchronization, key tree hierarchy building. Zero UI or OS-watcher dependencies. | None (Core runtime only) |
| **`Lingo.Workspace`** *(New)* | Common in-memory workspace session, document collection index, key-to-locale index, dirty state computation (SHA-256 baseline), selective persistence, and `FileSystemWatcher` change detection. | `Lingo.Core` |
| **`Lingo.UI`** | Avalonia desktop interface, MVVM view models, interactive key tree, multi-locale translation cards, conflict resolution dialogs, keyboard navigation. | `Lingo.Workspace`, `Lingo.Core` |
| **`Lingo.Mcp`** *(New)* | Standalone console executable implementing the Model Context Protocol (MCP) over `stdio` and SSE. Translates agent tool calls into `Lingo.Workspace` operations. | `Lingo.Workspace`, `Lingo.Core` |
| **`Lingo.Cli`** | Command-line batch sync, linting, and format conversion utility. | `Lingo.Core` |

---

## 6. Common In-Memory Workspace Model & Change Management

### 6.1 `WorkspaceSession` Model
The shared `Lingo.Workspace` project provides the single source of truth for an active localization project:
- **Document Index:** Maintains active `ILingoDocument` instances mapped by their normalized file paths and culture info.
- **Key Index:** Inverted index mapping translation keys (`unit.Id`) to corresponding `Unit` instances across all loaded documents.
- **Baseline Snapshot Cache:** Stores original file content hashes upon load to enable strict dirty tracking.

### 6.2 Selective Persistence Engine (`SelectivePersister`)
To maximize write efficiency and minimize disk disruption:
1. **Dirty Evaluation:**
   - Every document tracks an `IsDirty` flag, set to `true` whenever any unit is added, modified, or removed.
   - On save, the persister recalculates the serialized content hash against the baseline loaded hash. If identical, the document is marked clean and disk write is skipped.
2. **Selective Writing:**
   - `SaveDirtyFilesAsync()` iterates exclusively over dirty documents.
   - Clean documents are never written, preserving filesystem timestamps and preventing redundant I/O.
3. **Atomic File Replace:**
   - Each modified file is written to `{filePath}.tmp` first, flushed, and atomically moved over `{filePath}`.

### 6.3 External Disk Watcher & Conflict Pipeline
To prevent data loss from concurrent external file edits:
1. **Watcher Loop:** `FileWatcherService` uses .NET `FileSystemWatcher` with debounce timers to detect `Changed`, `Created`, or `Deleted` events on workspace files.
2. **Hash Comparison:** When a disk event triggers, the service reads the file and computes its current hash against the loaded baseline:
   - If disk hash matches in-memory baseline $\rightarrow$ file was rewritten identically (ignored).
   - If disk hash differs and in-memory document is clean $\rightarrow$ automatic transparent reload.
   - If disk hash differs and in-memory document is dirty $\rightarrow$ `ExternalConflictDetected` event raised with memory and disk document snapshots.

---

## 7. Model Context Protocol (MCP) Server Specifications

The `Lingo.Mcp` project implements a standard MCP server for seamless integration with AI coding agents (Claude Desktop, Junie, Cline, Cursor, JetBrains AI).

### 7.1 Tool Definitions

#### 1. `lingo_open_workspace`
- **Purpose:** Discovers and loads localization files from a directory into memory.
- **Input Schema:**
  ```json
  {
    "path": { "type": "string", "description": "Absolute or workspace-relative path to project directory" },
    "pattern": { "type": "string", "description": "Optional search pattern (default: **/*.xlf, **/*.xliff)" }
  }
  ```
- **Output:** JSON summary of loaded files, detected source/target locales, and total key count.

#### 2. `lingo_query_keys`
- **Purpose:** Executes advanced criteria-based queries over workspace translation keys, returning targeted results and saving agent token budget.
- **Input Schema:**
  ```json
  {
    "filterMode": { "type": "string", "enum": ["All", "UntranslatedOnly", "OutdatedOnly", "TranslatedOnly", "MissingOnly", "Custom"], "description": "High-level filter mode" },
    "targetLanguage": { "type": "string", "description": "Optional locale code to restrict criteria evaluation (e.g., 'de-DE')" },
    "keyPattern": { "type": "string", "description": "Optional glob or wildcard pattern for keys (e.g., 'Settings.*', '*.Button')" },
    "searchText": { "type": "string", "description": "Optional full-text search string in key, source text, or target translation" },
    "requiredState": { "type": "string", "enum": ["NeedsTranslation", "NeedsAdaptation", "Translated"], "description": "Explicit translation state to match" },
    "limit": { "type": "integer", "description": "Maximum number of keys to return (default: 100)" }
  }
  ```
- **Output:** Structured `KeyQueryResult` containing matches, missing flags, outdated flags, untranslated flags, and distinct matching key IDs.

#### 3. `lingo_get_untranslated_keys`
- **Purpose:** Fast convenience operation returning all keys that are missing in target locales, have `NeedsTranslation` status, or have empty translations.
- **Input Schema:**
  ```json
  {
    "targetLanguage": { "type": "string", "description": "Optional locale code to filter by specific language (e.g., 'fr-FR')" }
  }
  ```
- **Output:** Array of untranslated key items with source text and target language status.

#### 4. `lingo_get_outdated_translations`
- **Purpose:** Fast convenience operation retrieving all keys whose source text changed and require re-translation/adaptation (`TranslationState.NeedsAdaptation`).
- **Input Schema:**
  ```json
  {
    "targetLanguage": { "type": "string", "description": "Optional locale code to filter by specific language (e.g., 'de-DE')" }
  }
  ```
- **Output:** Array of outdated key items with updated source text and current obsolete target translation.

#### 5. `lingo_get_translation`
- **Purpose:** Returns complete translation details for a key across all locales.
- **Input Schema:**
  ```json
  {
    "key": { "type": "string", "description": "Exact translation key (e.g., 'App.Dialog.Save')" }
  }
  ```
- **Output:** Object containing source text, comments, and locale-specific target texts and states.

#### 6. `lingo_update_translation`
- **Purpose:** Updates or sets translation target text and state in memory.
- **Input Schema:**
  ```json
  {
    "key": { "type": "string", "description": "Translation key identifier" },
    "locale": { "type": "string", "description": "Target locale code (e.g., 'de-DE')" },
    "target": { "type": "string", "description": "Translated target string" },
    "state": { "type": "string", "enum": ["Translated", "NeedsReview", "NeedsTranslation", "SignedOff"], "description": "Translation state" }
  }
  ```
- **Output:** Success acknowledgement with updated unit details.

#### 7. `lingo_sync_keys`
- **Purpose:** Synchronizes missing keys across all loaded documents.
- **Input Schema:**
  ```json
  {
    "key": { "type": "string", "description": "Optional single key to sync. If omitted, syncs all missing keys." }
  }
  ```
- **Output:** Count and list of units created across target documents.

#### 8. `lingo_validate_workspace`
- **Purpose:** Runs QA verification across all keys and documents.
- **Input Schema:** `{}`
- **Output:** Diagnostics list containing placeholder mismatches, empty targets, and format errors.

#### 9. `lingo_save_workspace`
- **Purpose:** Persists all dirty in-memory documents to disk using selective persistence.
- **Input Schema:**
  ```json
  {
    "forceAll": { "type": "boolean", "description": "If true, writes all files; otherwise only dirty files (default: false)" }
  }
  ```
- **Output:** List of updated file paths written to disk and count of skipped clean files.

---

## 8. Implementation Roadmap & Value Delivery

1. **Phase 1: Foundation & Key-Centric Architecture (Completed)**
   - Initialized `Lingo.UI` Avalonia project and linked `Lingo.Core`.
   - Implemented `KeyHierarchyBuilder` and `KeyNode` in `Lingo.Core` with delimiter-based path decomposition.
   - Implemented `KeySynchronizer` for cross-language missing key detection and synchronization across XLIFF 1.2 and 2.0.
   - Built MVVM Key Tree View and multi-locale translation card editor.
2. **Phase 2: Common Memory Model & Selective Persistence (`Lingo.Workspace`)**
   - Create `Lingo.Workspace` shared project.
   - Implement `WorkspaceSession`, `DirtyStateManager`, and `SelectivePersister`.
   - Implement `FileWatcherService` for active disk tracking and conflict detection.
   - Integrate `Lingo.UI` to bind directly against `WorkspaceSession`.
3. **Phase 3: Standalone MCP Server (`Lingo.Mcp`)**
   - Create `Lingo.Mcp` project with standard JSON-RPC protocol handling.
   - Implement tool handlers for workspace management, key query, translation updating, synchronization, and QA validation.
   - Provide integration documentation and testing scripts for autonomous agent clients.
4. **Phase 4: QA, Diff Merge & Advanced Ergonomics**
   - Implement placeholder verification, whitespace checks, and conflict visual merge dialog.
   - Comprehensive keyboard shortcuts and status bar indicators for MCP server and dirty file counts.
5. **Phase 5: Extended Formats & Translation Memory**
   - Expand supported formats in `Lingo.Core` (Android XML, PO, JSON).
   - Translation memory indexing and external MT API hooks.

