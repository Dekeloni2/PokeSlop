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
- **David**, The math teacher, uses both his body and shapes to attack you. 
- **Dor Ben Dor** uses objects that are close to him or to his profession. Such as a Chess board, a boat that he fought over with his sister, or cloverbyte. Each time he uses those attacks, they evolve and become stronger and harder.

Getting hit costs HP and grants brief invulnerability frames. Run out and it's game over.

### Route tracking

Every resolved fight is recorded — who, and how. The run is classified as **Pacifist** (no kills), **Genocide** (no mercy), or **Neutral** (some of each), and the ending reacts to the whole run rather than the last fight.

---

## Controls

| Key | Overworld | Battle |
|---|---|---|
| **Arrow keys** | Walk | Move the menu cursor / move your soul while dodging |
| **Z** or **Enter** | Talk, interact, advance dialogue, confirm a menu | Confirm, advance dialogue, strike on the FIGHT bar |
| **X** or **C** | Back out of a submenu | Back out of a submenu |
| **C** | Open the pause menu (settings) | — |
| **Escape** | Hold ~1.2s to quit | Hold ~1.2s to quit |
| **F11** | Toggle fullscreen | Toggle fullscreen |

Debug builds only:

| Key | Action |
|---|---|
| **B** | Start a test battle |
| **O** | Toggle the elevator door layer |
| **V** | Open the vending machine from anywhere |
| **1–8** | Jump straight to one of the eight ending scripts, for previewing them without playing the whole route |

---

## Gameplay

The game opens on a **main menu** (Begin Game / Settings). Starting a run fades to white and drops you in the entrance hall, where movement is free and pixel-based rather than grid-locked. The very first time you reach the main hall, an Undertale-style title-card sting plays once (`CreditsIntroState`) before control hands over.

Pressing `Z` at a doorway, sign, or person opens a dialogue box. The **elevator** works the same way: interact with the panel inside and a floor menu opens. Pick a floor and the doors shut, the lift judders upward with its own music, a bell rings, and the doors open somewhere new. Walking out of them takes you to that floor.

A **classroom door is a boss gate**: walk into it and it asks whether you want to go in, showing a line that already hints at the route you're on. Accept and the screen flashes to a battle intro, your soul drops into the box, and the fight starts for real — it isn't a stand-in, it's the same `BattleState` a stray NPC bump uses. Gates also enforce **prerequisites** (`Requires` in the teacher's JSON): a locked door shows its own line until whatever it depends on has been resolved. Once a teacher is resolved, walking back into their door just shows a short reaction line instead of re-starting the fight.

From there it's the turn loop above — pick an action, survive the reply, repeat. Teachers escalate: Yakir's lessons run in a fixed order and get harder, and his final attack only comes out once he has taught everything he knows.

Losing a fight doesn't end the run: `GameOverState` fades in on a random line from `gameover.json`, then revives you back in the overworld to try again. Winning one crumbles the teacher to dust or freezes and fades him, depending on how you finished it, and the run tracker quietly writes down which.

Once every teacher on the floor is resolved, reaching the end of the run plays `EndingState` — one of eight scripted endings (all authored in `Content/endings.json`), chosen by exactly who was killed and who was spared, with a phone call scripted per teacher and a closing title card.

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
├── Entities/        The player, NPCs
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
| `MainMenuState` | The title screen: Begin Game / Settings, and the fade into the first run. |
| `OverworldState` | Loads maps, moves the player, checks transitions, warps, boss gates, and interactables. Decides which sequence starts. |
| `CreditsIntroState` | The one-time title-card sting played the first time the player reaches the main hall. |
| `BattleTransition` | The Undertale-style flash and soul-drop that hands off from the overworld to a fight. |
| `BattleState` | The turn machine: menu → action → enemy turn → feedback → repeat, plus the yield/mercy sequence. |
| `GameOverState` | Death screen: a random line from `gameover.json`, then revives the player back in the overworld. |
| `EndingState` | Plays one of the eight scripted endings from `Content/endings.json`, picked by who was killed/spared, and closes on the title card. |

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
| `RouteTracker` | Listens for resolved fights (via `EventBus`) and classifies the run as pacifist, neutral, or genocide. |
| `ItemData` / `ItemLoader` | The item catalog, loaded once from `Content/Items/items.json` and shared by the shop, the starting inventory, and teacher item rewards. |
| `EndingConfig` | Picks which of the eight ending scripts fits the run and loads its lines from `Content/endings.json`. |
| `GameOverConfig` | Loads the random game-over lines from `Content/gameover.json`. |

### World

| Class | Responsibility |
|---|---|
| `TileMap` | A loaded map: layers, collision, interactables, the elevator door layer. |
| `MapLoader` | Reads Tiled `.tmj` files into a `TileMap`. |
| `ElevatorSequence` | The lift: floor menu, doors, ride, judder, and arrival. Reaches the overworld only through `IElevatorHost`. |
| `Player` | Free pixel-based movement with per-axis collision sliding, and walk animation. |

### UI

| Class | Responsibility |
|---|---|
| `DialogueBox` | The bottom-of-screen box for signs, NPCs, and boss-gate prompts, with a typewriter effect. |
| `SpeechBubble` | The teacher's own in-battle speech, anchored above him rather than docked to the screen edge. |
| `BattleHud` | The player's HP bar; subscribes to `PlayerHpChangedEvent` rather than being told directly. |
| `OverworldMenu` | The `C`-menu opened from the overworld: settings, shared with the main menu's Settings screen. |
| `VendingMachineMenu` | The shop: browse the item catalog, spend gold, add to the inventory. |

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

The battle system is complete and the teachers are data-driven. Three teachers are implemented — **Yakir** (five attacks including a full four-lesson arc), **David** (hexagons, punch) and **Dor Ben Dor** (chess, cloverbyte, boat, napoleon) — alongside a training dummy that teaches the blue/orange dodge rules and a debug "Substitute" used for testing patterns in isolation.

The full loop is wired end to end: main menu → overworld → a classroom door opens a real fight (with locked/spared/killed reactions and prerequisites between teachers) → win or lose → one of eight endings once the run is over, or a revive back into the overworld on defeat. The overworld also supports free movement, map transitions, warps, interactables, dialogue, a working elevator between floors, and a shop.

Still to come: more floors/teachers beyond the current three, and further balancing of the mercy/ACT text now that the full loop is playable.
