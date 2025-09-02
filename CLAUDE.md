# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HT.ResourceMapper is a cloud resource management application that serves as a sophisticated, searchable catalog of resources across multiple cloud environments. The system combines organizational capabilities with advanced tagging, metadata management, and dependency tracking - conceptually similar to an enhanced enterprise-grade bookmark manager for cloud resources.

## Architecture

### Solution Structure
This is a .NET 9.0 solution organized into four main areas:

- **Libraries/**: Shared utility libraries (HT.* namespace)
  - `ApiContracts`: API contract definitions
  - `Collections`: Collection utilities
  - `Configuration`: IConfiguration extensions with type-safe access
  - `Logging`: ILogger extensions
  - `SqlClient`: SqlClient abstractions and extensions

- **Database/**: SQL Server database project
  - `HTServices`: Database schema, tables, stored procedures, and seed scripts
  - Uses Microsoft.Build.Sql SDK for database project management

- **Modules/**: Feature modules organized as Client/Server/Shared triplets
  - `Common`: Shared common functionality
  - `Feature1`: Example feature implementation
  - Each module follows Blazor architecture patterns

- **UI/**: User interface layer
  - `ResourceMapper.UI.Client`: Blazor WebAssembly client
  - `ResourceMapper.UI.Server`: ASP.NET Core server hosting the client

### Core Domain Model
The application centers around six primary entities:
1. **Resources** - The main entities being managed (websites, databases, gateways, etc.)
2. **Resource Types** - Extensible catalog of resource classifications
3. **Advanced Tagging System** - Multi-valued tags with type constraints and custom values
4. **Resource Dependencies** - Bidirectional "depends on"/"depended by" relationships
5. **Tag Content Types** - Extensible content type system (Text, Link, etc.)
6. **Resource Type Tags** - Template system for pre-defined tag schemas per resource type

## Development Commands

### Build and Test
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build UI/BlazorTemplate.UI.Server

# Build in Release mode
dotnet build -c Release
```

### Database Operations
```bash
# Build database project
dotnet build Database/HTServices/HTServices.sqlproj
```

### Running the Application
```bash
# Run the server (which hosts the Blazor WebAssembly client)
dotnet run --project UI/ResourceMapper.UI.Server
```

## Key Technologies

- **.NET 9.0** with C# 13 features
- **Nullable reference types** enabled across all projects
- **Blazor WebAssembly** with ASP.NET Core server
- **SQL Server** with database project using Microsoft.Build.Sql SDK
- **Serilog** for structured logging
- **Microsoft.AspNetCore.Components.WebAssembly.Server** for hosting

## Configuration

The application uses a sophisticated configuration system via the HT.Microsoft.IConfiguration.Extensions library that provides:

- Type-safe configuration access (GetString, GetInt, GetBool, etc.)
- Array and collection support with customizable separators
- Strongly-typed configuration sections via GetTypedSection<T>()
- Environment detection helpers (IsDevelopment, IsProduction, etc.)
- Configuration validation with ValidateRequiredKeys()
- Enhanced connection string management

## Project Conventions

### Naming Patterns
- Libraries use `HT.` prefix (e.g., HT.ApiContracts)
- Modules use `ResourceMapper.ModuleName.Layer` pattern
- UI projects use `ResourceMapper.UI.Layer` pattern

### Project Structure
- Each module follows Client/Server/Shared architecture
- Libraries are organized by functional area
- Database schema organized under ResourceMapper namespace
- Seed scripts numbered sequentially (01_, 02_, etc.)

### Dependencies
- UI.Server references UI.Client and feature modules
- Feature modules reference shared libraries
- All projects target net9.0 with ImplicitUsings and Nullable enabled

## Development Notes

- The solution uses .slnx format (Visual Studio solution file)
- User secrets are configured for the UI.Server project
- Serilog is configured with file sink in UI.Server
- The project includes comprehensive project notes in `__ProjectNotes/` directory with architectural concepts and user stories