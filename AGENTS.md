# NihongoLife repository rules

- Do not add Unity editor commands with `[MenuItem]`. Build, setup, asset integration and scene repair must not require manual menu actions.
- Do not create another Unity scene unless the user explicitly approves that exact scene. Prefer improving the existing gameplay scene and reusable prefabs.
- Persist finished content directly in scenes, prefabs and assets. Opening the project and pressing Play must use the finished state without a setup step.
- Never stack a replacement environment on top of an existing one. Remove or disable the superseded hierarchy, then validate renderers, colliders, doors, spawn points and navigation together.
- Do not claim a scene is complete without Play Mode visual and collision testing.
