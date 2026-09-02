using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Builds the water surface as a strip of vertices instead of a flat quad, so the
    /// surface can actually bend. Every column is a little spring: something falling in
    /// pushes a few of them down and the dip travels outwards.
    /// The shader still adds the small looping waves on top of this.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Interactable Water")]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(EdgeCollider2D))]
    public class InteractableWater : MonoBehaviour
    {
        [Header("Surface")]
        [Tooltip("Columns along the top edge. More columns = finer ripples, 64-128 is plenty.")]
        [Range(8, 400)] public int NumOfVertices = 96;

        [Min(0.1f)] public float Width = 10f;
        [Min(0.1f)] public float Height = 4f;

        [Tooltip("Width of the bottom edge as a fraction of the top. Taper it in to follow " +
                 "a V-shaped riverbed so the mesh corners stay inside the banks.")]
        [Range(0.02f, 1f)] public float BottomWidth = 1f;

        public Material WaterMaterial;

        [Header("Ripples")]
        [Tooltip("How hard a column is pulled back to the flat surface.")]
        [Range(0.001f, 0.2f)] public float springConstant = 0.025f;

        [Tooltip("How quickly a ripple dies out.")]
        [Range(0.001f, 0.2f)] public float damping = 0.035f;

        [Tooltip("How much of a column's movement leaks into its neighbours.")]
        [Range(0f, 0.5f)] public float spread = 0.06f;

        [Tooltip("Spread passes per step. Higher travels further but costs more.")]
        [Range(1, 8)] public int spreadPasses = 4;

        [Tooltip("Clamp so a heavy impact cannot tear the surface open.")]
        [Min(0.01f)] public float maxDisplacement = 1.2f;

        [Header("Rendering")]
        public string sortingLayer = "Default";
        public int sortingOrder = -2;

        [Header("Gizmos")]
        public Color GizmoColor = new Color(0.3f, 0.8f, 1f, 1f);

        private static readonly int AspectID = Shader.PropertyToID("_Aspect");

        private Mesh _mesh;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private EdgeCollider2D _edge;
        private MaterialPropertyBlock _block;

        private Vector3[] _vertices;
        private float[] _offsets;    // how far each column sits from the flat surface
        private float[] _velocities;
        private int _builtWith = -1;

        /// <summary>World height of the flat (undisturbed) surface.</summary>
        public float SurfaceLevel
        {
            get { return transform.position.y + Height * 0.5f * transform.lossyScale.y; }
        }

        public float LeftEdge
        {
            get { return transform.position.x - Width * 0.5f * transform.lossyScale.x; }
        }

        public float RightEdge
        {
            get { return transform.position.x + Width * 0.5f * transform.lossyScale.x; }
        }

        private void OnEnable()
        {
            Cache();
            Rebuild();
        }

        private void OnDisable()
        {
            if (_mesh != null && !Application.isPlaying) DestroyImmediate(_mesh);
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            Cache();
            Rebuild();
        }

        private void Cache()
        {
            if (_filter == null) _filter = GetComponent<MeshFilter>();
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_edge == null) _edge = GetComponent<EdgeCollider2D>();
            if (_block == null) _block = new MaterialPropertyBlock();
        }

        [ContextMenu("Rebuild Mesh")]
        public void Rebuild()
        {
            Cache();

            int columns = Mathf.Max(8, NumOfVertices);
            float halfW = Width * 0.5f;
            float halfH = Height * 0.5f;

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = "Interactable Water";
                _mesh.hideFlags = HideFlags.DontSave;
                _mesh.MarkDynamic();
            }

            if (_builtWith != columns || _vertices == null || _vertices.Length != columns * 2)
            {
                _vertices = new Vector3[columns * 2];
                _offsets = new float[columns];
                _velocities = new float[columns];
                _builtWith = columns;
                _mesh.Clear();
            }

            Vector2[] uvs = new Vector2[_vertices.Length];
            int[] tris = new int[(columns - 1) * 6];

            for (int i = 0; i < columns; i++)
            {
                float t = i / (float)(columns - 1);
                float x = Mathf.Lerp(-halfW, halfW, t);

                _vertices[i] = new Vector3(x, halfH + _offsets[i], 0f);            // top row
                _vertices[i + columns] = new Vector3(x * BottomWidth, -halfH, 0f); // bottom row

                uvs[i] = new Vector2(t, 1f);
                uvs[i + columns] = new Vector2(t, 0f);
            }

            for (int i = 0, tri = 0; i < columns - 1; i++, tri += 6)
            {
                int topL = i;
                int topR = i + 1;
                int botL = i + columns;
                int botR = i + columns + 1;

                tris[tri] = topL; tris[tri + 1] = botL; tris[tri + 2] = topR;
                tris[tri + 3] = topR; tris[tri + 4] = botL; tris[tri + 5] = botR;
            }

            _mesh.vertices = _vertices;
            _mesh.uv = uvs;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();

            _filter.sharedMesh = _mesh;

            if (WaterMaterial != null && _renderer.sharedMaterial != WaterMaterial)
                _renderer.sharedMaterial = WaterMaterial;

            _renderer.sortingLayerName = sortingLayer;
            _renderer.sortingOrder = sortingOrder;

            // The shader can only draw round bubbles if it knows how stretched the quad is.
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(AspectID, (Width * transform.lossyScale.x) / Mathf.Max(0.0001f, Height * transform.lossyScale.y));
            _renderer.SetPropertyBlock(_block);

            // Trigger line sitting on the flat surface - this is what fires the splashes.
            _edge.isTrigger = true;
            _edge.points = new Vector2[] { new Vector2(-halfW, halfH), new Vector2(halfW, halfH) };
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || _offsets == null) return;

            Simulate();
            ApplyToMesh();
        }

        private void Simulate()
        {
            int n = _offsets.Length;

            for (int i = 0; i < n; i++)
            {
                float acceleration = -springConstant * _offsets[i];
                _velocities[i] = (_velocities[i] + acceleration) * (1f - damping);

                float next = _offsets[i] + _velocities[i];
                if (next > maxDisplacement || next < -maxDisplacement) _velocities[i] = 0f;

                _offsets[i] = Mathf.Clamp(next, -maxDisplacement, maxDisplacement);
            }

            // Leak a bit of each column into its neighbours so the ripple travels.
            for (int pass = 0; pass < spreadPasses; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    if (i > 0)
                        _velocities[i - 1] += spread * (_offsets[i] - _offsets[i - 1]);

                    if (i < n - 1)
                        _velocities[i + 1] += spread * (_offsets[i] - _offsets[i + 1]);
                }
            }
        }

        private void ApplyToMesh()
        {
            int n = _offsets.Length;
            float halfH = Height * 0.5f;

            for (int i = 0; i < n; i++)
                _vertices[i].y = halfH + _offsets[i];

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        /// <summary>Dents the surface at a world X. Positive force pushes the water down.</summary>
        public void Splash(float worldX, float force, float radius)
        {
            if (_offsets == null || _offsets.Length == 0) return;

            float span = Width * transform.lossyScale.x;
            float local = Mathf.InverseLerp(transform.position.x - span * 0.5f, transform.position.x + span * 0.5f, worldX);
            if (local <= 0f || local >= 1f) return;

            int centre = Mathf.RoundToInt(local * (_offsets.Length - 1));
            int reach = Mathf.Max(1, Mathf.RoundToInt(radius / Mathf.Max(0.0001f, span) * _offsets.Length));

            for (int i = centre - reach; i <= centre + reach; i++)
            {
                if (i < 0 || i >= _offsets.Length) continue;

                float falloff = 1f - Mathf.Abs(i - centre) / (float)(reach + 1);
                _velocities[i] -= force * falloff;
            }
        }

        /// <summary>Splash sized from whatever just hit the water.</summary>
        public void Splash(Collider2D other, float force)
        {
            Splash(other.bounds.center.x, force, Mathf.Max(0.5f, other.bounds.size.x));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            float hw = Width * 0.5f;
            float hh = Height * 0.5f;
            float bw = hw * BottomWidth;

            Vector3 tl = new Vector3(-hw, hh, 0f), tr = new Vector3(hw, hh, 0f);
            Vector3 bl = new Vector3(-bw, -hh, 0f), br = new Vector3(bw, -hh, 0f);

            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
        }
    }
}
