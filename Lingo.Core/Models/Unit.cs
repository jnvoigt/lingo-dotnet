namespace Lingo.Core.Models;

public class Unit
{
    // The translation key / ID
    public string Id { get; set; } = null!;

    // The current value of this unit in this document.
    // For target documents this is the translation.
    // For source-only documents this is the source text.
    public string Value { get; set; } = null!;

    // The original source text. Null for simple key/value formats
    // that have no concept of a separate source language reference.
    public string? Source { get; set; }

    // Translation state. Null for formats that do not support it.
    public TranslationState? State { get; set; }

    // Metadata type — used to signal duplicates or missing keys
    public UnitType Type { get; set; } = UnitType.Default;
}