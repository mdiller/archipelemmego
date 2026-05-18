# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ArchipeLemmeGo is a Discord bot for organizing [Archipelago](https://archipelago.gg/) randomizer multiplayer sessions. It connects Discord channels to Archipelago rooms, lets players register their slots, and tracks item dependencies via dependency trees rendered as PNG graphs.

## Build & Run

```bash
# Build
dotnet build ArchipeLemmeGo/ArchipeLemmeGo.csproj

# Run locally
dotnet run --project ArchipeLemmeGo/

# Publish release
dotnet publish ArchipeLemmeGo/ArchipeLemmeGo.csproj -c Release
```

No test project exists in this repo.

Docker is for deployment only — never run Docker commands during development.

## Architecture

### Entry Point & DI Wiring

`Program.cs` creates a `BotManager` which registers all services via `Microsoft.Extensions.DependencyInjection`. `InteractionHandler` handles slash command discovery and error routing (wrapping `UserError` exceptions into user-visible messages).

### Three-Layer Structure

**Bot layer** (`Bot/`) — Discord interaction logic, slash command modules:
- `BotCommands.cs` — `/setuproom`, `/register`
- `ItemCommands.cs` — `/item todo|waiting|list|show`
- `DependancyCommands.cs` — `/dep add|addregex|show`
- `DepTreeNode.cs` + `DependancyTree.cs` — tree building logic

**Archipelago layer** (`Archipelago/`) — wraps `Archipelago.MultiClient.Net`:
- `ArchipelagoLowLevelClient` — raw socket connection
- `ArchipelagoClient` — per-slot connection with item/location data
- `ArchipelagoContext` — resolves the active room+slot from a Discord channel/user
- `ArchipelagoService` — handles registration flow

**Datamodel layer** (`Datamodel/`) — persistence and domain models:
- `InfoService` — reads/writes JSON via Newtonsoft.Json to `resources/info/{Category}/{Id}.json`
- `InfoUri` — typed URI scheme: `Type:Identifier` (e.g. `RoomInfo:W855...`)
- `InfoBase` — base class for all serializable info objects
- `Infos/` — `RoomInfo`, `SlotInfo`, `ChannelLinker`, `DependancyLink`, `RequestedHintInfo`
- `Arch/` — `ArchItem`, `ArchLocation` (Archipelago API entities)

### Graph Rendering

`TreeRenderer/TreeRenderer.cs` uses `Microsoft.Msagl` for layout and `SkiaSharp` for rasterization, outputting PNG images sent back to Discord.

### Data Persistence

All state is stored as JSON under `resources/info/`. The `ChannelLinker` maps Discord channel IDs → room seeds. `RoomInfo` contains the full room state including player slots, hints, and dependency links.

### Bot Configuration

`Bot/BotInfo.cs` contains the bot token, owner ID, and test guild ID. These are currently hardcoded — do not commit changes that expose or rotate the token publicly.

### Error Handling Pattern

Throw `UserError` (with a message string) anywhere in the command pipeline to surface a clean error to the Discord user. All other exceptions are caught by `InteractionHandler` and logged.
