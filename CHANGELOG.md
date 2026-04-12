# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0]

### Added
- **Core (Lingo.Core)**:
  - Initial implementation of translation unit enums, exceptions, and core interfaces (`ILingoDocument`, `ILingoDocumentFactory`, `ILingoDocumentWriter`).
  - Full support for **XLIFF 1.2 and 2.0** formats, including parsing, writing, and synchronization logic.
  - `InMemoryLingoDocument` for flexible document handling.
  - `FileCrawler` and `LingoFileInfo` for automatic translation file discovery and culture extraction.
  - `DocumentSynchronizer` for syncing source and target documents with detailed feedback.
  - `TestDirectory` utility for cleaner unit test file management.
- **CLI (Lingo.Cli)**:
  - New `sync` command to synchronize translation files.
  - Support for syncing single files or entire directories (sibling file discovery).
  - Detailed console feedback for sync operations (Added/Updated/Removed units with color coding).
- **Documentation & Infrastructure**:
  - Comprehensive `README.md`, `CONTRIBUTING.md`, and `LICENSE` files.
  - Editor configuration and automated testing guidelines (`AGENT.md`).
- **Internal Refactorings & Optimizations (as part of initial development)**:
  - Unified `IXliffDocument` interface for both 1.2 and 2.0.
  - Dynamic document factory resolution based on file extensions.
  - Optimized inline tag flattening and unit conversion logic.
  - Generic typing for `ILingoDocumentWriter`.
  - Decoupled document factory selection via `LingoFormatProvider`.
