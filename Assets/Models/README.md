# YOLO Models

This folder contains repository-supplied YOLO ONNX models for local testing.

Unity Inference Engine imports these `.onnx` files as `ModelAsset` assets. The ProAnima Vision quick-start presets look here first when creating runtime profiles:

- `yolo26n.onnx` for detection.
- `yolo26n-pose.onnx` for 2D pose.
- `yolo26n-seg.onnx` for segmentation.
- `yolo11n.onnx` and other variants are fallback or comparison models.

## Quick Test

1. Open `Tools > ProAnima Vision > Control Center`.
2. Choose `YOLO Detection + WebCam`, `YOLO Pose + WebCam`, or `YOLO Segmentation + WebCam`.
3. The tool creates `VisionModelProfile` and `VisionPipelineProfile` assets under `Assets/ProAnima Vision/Profiles`.
4. If the Experimental Scene is open, the preset wires that scene directly; otherwise it creates or updates a `ProAnima Vision Tracker` object in the active scene.
5. Run `Profile Validator`, then press Play.

## Production Notes

- Keep model binaries in Git LFS.
- Verify upstream model licenses before shipping an app.
- Keep profile parser ids and tensor schemas aligned with the exported model layout.
