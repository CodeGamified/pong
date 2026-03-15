# Pong: Hello World

The first game in the **CodeGamified** series. It's Pong — but you don't use a keyboard to move your paddle. You **write code**.

## Concept

Players write Python scripts that control their paddle in real time. Scripts run at 20 ops/sec (simulation time), restarting from the top each time they finish. Variables persist across ticks, so you can track state and build strategy. Smarter scripts win.

Crank the time scale to 1000x and watch thousands of matches play out in seconds.

## Builtins

| Function | Returns |
|---|---|
| `get_ball_x()` | Ball X position |
| `get_ball_y()` | Ball Y position |
| `get_ball_vx()` | Ball X velocity |
| `get_ball_vy()` | Ball Y velocity |
| `get_paddle_x()` | Your paddle X |
| `get_paddle_y()` | Your paddle Y |
| `get_opponent_y()` | Opponent paddle Y |
| `set_target_y(y)` | Move paddle toward Y |

## Default Script

```python
ball_y = get_ball_y()
set_target_y(ball_y)
```

Two lines, ~8 instructions. Tracks the ball. Simple — but beatable.

## Architecture

```
PongBootstrap          → scene setup, wiring, match management
PongMatchManager       → scoring, serve, win conditions
PaddleProgram          → ProgramBehaviour subclass, tick-based execution
PongIOHandler          → bridges CUSTOM opcodes to game state
PongCompilerExtension  → registers Pong builtins with the compiler
PongCodeDebugger       → three-panel live view (SOURCE │ MACHINE CODE │ STATE)
PongAIController       → built-in AI opponents (Easy/Medium/Hard/Expert)
```

### Engine Submodule

The `engine/` folder is a shared Git submodule ([CodeGamified.Engine](Pong/Assets/engine/CodeGamified.Engine/README.md)) providing:

- **Python compiler** — subset of Python → AST → RISC-like bytecode
- **Code executor** — time-scale-aware, deterministic instruction execution
- **TUI system** — retro terminal UI (windows, rows, colors, animations)
- **Editor** — in-game code editor with cursor, scrolling, syntax awareness
- **Persistence** — Git-based save/load for player scripts
- **Procedural** — runtime mesh generation for game objects

### Execution Model

```
Python Source → PythonCompiler → Instructions[]
Instructions[] → CodeExecutor (20 ops/sec sim-time)
    → HALT at end → PC resets to 0, memory persists
    → CUSTOM_0+ opcodes → PongIOHandler → game state
```

Scripts are deterministic: identical results at 0.5x, 1x, 100x, or 1000x speed.

### Debugger

Each paddle gets a live three-panel debugger:

```
┌──────────────┬──────────────┬──────────────┐
│ SOURCE CODE  │ MACHINE CODE │ STATE        │
│  1 ball_y .. │ 0000: LOAD   │ R0: 4.20     │
│  2 set_ta .. │ 0001: STORE  │ R1: 0.00     │
│              │ 0002: CUSTOM │ FLAGS: None  │
└──────────────┴──────────────┴──────────────┘
```

Machine code scrolls infinitely — wrapping around from the last instruction back to 0, matching the tick-based loop execution.

## Project Structure

```
Pong/Assets/
├── AI/                  AI difficulty levels
├── Audio/               Sound + haptics
├── Core/                Bootstrap, camera, time, warp
├── Game/                Ball, paddle, court, match, leaderboard
├── Persistence/         Script save/load via Git
├── Procedural/          Paddle, ball, court mesh blueprints
├── Scripting/           PaddleProgram, IO handler, compiler ext
├── UI/                  Code debugger, status bars, TUI manager
└── engine/              Shared CodeGamified submodule
    ├── CodeGamified.Engine/     Compiler + executor
    ├── CodeGamified.TUI/        Terminal UI system
    ├── CodeGamified.Editor/     In-game code editor
    ├── CodeGamified.Camera/     Camera rig + modes
    ├── CodeGamified.Time/       Simulation time + warp
    ├── CodeGamified.Audio/      Audio/haptic bridges
    ├── CodeGamified.Persistence/ Git-based persistence
    ├── CodeGamified.Procedural/  Runtime mesh generation
    ├── CodeGamified.Settings/    Settings system
    ├── CodeGamified.Quality/     Quality tier management
    └── CodeGamified.Bootstrap/   Shared bootstrap base
```

## License

MIT — Copyright CodeGamified 2025-2026