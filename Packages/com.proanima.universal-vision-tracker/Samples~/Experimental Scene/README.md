# Experimental Scene

This sample is the fastest real-camera smoke test for ProAnima Universal Vision Tracker.

It opens with a polished UI Toolkit dashboard and a real `WebCamTexture` preview by default. Use it to verify camera access, source sizing, fit mode, rotation, mirroring, dashboard rendering, and runtime status before connecting a model.

## Run

1. Open `Tools/ProAnima Vision/Control Center` and click `Import Sample`, or import `Experimental Scene` from Package Manager.
2. Open `ProAnimaVisionExperimentalScene.unity`.
3. Press Play.

The scene runs in WebCam preview mode by default. Select `ProAnima Vision Experimental Demo` to choose the camera device index/name, requested resolution/FPS, preview fit mode, rotation, and mirror settings.

## Connect A Real Pipeline

Select `ProAnima Vision Experimental Demo` in the scene and configure:

- keep `Run WebCam Preview` enabled if you want to use the same camera feed;
- enable `Configure Real Pipeline`;
- assign a `VisionPipelineProfile` or `VisionModelProfile`;
- optionally enable `Auto Start Real Pipeline`.

Use `Tools/ProAnima Vision/Control Center > Compatibility Inspector` before running real models.
