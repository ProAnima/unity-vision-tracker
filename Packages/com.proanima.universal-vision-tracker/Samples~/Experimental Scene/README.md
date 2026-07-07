# Experimental Scene

This sample is the fastest real-source smoke test for ProAnima Universal Vision Tracker.

It opens with a polished UI Toolkit dashboard and a real `WebCamTexture` preview by default. It can also be wired to a `VideoPlayer` source from Control Center presets. Use it to verify source sizing, fit mode, rotation, mirroring, dashboard rendering, and runtime status before connecting a model.

## Run

1. Open `Tools/ProAnima Vision/Control Center` and click `Import Sample`, or import `Experimental Scene` from Package Manager.
2. Open `ProAnimaVisionExperimentalScene.unity`.
3. Press Play.

The scene runs in WebCam preview mode by default. Use the dashboard camera controls to choose a camera device, switch to the next device, rotate the preview, and mirror the image. Select `ProAnima Vision Experimental Demo` for requested resolution/FPS and advanced preview settings.

## Connect A Real Pipeline

Select `ProAnima Vision Experimental Demo` in the scene and configure:

- keep `Run WebCam Preview` enabled if you want to use the same camera feed;
- enable `Configure Real Pipeline`;
- assign a `VisionPipelineProfile` or `VisionModelProfile`;
- optionally enable `Auto Start Real Pipeline`.

Quick Start YOLO presets disable the standalone `Run WebCam Preview` option on purpose. In that mode the real `VisionPipeline` owns the camera and passes the same source texture to the dashboard, avoiding two competing `WebCamTexture` instances.

For `YOLO ... + Video` presets, assign one `Video Clip` or URL on the `VideoPlayer` component, or add several clips/URLs to `VisionVideoPlaylistSource > Videos`. During Play Mode, the dashboard `Video` controls expose `<` and `>` buttons for moving backward and forward through that row. The preset sets `Real Pipeline Source` to `Video`, loops playback, and uses `APIOnly` rendering so the pipeline consumes the decoded frame texture directly.

Use `Tools/ProAnima Vision/Control Center > Compatibility Inspector` before running real models.
