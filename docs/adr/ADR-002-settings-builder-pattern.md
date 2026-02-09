# ADR-002: Builder Pattern for Settings Construction

## Status

Rejected

**Date**: 2026-02-06

## Context

The `Settings` class is used to configure mount path-based configuration loading. Currently, it uses:
1. **Constructor initialization** - sets default values
2. **Property setters** - allows customization via object initializer or Action<Settings>
3. **Validation in setters** - ensures invalid values are rejected immediately

The question is whether introducing a Builder Pattern would improve the API design.

## Decision

**We reject the Builder Pattern for `Settings` class** in favor of the current approach using Action<Settings> delegates.

### Current API (Kept)

```csharp
// Object initializer
var settings = new Settings
{
    FileName = "database",
    ConfigMountPath = "config",
    Optional = false
};

// Action<Settings> delegate (most common usage)
config.AddMountConfiguration(settings =>
{
    settings.FileName = "database";
    settings.Optional = false;
});
```

### Builder Pattern (Rejected)

```csharp
// Hypothetical builder pattern
var settings = new SettingsBuilder()
    .WithFileName("database")
    .WithConfigMountPath("config")
    .WithOptional(false)
    .Build();

// In extension methods
config.AddMountConfiguration(builder =>
{
    builder.WithFileName("database")
           .WithOptional(false);
});
```

## Rationale

### Why Action<Settings> is Better

1. **Familiar .NET Pattern**
   - `Action<T>` is the standard .NET configuration pattern
   - Used throughout ASP.NET Core (e.g., `services.Configure<Options>()`)
   - Users already understand this pattern

2. **Simpler API Surface**
   - No additional builder class to maintain
   - Fewer types to document and test
   - Direct property access is intuitive

3. **Consistency with .NET Ecosystem**
   - Microsoft.Extensions.Configuration uses similar patterns
   - Follows principle of least surprise
   - Integration with existing code is seamless

4. **Property Validation Already Present**
   - Setters validate input immediately
   - No need for separate validation in Build() method
   - Fail-fast behavior at assignment time

5. **IntelliSense Support**
   - IDEs provide excellent support for object initializers
   - Property discovery is straightforward
   - No learning curve for the builder API

### Disadvantages of Builder Pattern

1. **Over-engineering**
   - Settings class has only 6 properties
   - No complex construction logic required
   - Builder adds unnecessary abstraction

2. **Verbosity**
   - `WithPropertyName()` methods are more verbose than property setters
   - Each property requires a dedicated method
   - Additional `Build()` call required

3. **Maintenance Burden**
   - Two classes to maintain instead of one (Settings + SettingsBuilder)
   - Twice the XML documentation
   - Twice the unit tests

4. **Breaking Change**
   - Would require deprecating existing API
   - Migration period needed
   - User confusion during transition

### When Builder Pattern Would Make Sense

Builder Pattern would be beneficial if any of these apply:
- Complex validation logic spanning multiple properties
- Conditional construction based on combinations of values
- Immutable objects with many required parameters
- Construction order matters
- Multiple construction strategies needed

**None of these apply to Settings class.**

## Consequences

### Positive

1. **Keep existing, proven API** - No migration needed
2. **Consistency with .NET** - Follows framework conventions
3. **Lower maintenance** - Fewer classes to maintain
4. **Better user experience** - Familiar patterns, no learning curve
5. **Simpler codebase** - YAGNI (You Aren't Gonna Need It) principle

### Negative

1. **No fluent interface** - Some developers prefer method chaining
   - Mitigation: Object initializer syntax is also fluent
2. **Mutable object** - Properties can be changed after construction
   - Mitigation: This is intentional for flexibility; validation in setters prevents invalid states

## Alternatives Considered

### 1. Immutable Settings with Constructor

```csharp
public class Settings
{
    public Settings(
        string fileName = "appsettings",
        string configMountPath = "config",
        string hostingName = "Development",
        bool optional = true)
    {
        // ...
    }
}
```

**Rejected**: Too rigid, makes Action<Settings> pattern impossible, harder to extend.

### 2. Record Type with Init Properties (C# 9+)

```csharp
public record Settings
{
    public string FileName { get; init; } = "appsettings";
    // ...
}
```

**Partially Adopted**: Settings is now a record type (with IsExternalInit polyfill for net48/netcoreapp3.1), but uses regular setters instead of `init` properties.

Reasoning:
- **Record adopted**: Provides value-based equality and `with` expressions
- **Init rejected**: Code modifies properties after construction (e.g., `setting.Key = key`), which `init` blocks
- **Validation works**: Setters allow validation while maintaining flexibility
- **Best of both**: Value semantics from record + mutability from setters

This hybrid approach combines record benefits (equality, with expressions) with setter flexibility (post-construction modification, validation).

### 3. Hybrid Approach (Builder + Action)

```csharp
config.AddMountConfiguration(settings =>
{
    settings.FileName = "database";
    // OR
    settings.UseBuilder(b => b.WithFileName("database"));
});
```

**Rejected**: Adds complexity without clear benefits, confuses users with two APIs.

## Implementation Plan

No implementation needed - decision is to maintain current approach.

Future considerations:
- If Settings grows to 10+ properties, reconsider Builder Pattern
- If complex validation rules emerge, reconsider immutability
- Monitor user feedback for API usability

## References

- [Microsoft: Options pattern in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [.NET API Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Effective C#: Prefer Action<T> to Custom Delegates](https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/september/csharp-effective-csharp-prefer-action-t-to-custom-delegates)

## Related ADRs

- [ADR-001: Extension Methods Organization](ADR-001-extension-methods-organization.md) - How Settings is consumed
- ADR-003: Configuration Structure (planned) - Overall configuration architecture
