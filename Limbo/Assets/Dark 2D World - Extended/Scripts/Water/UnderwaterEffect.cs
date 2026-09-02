using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Screen wobble for whenever the player is under the surface. It is a quad parented
    /// to the camera, sitting on the gameplay plane and drawn after everything else, so
    /// it can re-sample the frame and bend it. Only the pixels below the water line are
    /// touched, which keeps a readable split right at the surface.
    /// Needs "Camera Sorting Layer Texture" enabled on the 2D Renderer.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Underwater Effect")]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class UnderwaterEffect : MonoBehaviour
    {
        [Header("Setup")]
        public Material overlayMaterial;

        [Tooltip("Who has to be underwater. Left empty, the object tagged Player is used.")]
        public Transform swimmer;

        [Tooltip("Depth of the plane the game is played on. Usually 0.")]
        public float gameplayPlaneZ = 0f;

        [Header("Feel")]
        [Tooltip("How quickly the effect fades in and out of a dunk.")]
        [Min(0.1f)] public float fadeSpeed = 7f;

        [Tooltip("How far under the surface before the effect is at full strength.")]
        [Min(0.01f)] public float fullEffectDepth = 0.5f;

        [Header("Rendering")]
        public string sortingLayer = "SortingTexture";
        public int sortingOrder = 100;

        private static readonly int WaterLevelID = Shader.PropertyToID("_WaterLevel");
        private static readonly int StrengthID = Shader.PropertyToID("_Strength");

        private Camera _camera;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Mesh _quad;

        private InteractableWater _water;
        private float _strength;

        private void OnEnable()
        {
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
            _camera = GetComponentInParent<Camera>();

            BuildQuad();

            if (overlayMaterial != null) _renderer.sharedMaterial = overlayMaterial;
            _renderer.sortingLayerName = sortingLayer;
            _renderer.sortingOrder = sortingOrder;
            _renderer.enabled = false;
        }

        private void OnDisable()
        {
            if (_quad != null && !Application.isPlaying) DestroyImmediate(_quad);
        }

        private void BuildQuad()
        {
            if (_quad == null)
            {
                _quad = new Mesh { name = "Underwater Overlay" };
                _quad.hideFlags = HideFlags.DontSave;
                _quad.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f,  0.5f, 0f), new Vector3(0.5f,  0.5f, 0f)
                };
                _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                _quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
                _quad.RecalculateBounds();
            }

            _filter.sharedMesh = _quad;
        }

        private void LateUpdate()
        {
            if (_camera == null) _camera = GetComponentInParent<Camera>();
            if (_camera == null) return;

            FitToView();

            if (!Application.isPlaying)
            {
                _renderer.enabled = false;
                return;
            }

            if (swimmer == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) swimmer = found.transform;
            }

            float target = 0f;
            InteractableWater water = FindWaterAround(swimmer);

            if (water != null)
            {
                float depth = water.SurfaceLevel - swimmer.position.y;
                target = Mathf.Clamp01(depth / fullEffectDepth);
                _water = water;
            }

            _strength = Mathf.MoveTowards(_strength, target, fadeSpeed * Time.deltaTime);
            _renderer.enabled = _strength > 0.001f;

            if (!_renderer.enabled) return;

            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(WaterLevelID, _water != null ? _water.SurfaceLevel : 0f);
            _block.SetFloat(StrengthID, _strength);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>Water body the swimmer is currently inside, if any.</summary>
        private InteractableWater FindWaterAround(Transform who)
        {
            if (who == null) return null;

            // Cheap check against the one we used last frame before searching again.
            if (_water != null && Contains(_water, who.position)) return _water;

            foreach (var candidate in FindObjectsByType<InteractableWater>(FindObjectsSortMode.None))
                if (Contains(candidate, who.position)) return candidate;

            return null;
        }

        private static bool Contains(InteractableWater water, Vector3 point)
        {
            if (point.x < water.LeftEdge || point.x > water.RightEdge) return false;
            if (point.y > water.SurfaceLevel) return false;

            float floor = water.SurfaceLevel - water.Height * water.transform.lossyScale.y;
            return point.y > floor;
        }

        private void FitToView()
        {
            float distance = Mathf.Abs(gameplayPlaneZ - _camera.transform.position.z);

            float height = _camera.orthographic
                ? _camera.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // A little margin so the quad never falls short at the screen edges.
            transform.localPosition = new Vector3(0f, 0f, distance);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(height * _camera.aspect * 1.05f, height * 1.05f, 1f);
        }
    }
}
