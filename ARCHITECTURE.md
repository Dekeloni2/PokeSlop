# Architecture Deep Dive

This is the extended version of the "Core Classes" section in the [README](README.md) — every notable class in the codebase, not just the ones central to the design. Split out here so the README stays focused on what the assignment asks for, without losing the detail for anyone (including future me) who wants the fuller picture.

---

## Projects

| Project | What it is |
|---|---|
| `FinalProject` | The game — every class below. A library, referencing only the KNI framework packages and no platform backend, which is what lets both heads share it. |
| `TiltanTale.DesktopGL` | Desktop head: an entry point, the icon and manifest, and `DesktopTiltanTaleGame`. Backed by SDL2/OpenGL. |
| `TiltanTale.Blazor` | Browser head: a Blazor WebAssembly page whose canvas is the game, and `BrowserTiltanTaleGame`. Backed by WebGL. Drives `Game.Tick()` from `requestAnimationFrame`, because a browser tab can't have its event loop blocked by `Game.Run()`. |

`FinalProject/Content` is shared: each head builds `Content.mgcb` for its own platform, and both import `Content/RawContent.props` for the JSON and Tiled files that skip the pipeline.

---

## Core Classes and Their Responsibilities

### Engine

| Class | Responsibility |
|---|---|
| `Game1` | The game proper. Loads assets, registers sprites and sounds, owns `PlayerData` and `RouteTracker`. Contains nothing platform-specific; `CanExitToDesktop` and `CanToggleFullscreen` are the two seams each head answers. |
| `ContentFiles` | Reads the hand-authored data files (maps, teachers, items, endings) through `TitleContainer` — a file read on desktop, a synchronous HTTP fetch in the browser. The reason no loader had to become async to run on the web. |
| `ContentPaths` | Builds those relative, forward-slash paths and folds `.`/`..` in string space, because WebAssembly has no working directory for `Path.GetFullPath` to resolve against. |
| `GameLog` | Where a content-load failure goes: `map_debug.txt` next to a windowed exe that has no console, and the browser devtools console. |
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
| `MapLoader` | Reads Tiled `.tmj` files into a `TileMap`, including its `Interactables` and `NPCs` object layers. |
| `Interactable` | One object from a map's `Interactables` layer — a tile footprint, some text, and an `Action` string (`"shop"`, `"warp"`, `"toilet"`, `"cheese"`, `"keypad"`, or nothing at all for a plain sign). `OverworldState.TryInteract` dispatches on `Action`, so a whole new piece of content — a flushable toilet, a pickable item, a keypad that's always wrong — is a `case` label and a JSON property, not a new subsystem. |
| `MapWarp` | An on-step trigger, distinct from `Interactable`: no facing or button press, just standing on the tile. Used for anything you should be able to walk straight through — a doorway between rooms, the elevator's own entrance from any floor. |
| `ElevatorSequence` | The lift: floor menu, doors, ride, judder, and arrival. Reaches the overworld only through `IElevatorHost`. Currently serves three floors — the entrance, Tiltan Hall, and Floor 2. |
| `Player` | Free pixel-based movement with per-axis collision sliding, and walk animation. |

### UI

| Class | Responsibility |
|---|---|
| `DialogueBox` | The bottom-of-screen box for signs, NPCs, and boss-gate prompts, with a typewriter effect. |
| `SpeechBubble` | The teacher's own in-battle speech, anchored above him rather than docked to the screen edge. |
| `ChoiceBox` | The soul-cursor Yes/No box. One instance, shared by every yes/no prompt in the overworld — a boss gate's "enter his class?", the toilet's "flush it?", the cheese's "pick it up?", the keypad's "type code in?" — each routed to its own handler by a dispatch flag rather than one box per feature. |
| `KeypadEntry` | A four-digit code dialer: pick a digit, spin it 0–9, confirm. Backs the Floor 2 keypad, which never accepts a correct code no matter what's dialed in. |
| `BattleHud` | The player's HP bar; subscribes to `PlayerHpChangedEvent` rather than being told directly. |
| `OverworldMenu` | The `C`-menu opened from the overworld: settings, shared with the main menu's Settings screen. |
| `VendingMachineMenu` | The shop: browse the item catalog, spend gold, add to the inventory. Items can be marked `hidden` to exist (found, given as a reward) without ever being sold. |
