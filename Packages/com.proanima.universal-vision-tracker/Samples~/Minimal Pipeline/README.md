# Minimal Pipeline Sample

This sample provides a small bootstrap component for a profile-driven tracker object.

## Use

1. Import this sample from Package Manager.
2. Create an empty GameObject.
3. Add `ProAnimaVisionMinimalPipelineBootstrap`.
4. Assign either a `VisionPipelineProfile` or a single `VisionModelProfile`.
5. Choose a frame source and click `Apply Sample Setup` from the component context menu.

The component configures `UniversalTrackerManager`, optional tracking, optional UI Toolkit dashboard, and the selected source type.

For production scenes, prefer `Tools/ProAnima Vision/Control Center > Open Setup Wizard`; this sample is intentionally tiny and readable.
