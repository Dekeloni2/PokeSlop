# TiltanTale

An Undertale-style RPG built in C# with MonoGame, where the bosses are the teachers of Tiltan College.

---

## Goal of the Game

You are a student at Tiltan College. Work your way up through the building, and at the end of each classroom is a teacher who will not let you pass without a lesson.

Every fight can end two ways. You can **beat the teacher into submission**, or you can **listen, engage, and earn their respect** until they let you through willingly. Both get you past the door.

The game remembers which you chose. Spare every teacher and you finish the course on good terms; fight your way through and you finish it alone. **The goal isn't just to reach the top of the building — it's to decide what kind of student you were on the way up.**

---

## Core Mechanics

### Turn-based fights with real-time dodging

A battle alternates between your turn (menu-driven, no time pressure) and the teacher's turn (real-time, dodge or take damage). Four options on your turn:

| Option | What it does |
|---|---|
| **FIGHT** | A marker sweeps across a bar — press `Z` to stop it. The closer to centre, the more damage. Miss the window entirely and the attack does nothing. |
| **ACT** | Teacher-specific interactions. Some raise MERCY, some just reveal character, and some provoke a reaction you weren't ready for. |
| **ITEM** | Use something from your inventory, usually to heal. |
| **SPARE** | Ends the fight peacefully — but only once MERCY is full. The option turns yellow when it will actually work. |

### MERCY

Every teacher has a hidden mercy value. The right ACT choices raise it; the wrong ones waste a turn. Fill it and SPARE becomes a real ending instead of a rejected gesture. This is the non-violent path, and it is always slower than simply attacking — that's the point.

### The dodging phase

On the teacher's turn the battle box becomes the arena and you control your soul inside it with the arrow keys. Each teacher has their own **bullet patterns**, and the patterns are the characterisation:

- **Yakir**, the C# teacher, doesn't attack so much as *teach*. His turns are a four-lesson curriculum — data types, conditionals, loops, and finally a program that runs against the battle box itself, resizing and dragging the arena while code rains down. His ultimate is the single pixel he has spent his whole career failing to draw.
- **Dor Ben Dor** sets a chess board and takes your free movement away, making you step one square at a time against pieces that hunt using real chess moves.

Getting hit costs HP and grants brief invulnerability frames. Run out and it's game over.

### Route tracking

Every resolved fight is recorded — who, and how. The run is classified as **Pacifist** (no kills), **Genocide** (no mercy), or **Neutral** (some of each), and the ending reacts to the whole run rather than the last fight.

---

## Controls

| Key | Overworld | Battle |
|---|---|---|
| **Arrow keys** | Walk | Move the menu cursor / move your soul while dodging |
| **Z** or **Enter** | Talk, interact, advance dialogue | Confirm, advance dialogue, strike on the FIGHT bar |
| **X** | — | Back out of a submenu |
| **Escape** | Quit | Quit |

Debug builds only:

| Key | Action |
|---|---|
| **B** | Start a test battle |
| **O** | Toggle the elevator door layer |

---

## Gameplay

You start in the entrance hall and move freely — movement is pixel-based, not tile-locked, so you walk rather than step.

Pressing `Z` at a doorway, sign, or person opens a dialogue box. The **elevator** works the same way: interact with the panel inside and a floor menu opens. Pick a floor and the doors shut, the lift judders upward with its own music, a bell rings, and the doors open somewhere new. Walking out of them takes you to that floor.

Behind a classroom door is a teacher. The fight starts, and from there it's the turn loop above — pick an action, survive the reply, repeat. Teachers escalate: Yakir's lessons run in a fixed order and get harder, and his final attack only comes out once he has taught everything he knows.

When the fight ends the teacher either crumbles to dust or freezes and fades, depending on how you finished it — and the run tracker quietly writes down which.

---

## How the Project Is Organized

```
FinalProject/
├── Core/            Engine-level systems, no game rules
│   ├── StateMachine/    Game state stack (push/pop/replace)
│   ├── Graphics/        Sprites, spritesheets, animation, camera
│   ├── Audio/           Sound effects, music, looping SFX
│   ├── Input/           Keyboard snapshot, edge-triggered key presses
│   └── Text/            Typewriter effect and word wrapping
├── States/          The screens: menu, overworld, battle, game over
├── Battle/          Everything that happens inside a fight
│   └── Patterns/        One class per attack, all implementing IBulletPattern
├── World/           Tile maps, map loading, warps, the elevator
├── Entities/        The player
├── Data/            Plain data objects and the JSON loaders that build them
├── Events/          Event definitions for the event bus
├── UI/              Dialogue boxes, speech bubbles, HUD, choice menus
└── Content/         Assets and data files
    ├── Teachers/        One JSON file per teacher
    ├── Maps/            Tiled .tmj maps plus transition/warp definitions
    ├── Sprites/  Audio/  Fonts/  Items/
```

### Two ideas the structure is built around

**Teachers are data, not code.** A teacher is a JSON file — stats, sprite layout, dialogue, ACT options, moves, and which bullet pattern each move uses. `TeacherLoader` turns that into a `TeacherStats`, and `PatternRegistry` maps a pattern name from the file to the class that implements it. Adding a teacher means writing a JSON file and, only if they need a brand-new attack, one new pattern class.

**Sequences own themselves.** Anything that takes over input and runs through its own phases — the elevator, a battle's dodge phase — lives in its own class with a small interface back to whoever is hosting it. States decide *which* sequence runs; they don't implement it.

---

## Core Classes and Their Responsibilities

### Engine

| Class | Responsibility |
|---|---|
| `Game1` | Entry point. Loads assets, registers sprites and sounds, owns `PlayerData` and `RouteTracker`. |
| `GameStateManager` | A stack of game states with push / pop / replace, so a battle can sit on top of the overworld and return to it. |
| `GameState` | Base class for a screen. `OnEnter` / `OnExit` / `Pause` / `Resume` / `Update` / `Draw`. |
| `InputManager` | One keyboard snapshot per frame, distinguishing "held" from "just pressed". |
| `SpriteManager` | Named sprite lookup, so nothing loads textures by path at draw time. |
| `SoundManager` | Sound effects, music, and looping SFX behind names rather than files. |
| `EventBus` | Publish/subscribe keyed by event type. Used where a publisher genuinely shouldn't know its listeners. |
| `GameSettings` | Window size, zoom, movement speed, and other tuning constants in one place. |

### States

| Class | Responsibility |
|---|---|
| `OverworldState` | Loads maps, moves the player, checks transitions, warps, and interactables. Decides which sequence starts. |
| `BattleState` | The turn machine: menu → action → enemy turn → feedback → repeat, plus the ending sequence. |
| `BattleTransition` | The Undertale-style flash and soul-drop that hands off from the overworld to a fight. |
| `GameOverState` | Death screen and retry. |

### Battle

| Class | Responsibility |
|---|---|
| `Teacher` | A live opponent in a fight — current HP, mercy progress, spared/alive state. |
| `IBulletPattern` | The interface every attack implements: a duration, a `Start`, and an `Update`. |
| `DodgePhase` | Runs one enemy turn — updates the pattern, moves the soul, resolves collisions and damage. |
| `DodgeContext` | The narrow surface a pattern is allowed to touch. Patterns can spawn hazards and shake the camera; they cannot reach the player's save data. |
| `PatternRegistry` | Maps a pattern name in JSON to the class that implements it. |
| `AttackMinigame` | The FIGHT timing bar and the accuracy it produces. |
| `ActionMenu` | The paged, two-column option lists and result text inside the battle box. |
| `TeacherSprite` | Multi-part procedural animation — each body part bobs on its own sine wave — plus hurt, dust, and freeze effects. |

### Data

| Class | Responsibility |
|---|---|
| `TeacherStats` | Everything defining a teacher, built from JSON. |
| `TeacherLoader` | Parses a teacher file into `TeacherStats`. |
| `MoveData` | One enemy move: its pattern, and any authored dialogue beats for it. |
| `ActOption` | One ACT entry — its text, mercy value, and optionally an attack it provokes. |
| `PlayerData` | HP, attack, gold, inventory. Publishes an event when HP changes. |
| `RouteTracker` | Listens for resolved fights and classifies the run as pacifist, neutral, or genocide. |

### World

| Class | Responsibility |
|---|---|
| `TileMap` | A loaded map: layers, collision, interactables, the elevator door layer. |
| `MapLoader` | Reads Tiled `.tmj` files into a `TileMap`. |
| `ElevatorSequence` | The lift: floor menu, doors, ride, judder, and arrival. Reaches the overworld only through `IElevatorHost`. |
| `Player` | Free pixel-based movement with per-axis collision sliding, and walk animation. |

---

## Building and Running

Requires the **.NET SDK** and the **MonoGame Content Builder**.

```bash
dotnet build
```

```bash
dotnet run --project FinalProject
```

Content is built automatically from `FinalProject/Content/Content.mgcb` as part of the build.

---

## Current State

The battle system is complete and the teachers are data-driven. Two teachers are implemented — **Yakir** (five attacks including a full four-lesson arc) and **Dor Ben Dor** (chess and cloverbyte) — alongside a debug "Substitute" used for testing patterns in isolation.

The overworld supports free movement, map transitions, warps, interactables, dialogue, and a working elevator between floors.

Still to come: classroom doors that start real fights (battles currently begin from the debug `B` key), the remaining teachers, and the ending that reads the route tracker.
