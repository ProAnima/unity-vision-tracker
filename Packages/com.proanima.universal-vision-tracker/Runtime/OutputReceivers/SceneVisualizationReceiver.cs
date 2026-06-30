using UnityEngine;
using UniversalTracker.Core;

namespace UniversalTracker.OutputReceivers
{
    public class SceneVisualizationReceiver : MonoBehaviour, IOutputReceiver
    {
        [Tooltip("Camera used as the reference for scene-space visualization.")]
        public Camera targetCamera;
        [Tooltip("Parent transform for spawned visualization markers.")]
        public Transform visualizationRoot;
        [Tooltip("Prefab spawned for each detection.")]
        public GameObject bboxMarkerPrefab;
        [Tooltip("Prefab spawned for each visible pose keypoint.")]
        public GameObject keypointMarkerPrefab;

        [SerializeField, Tooltip("Enable this receiver.")]
        private bool isEnabled = true;
        [Tooltip("Spawn 3D scene markers for detections and keypoints.")]
        public bool spawn3DMarkers = true;

        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }

        public void Initialize()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (visualizationRoot == null)
            {
                var root = new GameObject("SceneVisualization");
                root.transform.SetParent(transform);
                visualizationRoot = root.transform;
            }
        }

        public void ReceiveVisionResult(VisionFrameResult result, Texture sourceTexture = null)
        {
            if (!isEnabled || result == null)
                return;

            Clear();

            if (!spawn3DMarkers)
                return;

            SpawnDetections(result.detections);
            SpawnPoses(result.poses);
        }

        public void Clear()
        {
            if (visualizationRoot == null)
                return;

            foreach (Transform child in visualizationRoot)
                Destroy(child.gameObject);
        }

        public void Release()
        {
            Clear();
        }

        private void SpawnDetections(VisionDetection[] detections)
        {
            if (bboxMarkerPrefab == null || detections == null)
                return;

            foreach (VisionDetection detection in detections)
            {
                GameObject marker = Instantiate(bboxMarkerPrefab, visualizationRoot);
                marker.name = $"Detection_{detection.classId}_{detection.confidence:F2}";
            }
        }

        private void SpawnPoses(VisionPose[] poses)
        {
            if (keypointMarkerPrefab == null || poses == null)
                return;

            foreach (VisionPose pose in poses)
            {
                if (pose.keypoints == null)
                    continue;

                foreach (VisionKeypoint keypoint in pose.keypoints)
                {
                    if (!keypoint.isVisible)
                        continue;

                    GameObject marker = Instantiate(keypointMarkerPrefab, visualizationRoot);
                    marker.name = $"Keypoint_{keypoint.index}_{keypoint.confidence:F2}";
                }
            }
        }
    }
}
