using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Marks a crate/log the player can grab and drag around.
    /// Mass comes from the collider area, so a big crate really is heavier than a small
    /// one - scale the object in the scene and the weight follows.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Pushable Prop")]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PushableProp : MonoBehaviour
    {
        [Header("Weight")]
        [Tooltip("Mass per square unit of collider. Wood ~0.5, stone ~2.5.")]
        [Min(0.01f)] public float density = 0.5f;

        [Tooltip("Recalculate mass from the collider whenever the object changes in the editor.")]
        public bool autoMassFromSize = true;

        [Header("Handling")]
        [Tooltip("Mass that halves the player's push speed. Lower = everything feels heavy.")]
        [Min(0.1f)] public float referenceMass = 12f;

        [Tooltip("Slowest the player is ever allowed to drag this, as a fraction of walk speed.")]
        [Range(0.05f, 1f)] public float minSpeedFactor = 0.25f;

        [Tooltip("Extra drag while being dragged, so it stops when the player stops.")]
        [Min(0f)] public float grabbedDrag = 6f;

        private Rigidbody2D _body;
        private float _restingDrag;

        public Rigidbody2D Body
        {
            get
            {
                if (_body == null) _body = GetComponent<Rigidbody2D>();
                return _body;
            }
        }

        /// <summary>How much of the player's walk speed survives while dragging this.</summary>
        public float SpeedFactor
        {
            get { return Mathf.Max(minSpeedFactor, referenceMass / (referenceMass + Body.mass)); }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _restingDrag = _body.linearDamping;
            ApplyMass();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyMass();
        }

        [ContextMenu("Apply Mass From Size")]
        public void ApplyMass()
        {
            if (!autoMassFromSize) return;

            Collider2D col = GetComponent<Collider2D>();
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (col == null || body == null || body.bodyType != RigidbodyType2D.Dynamic) return;

            // Unity's own auto mass already does area * density, so just feed it the density.
            col.density = density;
            body.useAutoMass = true;
        }

        public void OnGrabbed()
        {
            Body.linearDamping = grabbedDrag;
        }

        public void OnReleased()
        {
            Body.linearDamping = _restingDrag;
        }
    }
}
