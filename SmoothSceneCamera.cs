using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace SmoothSceneCamera
{
    [InitializeOnLoad]
    public class SmoothSceneCamera
    {
        #region Preferences

        private const string UseSmoothZoomKey = "NnUtils_SmoothSceneCamera_SmoothZoom";
        private static bool UseSmoothZoom
        {
            get => EditorPrefs.GetBool(UseSmoothZoomKey, true);
            set => EditorPrefs.SetBool(UseSmoothZoomKey, value);
        }

        private const string ZoomAmountKey = "NnUtils_SmoothSceneCamera_ZoomAmount";
        private static float ZoomAmount
        {
            get => EditorPrefs.GetFloat(ZoomAmountKey, 0.1f);
            set => EditorPrefs.SetFloat(ZoomAmountKey, value);
        }

        #endregion

        private static float _lastFrameTime;
        private static float _sizeDelta;

        private static float _zoomDistancePower = 1.15f;
        private static float _zoomDuration = 1;
        private static Easings.Type _zoomEasing = Easings.Type.ExpoOut;
        private static AnimationCurve _zoomCurve;

        static SmoothSceneCamera() => SceneView.duringSceneGui += OnSceneGUI;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!UseSmoothZoom) return;

            var e = Event.current;

            if (e.type == EventType.ScrollWheel)
            {
                Zoom(e.delta.y);
                e.Use();
            }
        }

        private static void Zoom(float dir)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (!sceneView) return;

            // Reset size delta if its sign is opposite of current dir
            if (!Mathf.Approximately(Mathf.Sign(dir), Mathf.Sign(_sizeDelta))) _sizeDelta = 0;

            var startSize = sceneView.size;
            _sizeDelta += dir * ZoomAmount * Mathf.Pow(startSize + _sizeDelta, _zoomDistancePower);
            var targetSize = startSize + _sizeDelta;

            if (_zoomRoutine != null) EditorCoroutineUtility.StopCoroutine(_zoomRoutine);


            _zoomRoutine = EditorCoroutineUtility.StartCoroutineOwnerless(
                ZoomRoutine(sceneView, startSize, targetSize));
        }

        private static EditorCoroutine _zoomRoutine;

        private static IEnumerator ZoomRoutine(
            SceneView sceneView, float startSize, float targetSize)
        {
            _lastFrameTime = (float)EditorApplication.timeSinceStartup;
            _zoomCurve ??= AnimationCurve.Linear(0, 0, 1, 1);
            var startSizeDelta = _sizeDelta;

            float lerpPos = 0;
            while (lerpPos < 1)
            {
                var deltaTime = (float)(EditorApplication.timeSinceStartup - _lastFrameTime);
                lerpPos += deltaTime / _zoomDuration;
                lerpPos = Mathf.Clamp01(lerpPos);
                var t = _zoomCurve.Evaluate(Easings.Ease(lerpPos, _zoomEasing));

                UpdateCameraDistance(sceneView, startSize, targetSize, t);
                _sizeDelta = Mathf.LerpUnclamped(startSizeDelta, 0, t);

                _lastFrameTime = (float)EditorApplication.timeSinceStartup;
                yield return null;
            }

            _zoomRoutine = null;
        }

        private static void UpdateCameraDistance(SceneView sceneView,
                                                 float startPos, float targetPos, float t)
            => sceneView.LookAt(sceneView.pivot, sceneView.rotation,
                                Mathf.LerpUnclamped(startPos, targetPos, t),
                                sceneView.camera.orthographic, true);

        [SettingsProvider]
        public static SettingsProvider CreateMyPreferencesProvider()
        {
            var provider =
                new SettingsProvider("Preferences/NnUtils/Smooth Scene Camera", SettingsScope.User)
                {
                    label = "Smooth Scene Camera",
                    guiHandler = _ =>
                    {
                        EditorGUI.BeginChangeCheck();

                        var useSmoothZoom = EditorGUILayout.Toggle("Smooth Zoom", UseSmoothZoom);
                        var zoomAmount = EditorGUILayout.FloatField("Zoom Amount", ZoomAmount);

                        if (EditorGUI.EndChangeCheck())
                        {
                            UseSmoothZoom = useSmoothZoom;
                            ZoomAmount = zoomAmount;
                        }
                    }
                };

            return provider;
        }
    }
}
