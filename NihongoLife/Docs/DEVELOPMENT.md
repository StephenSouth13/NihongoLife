# Development Guide

This guide describes how to configure, test, and build the Nihongo Life simulation.

## Scene Structure

The application flow requires that the bootstrap scene runs first to initialize persistent game objects:

1. **`00_Bootstrap`**: Loads the persistent `AppRoot` prefab (which registers scene controllers, audio channels, save profiles, and scenario registries) and then auto-loads the Main Menu.
2. **`01_MainMenu`**: Offers game starts, chapters configuration, and settings.
3. **`90_TestSandbox`**: Interactive testing scene containing cashier booths, shelves, trigger areas, and player spawn pivots.

## Running Unit & Integration Tests

Open the Unity Test Runner window (**Window -> General -> Test Runner**):
- Under the **EditMode** tab, run the `ScenarioEngineTests` group.
- These verify scoring aggregations, node graph resolution, and serialization formats.

## WebGL Build Considerations

- Local saving utilizes `File.WriteAllText` targeting `Application.persistentDataPath`. On WebGL, Unity uses Emscripten FS which persists to IndexedDB.
- Avoid synchronous thread blocking or direct file streaming outside `Application.persistentDataPath`.
- Optimize texture size configurations and keep low poly geometry to reduce load latency.
