# Contributing to NihongoLife

Thank you for your interest in contributing to **NihongoLife**! This document provides guidelines to help you contribute effectively to our Japanese-learning metaverse.

## 1. Code of Conduct
By participating in this project, you agree to abide by our Code of Conduct. Please be respectful, constructive, and helpful to other contributors.

## 2. Getting Started
1. **Fork the Repository:** Fork the project to your own GitHub account.
2. **Clone:** Clone the repository locally.
3. **Unity Version:** Ensure you are using the exact Unity version specified in `ProjectSettings/ProjectVersion.txt`.

## 3. Branching Strategy
- `main`: The stable branch. Do not commit directly here.
- `feature/*`: Create a branch named `feature/your-feature-name` for new additions.
- `bugfix/*`: Create a branch named `bugfix/issue-description` for bug fixes.

## 4. Development Guidelines
- **Scene Architecture:** We use `90_TestSandbox` for gameplay testing. Do not rely on `01_MainMenu` for building test features. Avoid creating new scenes unless explicitly approved; prefer prefabs and modular components.
- **Third-Party Assets:** Do not commit copyrighted assets without verifying their license. Any third-party asset must be placed in `ThirdParty/` with its respective license file.
- **API Keys & Secrets:** NEVER commit API keys (e.g., Supabase, Agora) directly in plain text. Use environment variables or obfuscate them if providing placeholders.
- **No Manual Build Steps:** Ensure all new features, setups, and scene initializations do not require manual `[MenuItem]` execution. The game must work out of the box when pressing Play.

## 5. Testing
Before submitting a Pull Request, ensure:
1. You have tested your changes in **Play Mode** inside Unity.
2. No magenta/missing materials exist.
3. Console is free of compilation errors or severe warnings.

## 6. Submitting a Pull Request (PR)
1. Commit your changes with clear, descriptive messages.
2. Push your branch to your fork.
3. Open a Pull Request against the `main` branch.
4. Describe your changes in detail, including screenshots or videos if you modified the UI or 3D environment.

We look forward to building the ultimate Japanese learning experience together!
