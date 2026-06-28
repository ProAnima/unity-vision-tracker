# Dashboard Overlay Sample

This sample previews the UI Toolkit dashboard with synthetic `VisionFrameResult` data.

It does not require a model, camera, or runtime adapter. Use it to inspect overlay geometry, labels, keypoints, skeleton bones, masks, and runtime stats.

## Use

1. Import this sample from Package Manager.
2. Create an empty GameObject.
3. Add `ProAnimaVisionDashboardFixture`.
4. Enter Play Mode.

The component adds/configures `VisionToolkitDashboardReceiver` and sends a moving synthetic person/object frame to the dashboard.
