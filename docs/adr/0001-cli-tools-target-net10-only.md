# ADR-0001: CLI Tools Target .NET 10 Only

**Date:** 2025-07-14  
**Status:** Accepted  
**Deciders:** Voyager-Poland team

---

## Context

The repository contains three standalone CLI tools:

| Project | Purpose |
|---|---|
| `Voyager.Configuration.Encrypt` | Encrypts a single value using legacy DES |
| `Voyager.Configuration.Decrypt` | Decrypts a single value using legacy DES |
| `Voyager.Configuration.Tool` | Full-featured AES-256-GCM encrypt/decrypt tool |

The main library (`Voyager.Configuration.MountPath`) is a multi-targeted NuGet package that supports `.NET Framework 4.8`, `.NET Core 3.1`, `.NET 6`, and `.NET 8` to maximise consumer compatibility.

The CLI tools were initially inheriting the same multi-targeting from the repository's `Directory.Build.props`. This caused build failures because their dependencies (`System.CommandLine 2.0.7`, top-level statements returning `Task`, `ImplicitUsings`) require at minimum C# 10 / .NET 6, while `net48` and `netcoreapp3.1` only support up to C# 9.

---

## Decision

**CLI tools are pinned to `net10.0` as their sole target framework and use `System.CommandLine 2.0.7` (stable).**

Each tool project explicitly sets:

```xml
<TargetFrameworks>net10.0</TargetFrameworks>
```

This overrides any multi-targeting configured in `Directory.Build.props`.  
The `System.CommandLine` stable release `2.0.7` is used — **not** any pre-release (beta) version.

---

## Rationale

### CLI tools are not libraries

The tools are standalone executables distributed and run by developers on a local machine or in a CI pipeline. They are **not consumed as NuGet packages** by other projects. There is no compatibility obligation to older runtimes.

### .NET 10 is the current release

.NET 10 is the latest release. Any developer workstation or CI agent running these tools is expected to have the .NET 10 SDK installed.

### `System.CommandLine` stable (2.0.7) — not beta

The previous code targeted `2.0.0-beta4.22272.1` whose API (`AddOption`, `SetHandler`, `aliases` constructor overloads) is incompatible with the stable `2.0.7` release. Using a beta package in production tooling is not acceptable. The stable API uses:

| Operation | Stable API |
|---|---|
| Add argument / option / subcommand | `command.Add(symbol)` |
| Set action | `command.SetAction(ParseResult => ...)` |
| Read value in handler | `parseResult.GetValue(symbol)` |
| Required option | `option.Required = true` |
| Default value | `option.DefaultValueFactory = _ => value` |
| Invoke | `rootCommand.Parse(args).InvokeAsync()` |

### `ImplicitUsings` and top-level programs require C# 10+

Both are enabled in the tool projects. Targeting `net10.0` exclusively avoids the need for `#if` guards or per-TFM language version overrides.

### Smaller, faster builds

Removing three extra TFMs (net48, netcoreapp3.1, net6.0) from each tool project cuts build and test time without losing any functionality.

---

## Consequences

### Positive

- Build succeeds without compiler errors.
- Code uses only stable, released NuGet packages.
- Projects can freely use `async/await` top-level programs, `ImplicitUsings`, nullable reference types, and the stable `System.CommandLine` 2.0.7 API.
- Consistent across all three tool projects.

### Negative / Trade-offs

- Developers on machines that only have .NET Framework or .NET Core 3.1 runtimes installed cannot run the CLI tools. This is considered acceptable because those runtimes are no longer supported by Microsoft.
- Any future requirement to run the tools on an older runtime would require revisiting this decision.

### Neutral

- The main library (`Voyager.Configuration.MountPath`) continues to multi-target all four frameworks. This ADR does **not** affect library targeting.

---

## Alternatives Considered

| Alternative | Why rejected |
|---|---|
| Keep `System.CommandLine beta4` | Pre-release in production tooling is not acceptable |
| Multi-target tools with `#if` guards | High complexity, no practical benefit for CLI executables |
| Use an older CLI parsing library compatible with net48 | Adds a new dependency; inconsistent with the rest of the tooling |
| Stay on net8.0 | net10.0 is current; no reason to stay behind |

---

## References

- [.NET 10 support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [System.CommandLine 2.0.7 release notes](https://github.com/dotnet/command-line-api/releases)
- `src/Voyager.Configuration.Tool/Voyager.Configuration.Tool.csproj` — reference project

**Date:** 2025-07-14  
**Status:** Accepted  
**Deciders:** Voyager-Poland team

---

## Context

The repository contains three standalone CLI tools:

| Project | Purpose |
|---|---|
| `Voyager.Configuration.Encrypt` | Encrypts a single value using legacy DES |
| `Voyager.Configuration.Decrypt` | Decrypts a single value using legacy DES |
| `Voyager.Configuration.Tool` | Full-featured AES-256-GCM encrypt/decrypt tool |

The main library (`Voyager.Configuration.MountPath`) is a multi-targeted NuGet package that supports `.NET Framework 4.8`, `.NET Core 3.1`, `.NET 6`, and `.NET 8` to maximise consumer compatibility.

The CLI tools were initially inheriting the same multi-targeting from the repository's `Directory.Build.props`. This caused build failures because their dependencies (`System.CommandLine 2.0.0-beta4`, top-level statements returning `Task`, `ImplicitUsings`) require at minimum C# 10 / .NET 6, while `net48` and `netcoreapp3.1` only support up to C# 9.

---

## Decision

**CLI tools are pinned to `net8.0` as their sole target framework.**

Each tool project explicitly sets:

```xml
<TargetFramework>net8.0</TargetFramework>
```

This overrides any multi-targeting configured in `Directory.Build.props`.

---

## Rationale

### CLI tools are not libraries

The tools are standalone executables distributed and run by developers on a local machine or in a CI pipeline. They are **not consumed as NuGet packages** by other projects. There is no compatibility obligation to older runtimes.

### .NET 8 is the current LTS release

.NET 8 is the Long-Term Support release (supported until November 2026). Any developer workstation or CI agent running these tools is expected to have the .NET 8 SDK installed.

### `System.CommandLine` requires modern C#

`System.CommandLine 2.0.0-beta4` uses features (e.g., generic `SetHandler`) that do not compile under `net48` or `netcoreapp3.1` with C# 9. Maintaining compatibility shims would add complexity with no real benefit.

### `ImplicitUsings` and top-level programs require C# 10+

Both are enabled in the tool projects. Targeting `net8.0` exclusively avoids the need for `#if` guards or per-TFM language version overrides.

### Smaller, faster builds

Removing three extra TFMs (net48, netcoreapp3.1, net6.0) from each tool project cuts build and test time without losing any functionality.

---

## Consequences

### Positive

- Build succeeds without compiler errors related to C# language version.
- Code is clean — no `#if NETFRAMEWORK` guards needed.
- Projects can freely use `async/await` top-level programs, `ImplicitUsings`, nullable reference types, and modern `System.CommandLine` APIs.
- Consistent with how `Voyager.Configuration.Tool` was already configured.

### Negative / Trade-offs

- Developers on machines that only have .NET Framework or .NET Core 3.1 runtimes installed cannot run the CLI tools. This is considered acceptable because those runtimes are no longer supported by Microsoft.
- Any future requirement to run the tools on an older runtime would require revisiting this decision.

### Neutral

- The main library (`Voyager.Configuration.MountPath`) continues to multi-target all four frameworks. This ADR does **not** affect library targeting.

---

## Alternatives Considered

| Alternative | Why rejected |
|---|---|
| Multi-target tools with `#if` guards | High complexity, no practical benefit for CLI executables |
| Use an older CLI parsing library compatible with net48 | Adds a new dependency; inconsistent with the rest of the tooling |
| Require developers to install .NET Framework SDK | Counterproductive — forces use of a deprecated runtime |

---

## References

- [.NET 8 LTS support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [System.CommandLine repo](https://github.com/dotnet/command-line-api)
- `src/Voyager.Configuration.Tool/Voyager.Configuration.Tool.csproj` — reference project already pinned to `net8.0`
