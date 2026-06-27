# Contributing

Thanks for your interest in ProAnima Universal Vision Tracker.

This project is currently in active productionization. Contributions are welcome, especially in areas that improve reliability, model compatibility, camera sources, tracking quality, editor tooling, and documentation.

## Development Principles

- Keep the core model-agnostic.
- Keep camera/frame sources independent from model-specific input sizes.
- Prefer clear `ScriptableObject` profiles over hidden filename magic.
- Preserve Unity-friendly workflows through components, inspectors, events, and sample scenes.
- Avoid large unrelated refactors in feature PRs.
- Put large model and media files under Git LFS.

## Before Opening a PR

- Open the project in the target Unity version.
- Check the Console for compile errors.
- Test the relevant demo scene or workflow.
- Update docs when changing public behavior.
- Keep generated Unity metadata files (`.meta`) with their assets.

## Current Focus

See [ARCHITECTURE_ROADMAP.md](Assets/Scripts/UniversalTracker/ARCHITECTURE_ROADMAP.md) for the production roadmap.

