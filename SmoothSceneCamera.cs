using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace SmoothSceneCamera
{
    public class SmoothSceneCamera : MonoBehaviour
    {
        private static float _lastFrameTime;
        private static float _zoomDuration = 1;
        private static Easings.Type _zoomEasing = Easings.Type.ExpoOut;
        private static AnimationCurve _zoomCurve;
        
        [MenuItem("Tools/Zoom Scene Camera")]
        public static void ZoomOutSceneCamera()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(ZoomRoutine());
        }

        private static IEnumerator ZoomRoutine()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (!sceneView) yield break;
            
            _lastFrameTime = (float)EditorApplication.timeSinceStartup;
            _zoomCurve ??= AnimationCurve.Linear(0, 0, 1, 1);

            var startSize = sceneView.size;
            var targetSize = startSize + 1;
            
            float lerpPos = 0;
            while (lerpPos < 1)
            {
                var deltaTime = (float)(EditorApplication.timeSinceStartup - _lastFrameTime);
                lerpPos += deltaTime / _zoomDuration;
                lerpPos = Mathf.Clamp01(lerpPos);
                var t = _zoomCurve.Evaluate(Easings.Ease(lerpPos, _zoomEasing));
                
                UpdateCameraDistance(sceneView, startSize, targetSize, t);
                
                _lastFrameTime = (float)EditorApplication.timeSinceStartup;
                yield return null;
            }
            
        }

        private static void UpdateCameraDistance(SceneView sceneView,
                                                 float startPos, float targetPos, float t)
            => sceneView.LookAt(sceneView.pivot, sceneView.rotation,
                                Mathf.LerpUnclamped(startPos, targetPos, t),
                                sceneView.camera.orthographic, true);
    }
}
