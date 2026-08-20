# Product Roadmap

This roadmap details the future integration phases of the Nihongo Life commercial simulator.

## Phase 1 — Local Foundation (Completed)
- Reusable namespace and folder layout.
- Decoupled `GameServices` interface registries.
- Player character controller and Orbit Camera.
- Node-driven dialogue branching and response scoring modifiers.
- Convenience store (コンビニ) vertical slice scenario.
- Deterministic mastery calculation and local profile saving.

## Phase 2 — Asset Integration & Expansion
- Replace geometry primitives with low-poly stylized Japanese environment assets.
- Integrate character models with standard Mecanim animations (walk, idle, talk, checkout gesture).
- Write and build scenarios for:
  - Chapter 3: Ordering Ramen at a Restaurant
  - Chapter 4: Buying transit tickets at a Train Station
- Support audio clip playback of native spoken phrases.

## Phase 3 — Next.js + Supabase Integration
- Swap out `LocalProgressRepository` with a remote repository invoking standard HTTPS REST requests.
- Integrate WebGL launch tokens to authenticate and link student profiles:
  1. Student starts lesson on Web Portal.
  2. Web Portal launches WebGL Unity frame with a session token.
  3. Unity reads session token, downloads scenario definition via API, and runs it.
  4. Unity posts the resulting `ScoreBreakdownDto` to the API on complete.
  5. API validates transaction security and updates progress in Supabase.
