# Scenario Engine & Node System

The Scenario Engine drives all interactive Japanese E-learning exercises. Scenarios are created entirely using ScriptableObjects, meaning new lessons can be authored directly within the Unity Editor without modifying C# source code.

## Scenario Definition

A `ScenarioDefinition` is composed of:
- **Metadata**: Unique ID, chapter, titles, and localized description.
- **Learning Targets**: IDs mapping to vocabulary and grammar items (e.g., `vocab.n5.onigiri`, `grammar.n5.wo_kudasai`).
- **Objectives**: A checklist of goals the student must accomplish to complete the mission.
- **Node List**: A linear or branching set of instructions that direct gameplay states.

## Node Types

Scenarios transition through nodes using `nextNodeId` or choice branches.

1. **Dialogue Node**:
   Presents NPC text (with optional Japanese audio). Can hold a list of `DialogueChoice` elements.
   Each choice provides text, modifies scores (Vocabulary, Grammar, etc.), updates learning progress, and sets the `nextNodeId`.

2. **CollectItem Node**:
   Pauses story narrative, unlocking player movement. The objective remains active until the player interacts with an `InteractiveItem` containing a matching `targetItemId` (e.g. `onigiri`).

3. **InspectItem Node**:
   Similar to CollectItem, but registers as an inspection interaction.

4. **GoToArea Node**:
   Requires the player to navigate to a trigger collider mapping to the `targetAreaId`.

5. **Complete / Fail Nodes**:
   Ends the simulation, triggering the Scoring calculations, updates progress persistence, and calls up the Result UI screen.
