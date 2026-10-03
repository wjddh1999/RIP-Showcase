using RIP.Player.Move;
using UnityEngine;

namespace RIP.Player.Modular
{
    [DisallowMultipleComponent]
    public sealed class TrackBeltAnimator : MonoBehaviour
    {
        private static readonly int BaseMap =
            Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor =
            Shader.PropertyToID("_BaseColor");
        private static readonly int BumpMap =
            Shader.PropertyToID("_BumpMap");
        private static readonly int BumpScale =
            Shader.PropertyToID("_BumpScale");
        private static readonly int Metallic =
            Shader.PropertyToID("_Metallic");
        private static readonly int Smoothness =
            Shader.PropertyToID("_Smoothness");
        private static readonly int AtlasRect =
            Shader.PropertyToID("_AtlasRect");
        private static readonly int TrackOffset =
            Shader.PropertyToID("_TrackOffset");

        [SerializeField] private Shader _trackShader;
        [SerializeField] private string _leftRendererName = "Track_L";
        [SerializeField] private string _rightRendererName = "Track_R";
        [SerializeField] private Vector4 _atlasRect =
            new(0.5259f, 0.4448f, 0.0603f, 0.3031f);
        [SerializeField, Min(0f)] private float _uvCyclesPerMeter = 0.45f;
        [SerializeField, Min(0f)] private float _halfTrackWidth = 1.9f;
        [SerializeField, Min(0f)] private float _speedResponse = 12f;
        [SerializeField] private float _leftUvDirection = -1f;
        [SerializeField] private float _rightUvDirection = -1f;

        private PlayerNetworkController _controller;
        private Transform _motionRoot;
        private Renderer _leftRenderer;
        private Renderer _rightRenderer;
        private Material _leftMaterial;
        private Material _rightMaterial;
        private Material _originalLeftMaterial;
        private Material _originalRightMaterial;
        private Vector3 _previousPosition;
        private float _previousYaw;
        private float _leftSpeed;
        private float _rightSpeed;
        private float _leftOffset;
        private float _rightOffset;
        private bool _initialized;

        private void OnEnable()
        {
            TryInitialize();
        }

        private void LateUpdate()
        {
            if (!_initialized && !TryInitialize())
                return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            bool hasNetworkState =
                _controller != null &&
                _controller.Object != null &&
                _controller.Object.IsValid;
            Vector3 velocity = hasNetworkState
                ? _controller.Velocity
                : (_motionRoot.position - _previousPosition) / deltaTime;
            Vector3 localVelocity =
                _motionRoot.InverseTransformDirection(velocity);

            float yaw = _motionRoot.eulerAngles.y;
            float yawRate =
                Mathf.DeltaAngle(_previousYaw, yaw) *
                Mathf.Deg2Rad /
                deltaTime;
            float targetLeftSpeed =
                localVelocity.z - yawRate * _halfTrackWidth;
            float targetRightSpeed =
                localVelocity.z + yawRate * _halfTrackWidth;
            float response =
                1f - Mathf.Exp(-_speedResponse * deltaTime);

            _leftSpeed = Mathf.Lerp(
                _leftSpeed,
                targetLeftSpeed,
                response);
            _rightSpeed = Mathf.Lerp(
                _rightSpeed,
                targetRightSpeed,
                response);
            _leftOffset = Mathf.Repeat(
                _leftOffset +
                _leftSpeed *
                _uvCyclesPerMeter *
                _leftUvDirection *
                deltaTime,
                1f);
            _rightOffset = Mathf.Repeat(
                _rightOffset +
                _rightSpeed *
                _uvCyclesPerMeter *
                _rightUvDirection *
                deltaTime,
                1f);

            _leftMaterial.SetFloat(TrackOffset, _leftOffset);
            _rightMaterial.SetFloat(TrackOffset, _rightOffset);
            _previousPosition = _motionRoot.position;
            _previousYaw = yaw;
        }

        private bool TryInitialize()
        {
            if (_trackShader == null)
                return false;

            _leftRenderer = FindRenderer(_leftRendererName);
            _rightRenderer = FindRenderer(_rightRendererName);
            if (_leftRenderer == null || _rightRenderer == null)
                return false;

            _controller = GetComponentInParent<PlayerNetworkController>();
            _motionRoot = _controller != null
                ? _controller.transform
                : transform.root;
            _originalLeftMaterial = _leftRenderer.sharedMaterial;
            _originalRightMaterial = _rightRenderer.sharedMaterial;
            _leftMaterial = CreateTrackMaterial(_originalLeftMaterial);
            _rightMaterial = CreateTrackMaterial(_originalRightMaterial);
            _leftRenderer.sharedMaterial = _leftMaterial;
            _rightRenderer.sharedMaterial = _rightMaterial;
            _previousPosition = _motionRoot.position;
            _previousYaw = _motionRoot.eulerAngles.y;
            _initialized = true;
            return true;
        }

        private Material CreateTrackMaterial(Material source)
        {
            Material material = new(_trackShader)
            {
                name = $"{name}_TrackRuntime"
            };

            if (source != null)
            {
                Texture baseTexture = GetTexture(
                    source,
                    "_BaseMap",
                    "_MainTex");
                Texture normalTexture = GetTexture(
                    source,
                    "_BumpMap");
                material.SetTexture(BaseMap, baseTexture);
                material.SetTexture(BumpMap, normalTexture);
                material.SetColor(
                    BaseColor,
                    GetColor(
                        source,
                        Color.white,
                        "_BaseColor",
                        "_Color"));
                material.SetFloat(
                    BumpScale,
                    GetFloat(source, 1f, "_BumpScale"));
                material.SetFloat(
                    Metallic,
                    GetFloat(source, 0f, "_Metallic"));
                material.SetFloat(
                    Smoothness,
                    GetFloat(
                        source,
                        0.45f,
                        "_Smoothness",
                        "_Glossiness"));
            }

            material.SetVector(AtlasRect, _atlasRect);
            return material;
        }

        private Renderer FindRenderer(string rendererName)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer candidate in renderers)
            {
                if (candidate.name == rendererName)
                    return candidate;
            }

            return null;
        }

        private static Texture GetTexture(
            Material material,
            params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                    return material.GetTexture(propertyName);
            }

            return null;
        }

        private static Color GetColor(
            Material material,
            Color fallback,
            params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                    return material.GetColor(propertyName);
            }

            return fallback;
        }

        private static float GetFloat(
            Material material,
            float fallback,
            params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                    return material.GetFloat(propertyName);
            }

            return fallback;
        }

        private void OnDestroy()
        {
            if (_leftRenderer != null)
                _leftRenderer.sharedMaterial = _originalLeftMaterial;
            if (_rightRenderer != null)
                _rightRenderer.sharedMaterial = _originalRightMaterial;

            DestroyMaterial(_leftMaterial);
            DestroyMaterial(_rightMaterial);
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
                return;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }
    }
}
