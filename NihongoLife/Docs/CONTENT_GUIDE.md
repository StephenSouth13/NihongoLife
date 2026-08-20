# Content Creation Guide

This document details how to author new Japanese learning scenarios for Nihongo Life.

## Adding a New Scenario Asset

1. In the Unity Project window, navigate to a resource folder (recommended: `Assets/NihongoLife/Resources/Scenarios/`).
2. Right-click and choose **Create -> NihongoLife -> Scenario Definition**.
3. Set the `id` field using a dot-notation namespace (e.g. `scenario.restaurant.order_ramen`).
4. Fill in the title, description, and chapter metadata.

## Configured Curriculum Guidelines

Keep scenarios focused on practical everyday-life interactions:
- **Chapter 1 — Greetings & Introductions**: `Chapter 1 — はじめまして`
- **Chapter 2 — Store Transactions**: `Chapter 2 — コンビニ` (Buy items, handle prompts about receipt, bags, points card).
- **Chapter 3 — Diners & Restaurants**: `Chapter 3 — レストラン` (Order food, choose table types, requests).
- **Chapter 4 — Transit & Station Navigation**: `Chapter 4 — 駅` (Ask directions, buy tickets).

## Writing Dialogue Branches

When creating Dialogue choices:
- Each option should represent a plausible learner response.
- Assign **Score Event Modifiers** to reward grammatically correct or high-politeness responses (e.g. `Grammar +10` for using polite forms, `ResponseAccuracy -5` for choosing inappropriate/casual responses in a formal context).
- Attach **Grammar/Vocabulary Tags** (e.g. `grammar.n5.wo_kudasai`) to update specific learning mastery levels on successful completion.
