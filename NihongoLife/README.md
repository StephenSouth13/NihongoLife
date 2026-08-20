# Nihongo Life: N5 Japanese Daily Life Simulator

Nihongo Life is a production-ready, data-driven 3D simulator designed for N5 Japanese E-learning context. It places learners in realistic Japanese environments (convenience stores, restaurants, transit hubs) where they must apply vocabulary and grammar to solve everyday tasks.

Designed as a modular, extensible engine, Nihongo Life separating gameplay logic from lesson content. It is ready for WebGL builds and direct future integration with Next.js web LMS and Supabase backend databases.

## Key Features

- **Data-Driven Scenario Engine**: Entire lessons are authored via ScriptableObjects (`ScenarioDefinition`), handling objectives, trigger actions, and dialog structures with zero custom coding.
- **Dynamic Dialogue & Hint Controls**: Branching conversations support Vietnamese localizations, romaji, and reading/furigana guides, selectively shown or hidden depending on target learning modes (Guided, Practice, Assessment).
- **Service Locator Architecture**: Persistent systems (audio, saves, scenes) are accessed through interfaces (`GameServices`), allowing local implementations to be seamlessly swapped with web API channels later.
- **Robust Third-Person Controller**: Clean player physics, walk/run variables, look-at orbit camera, and collision raycasting.
- **Metric-Based E-Learning Assessment**: Evaluates learner accuracy across Vocabulary, Grammar, Listening, Response Accuracy, and Task Completion with persistent mastery progress updates.

## Project Structure

```
Assets/NihongoLife/
├── Scripts/
│   ├── Core/                # Bootstrap and scene flow controllers
│   ├── Architecture/        # Interface specifications
│   ├── Player/              # Character controller logic
│   ├── Camera/              # Orbit camera tracking
│   ├── Interaction/         # Detection filters and interactables
│   ├── NPC/                 # State management and LookAt anchors
│   ├── Dialogue/            # Dialogue logic and choice branchers
│   ├── Scenario/            # Engine loops and runtime definitions
│   ├── Learning/            # Mastery smoothing algorithms
│   ├── Scoring/             # Event-based scoring aggregates
│   ├── Save/                # JSON saving systems
│   └── UI/                  # HUD, dialogue panels, results
├── Resources/Scenarios/     # ScriptableObject lesson content
└── Tests/EditMode/          # NUnit verification suite
```

## Getting Started

1. Open this project folder inside Unity Editor version **6000.3.12f1**.
2. Run from the scene `Assets/NihongoLife/Scenes/00_Bootstrap` to start initialization.
3. Open **Window -> General -> Test Runner** to execute NUnit tests verifying the scenario runtime engine.

## Documentation

See the `Docs/` directory for full specifications:
- [`ARCHITECTURE.md`](Docs/ARCHITECTURE.md) - Deep dive on software design patterns.
- [`SCENARIO_SYSTEM.md`](Docs/SCENARIO_SYSTEM.md) - Graph nodes and execution cycles.
- [`CONTENT_GUIDE.md`](Docs/CONTENT_GUIDE.md) - Authoring parameters for curriculum designers.
- [`DEVELOPMENT.md`](Docs/DEVELOPMENT.md) - Build setups, testing, and debugging.
- [`ROADMAP.md`](Docs/ROADMAP.md) - Product development goals.
