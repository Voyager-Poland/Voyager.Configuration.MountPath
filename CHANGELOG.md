# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Build infrastructure** - Centralized build configuration using Directory.Build.props
  - `Directory.Build.props` - Main configuration file applying to all projects
  - `build/Build.Versioning.props` - MinVer 7.0.0 for automatic versioning from Git tags
  - `build/Build.CodeQuality.props` - Code analyzers, TreatWarningsAsErrors, deterministic builds
  - `build/Build.SourceLink.props` - SourceLink for GitHub integration
  - `build/Build.NuGet.props` - NuGet package settings

- **Code style enforcement** - Added `.editorconfig` with comprehensive C# coding conventions
  - Naming conventions (interfaces, types, fields)
  - Formatting rules (braces, spacing, indentation)
  - Code style preferences (var usage, expression-bodied members, pattern matching)

### Changed

- **Versioning** - Switched from manual version in .csproj to MinVer automatic versioning
  - Version is now calculated from Git tags (format: `v1.2.3`)
  - Assembly version uses major version only for binary compatibility
  - Pre-release builds automatically get `preview` suffix

- **Company name** - Updated from "Voyager.com sp. z o.o." to "Sindbad IT sp. z o.o."

- **Project files simplified** - Removed duplicated settings now inherited from Directory.Build.props
  - Voyager.Configuration.MountPath.csproj
  - Voyager.Configuration.Encrypt.csproj
  - Voyager.Configuration.Decrypt.csproj

### Fixed

- **Build compatibility** - Added NoWarn for known issues in existing code
  - SYSLIB0021 - DESCryptoServiceProvider obsolete warning (to be refactored later)
  - NU1701 - .NET Framework package compatibility in Owin test project
  - Disabled nullable for projects with legacy code

## [1.2.8] - Previous Release

- Last manually versioned release before MinVer adoption
