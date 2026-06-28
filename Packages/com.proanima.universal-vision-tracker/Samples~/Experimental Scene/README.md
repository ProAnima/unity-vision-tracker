# Experimental Scene

This sample is the fastest visual smoke test for ProAnima Universal Vision Tracker.

It opens with a polished UI Toolkit dashboard and a synthetic `VisionFrameResult` stream, so it works without a camera, model asset, or platform SDK. Use it to verify dashboard rendering, result-list behavior, coordinate mapping, detections, pose bones, keypoints, masks, health, and runtime status.

## Run

1. Import `Experimental Scene` from Package Manager.
2. Open `ProAnimaVisionExperimentalScene.unity`.
3. Press Play.

The scene runs in synthetic preview mode by default.

## Connect A Real Pipeline

Select `ProAnima Vision Experimental Demo` in the scene and configure:

- disable `Run Synthetic Preview`;
- enable `Configure Real Pipeline`;
- assign a `VisionPipelineProfile` or `VisionModelProfile`;
- choose a frame source;
- optionally enable `Auto Start Real Pipeline`.

Use `Tools/ProAnima Vision/Profile Compatibility Inspector` before running real models.
