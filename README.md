# TiltanTale

An Undertale-style RPG built in C# with [KNI](https://github.com/kniEngine/kni) (a MonoGame-compatible fork), where the bosses are the teachers of Tiltan College. It runs both as a desktop window and in a browser, from the same game code.

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
- **David**, the math teacher, uses both his body and shapes to attack you — onion-ring waves, a hexagon barrage, a heavy punch he leaps off screen and back in to throw. His ultimate puts a song on and layers all three on top of it at once, with a real crouch-and-launch exit animation before it starts.
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
| **B** | Start a test battle with an assigned teacher at StartDebugBattle() |
| **O** | Toggle the elevator door layer (To test if the layering works) |
| **V** | Open the vending machine from anywhere |
| **1–8** | Jump straight to one of the eight ending scripts, for previewing them without playing the whole route |

---

## Gameplay

The game opens on a **main menu** (Begin Game / Settings). Starting a run fades to white and drops you in the entrance hall, where movement is free and pixel-based rather than grid-locked. The very first time you reach the main hall, an Undertale-style title-card sting plays once (`CreditsIntroState`) before control hands over.

Pressing `Z` at a doorway, sign, or person opens a dialogue box. The **elevator** works the same way: interact with the panel inside and a floor menu opens. Pick a floor and the doors shut, the lift judders upward with its own music, a bell rings, and the doors open somewhere new. Walking out of them takes you to that floor.

A **classroom door is a boss gate**: walk into it and it asks whether you want to go in, showing a line that already hints at the route you're on. Accept and the screen flashes to a battle intro, your soul drops into the box, and the fight starts for real — it isn't a stand-in, it's the same `BattleState` a stray NPC bump uses. Gates also enforce **prerequisites** (`Requires` in the teacher's JSON): a locked door shows its own line until whatever it depends on has been resolved. Once a teacher is resolved, walking back into their door just shows a short reaction line instead of re-starting the fight.

From there it's the turn loop above — pick an action, survive the reply, repeat. Teachers escalate: Yakir's lessons run in a fixed order and get harder, and his final attack only comes out once he has taught everything he knows.

Losing a fight doesn't end the run: `GameOverState` fades in on a random line from `gameover.json`, then revives you back in the overworld in the spot they entered the classroom to try again. Winning one crumbles the teacher to dust or freezes and fades him, depending on how you finished it, and the run tracker quietly writes down which. The player can attempt to enter the room again after the fight, to get a certain message depending on what ending they chose

Once every teacher on the floor is resolved, reaching the end of the run plays `EndingState` — one of eight scripted endings (all authored in `Content/endings.json`), chosen by exactly who was killed and who was spared, with a phone call scripted per teacher and a closing title card.

---

## How the Project Is Organized

The solution is one game library with two thin platform heads on top of it:

```
FinalProject/           The whole game. Knows nothing about where it runs.
TiltanTale.DesktopGL/   Desktop head — a window, via SDL2/OpenGL
TiltanTale.Blazor/      Browser head — a WebGL canvas, via WebAssembly
```

Between them the heads contribute an entry point, an icon, a page, and the
answers to two questions (`CanExitToDesktop`, `CanToggleFullscreen`) — no game
logic and no duplicated content. See [Building and Running](#building-and-running).

```
FinalProject/
├── Core/            Engine-level systems, no game rules
│   ├── StateMachine/    Game state stack (push/pop/replace)
│   ├── Graphics/        Sprites, spritesheets, animation, camera
│   ├── Audio/           Sound effects, music, looping SFX
│   ├── Input/           Keyboard snapshot, edge-triggered key presses
│   ├── Text/            Typewriter effect and word wrapping
│   └── ContentFiles     Reads data files the same way on disk and over HTTP
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

**The platform is a head, not a branch.** There is no `#if BLAZOR` anywhere in the game. Where the two targets genuinely differ, `Game1` asks a question a subclass answers — a browser tab can't close itself or take over the screen, so `BrowserTiltanTaleGame` says so, and both the quit timer and the instructions screen follow from that one answer. Reading data files is the same story: every loader goes through `ContentFiles`, which is a file read on desktop and an HTTP fetch in the browser, and neither the loaders nor the maps know which.

---

## Core Classes and Their Responsibilities

| Class | Responsibility |
|---|---|
| `Game1` | The game proper. Loads assets, registers sprites and sounds, owns `PlayerData` and `RouteTracker`. Platform-independent — each head subclasses it (`DesktopTiltanTaleGame`, `BrowserTiltanTaleGame`). |
| `ContentFiles` | The one door every hand-authored data file goes through. A file read on desktop, an HTTP fetch in the browser, one call either way. |
| `GameStateManager` | A stack of game states with push / pop / replace, so a battle can sit on top of the overworld and return to it. |
| `OverworldState` | Loads maps, moves the player, and checks transitions, warps, boss gates, and interactables. |
| `BattleState` | The turn machine: menu → action → enemy turn → feedback → repeat, plus the yield/mercy sequence. |
| `IBulletPattern` | The interface every attack implements: a duration, a `Start`, and an `Update`. One class per attack. |
| `DodgePhase` | Runs one enemy turn — updates the pattern, moves the soul, resolves collisions and damage. |
| `TeacherLoader` / `TeacherStats` | Parses a teacher's JSON file (stats, dialogue, moves) into the data the rest of the game reads. |
| `PatternRegistry` | Maps a pattern name in a teacher's JSON to the class that implements it. |
| `RouteTracker` | Listens for resolved fights (via `EventBus`) and classifies the run as pacifist, neutral, or genocide. |
| `EventBus` | Publish/subscribe keyed by event type, used where a publisher genuinely shouldn't know its listeners (e.g. the HP bar reacting to damage). |
| `TileMap` / `MapLoader` | A loaded map and the Tiled `.tmj` reader that builds it — layers, collision, interactables. |
| `Interactable` | One object from a map's `Interactables` layer: a tile footprint, some text, and an `Action` string (`"shop"`, `"warp"`, `"toilet"`, etc.) that `OverworldState` dispatches on. |
| `ElevatorSequence` | The lift: floor menu, doors, ride, and arrival, reaching the overworld only through a small `IElevatorHost` interface. |
| `ChoiceBox` | The soul-cursor Yes/No box, shared by every yes/no prompt in the overworld rather than one box per feature. |

A full class-by-class breakdown (every class, not just the central ones) is in [ARCHITECTURE.md](ARCHITECTURE.md).

---

## Building and Running

Requires the **.NET 8 SDK**. The content builder comes in as a NuGet package, so there's nothing else to install.

**Desktop:**

```bash
dotnet run --project TiltanTale.DesktopGL
```

**Browser** (then open the printed `localhost` URL):

```bash
dotnet run --project TiltanTale.Blazor
```

**Publish the web build** to a folder you can upload anywhere that serves static files — itch.io, GitHub Pages, any web host:

```bash
dotnet publish TiltanTale.Blazor -c Release -o publish-web
```

The site to upload is `publish-web/wwwroot`.

Content is built from `FinalProject/Content/Content.mgcb` as part of each head's build, compiled for that head's platform — `.xnb` next to the executable for desktop, `.xnb` under `wwwroot` for the browser (where the music is also transcoded to `.mp3`, since that's what browsers can decode). The hand-authored JSON and Tiled maps skip the pipeline entirely and are listed once in `FinalProject/Content/RawContent.props`, which both heads import.

### Notes on the browser build

- **Everything is fetched over HTTP.** WebAssembly has no filesystem, so `ContentFiles` reads through KNI's `TitleContainer`, which is a synchronous `XMLHttpRequest` in the browser and a plain file read on desktop. That's what let every loader stay synchronous instead of turning the whole game async.
- **A map's optional sidecar files 404 when absent.** `entrance.transitions.json` and `entrance.bossgates.json` don't exist, and asking for them is how the game finds that out — there's no cheap "does this exist" over HTTP. It's handled as "there are none", exactly as the old `File.Exists` check was; the 404s in devtools are expected, not errors.
- **The page owns the frame loop.** `requestAnimationFrame` calls into `Index.razor.cs`, which advances the game one `Tick()` per frame. `Game.Run()` can't block a browser tab the way it blocks on desktop.

---

## Current State

The battle system is complete and the teachers are data-driven. Three teachers are implemented — **Yakir** (five attacks including a full four-lesson arc), **David** (onion, hexagon, punch, and a song-driven ultimate) and **Dor Ben Dor** (chess, cloverbyte, boat, napoleon) — alongside a training dummy that teaches the blue/orange dodge rules and a debug "Substitute" used for testing patterns in isolation.

The full loop is wired end to end and the game is functionally complete: main menu → overworld → a classroom door opens a real fight (with locked/spared/killed reactions and prerequisites between teachers) → win or lose → one of eight endings once the run is over, or a revive back into the overworld on defeat. The overworld also supports free movement, map transitions, warps, interactables, dialogue, a shop, and a **three-floor elevator** connecting the entrance, Tiltan Hall, and Floor 2.

Two rooms exist purely as side content, built to prove out the `Interactable`/`MapWarp` systems beyond the main path: a **bathroom** off Tiltan Hall with three usable stalls (one of which is just a sign, uselessly), and **Floor 2**, home to a keypad that authoritatively rejects every code you dial into it. Both are walk-in-and-explore rather than gated behind progress. Tiltan Hall also hides one genuine secret: a wedge of cheese sitting out in plain sight, a leftover placeholder that got a joke about itself and a real 99-HP item instead of being deleted.
