# XLIFF 1.2 Class Generation

The C# classes in `Lingo.Core.Formats.Xliff.V12.cs` are generated from the XLIFF 1.2 XSD using `XmlSchemaClassGenerator`.

## Generation Command

To (re)generate the classes, run the following command from the project root:

```bash
xscgen -v --ca -n Lingo.Core.Formats.Xliff.V12 -o Lingo.Core/Formats/Xliff/v12 Lingo.Core/Formats/Xliff/v12/xliff-core-1.2-transitional.xsd
```

## Intended Outcome & Manual Modifications

The XSD defines `xml:space` with `default="default"` for several elements. By default, `XmlSerializer` includes these attributes in the output even when they match the default value.

To suppress the redundant `xml:space="default"` in the output while keeping the code compatible with the XSD, the following manual modifications must be applied to the generated `Lingo.Core.Formats.Xliff.V12.cs` file:

### 1. Add `[DefaultValue(Space.Default)]` to `Space` properties

Find the `Space` property in the following classes and add the `[DefaultValue(Lingo.Core.Formats.Xliff.V12.Space.Default)]` attribute:

*   `File`
*   `Group`
*   `TransUnit`
*   `AltTrans`

**Example:**

```csharp
[System.ComponentModel.DefaultValueAttribute(Lingo.Core.Formats.Xliff.V12.Space.Default)]
[System.Xml.Serialization.XmlAttributeAttribute("space", Namespace="http://www.w3.org/XML/1998/namespace", Form=System.Xml.Schema.XmlSchemaForm.Qualified)]
public Lingo.Core.Formats.Xliff.V12.Space Space { get; set; }
```

### 2. (Optional) Initialize private fields

For classes that use a backing field for `Space`, ensure it is initialized to `Space.Default`:

```csharp
private Lingo.Core.Formats.Xliff.V12.Space _space = Lingo.Core.Formats.Xliff.V12.Space.Default;
```

These changes ensure that `xml:space="default"` is omitted during serialization, but `xml:space="preserve"` is still correctly generated when explicitly set.
