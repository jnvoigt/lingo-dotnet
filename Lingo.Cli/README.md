# Lingo CLI

Lingo localization tool.

## Usage

```bash
dotnet run --project Lingo.Cli -- [command] [options]
```

### Commands

#### sync

Synchronizes a source localization file to a target file or all sibling files if target is omitted.

**Options:**

- `-s`, `--source <FILE>` (Required): The source localization file.
- `-t`, `--target <FILE>`: The target localization file.

**Examples:**

Sync a specific target:

```bash
dotnet run --project Lingo.Cli sync --source "source.xlf" --target "target.xlf"
```

Sync all sibling files in the same directory:

```bash
dotnet run --project Lingo.Cli sync --source "source.xlf"
```
