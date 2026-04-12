# Contributing to Lingo

Thank you for considering contributing to Lingo!

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Branching Model

- `main`: The stable branch. This branch always contains the latest release.
- `develop`: The development branch. Active work and features are merged here before being promoted to `main`.

## Pull Requests

- Open an issue before submitting a large PR.
- Link your PR to a corresponding issue.
- Ensure that all tests pass.
- Each PR should focus on a single concern.

## Adding a New Format Driver

To add support for a new localization format, you need to:
1. Implement the `ILingoDocument` interface (or derive from a base class if available).
2. Register your driver in the `LingoDocumentFactory`.

Please follow existing drivers in `Lingo.Core/Formats` as a reference.
