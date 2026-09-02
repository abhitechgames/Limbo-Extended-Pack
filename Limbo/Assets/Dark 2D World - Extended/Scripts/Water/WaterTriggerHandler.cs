using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Sits on the water surface trigger. Anything that crosses the line gets a ripple
    /// and a splash effect, sized by how fast it was going.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Water Trigger Handler")]
    [RequireComponent(typeof(InteractableWater))]
    public class WaterTriggerHandler : MonoBehaviour
    {
        [Header("Splash Effect")]
        public GameObject splashPrefab;

        [Tooltip("Slower than this and nothing happens - stops jitter at the surface.")]
        public float minImpactSpeed = 1.5f;

        [Tooltip("Speed above which the splash is already at full size.")]
        public float fullImpactSpeed = 18f;

        [Range(0.1f, 3f)] public float minSplashScale = 0.5f;
        [Range(0.1f, 6f)] public float maxSplashScale = 2.2f;

        [Tooltip("Seconds before the spawned effect is cleaned up.")]
        public float splashLifetime = 3f;

        [Header("Ripple")]
        [Tooltip("Ripple strength per unit of impact speed.")]
        public float rippleStrength = 0.006f;

        [Tooltip("A leaving object pulls the surface up instead of down.")]
        [Range(0f, 1f)] public float exitRippleScale = 0.5f;

        [Header("Filter")]
        public LayerMask reactingLayers = ~0;

        private InteractableWater _water;

        private void Awake()
        {
            _water = GetComponent<InteractableWater>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            React(other, 1f, true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            React(other, exitRippleScale, false);
        }

        private void React(Collider2D other, float scale, bool entering)
        {
            if (scale <= 0f) return;
            if ((reactingLayers.value & (1 << other.gameObject.layer)) == 0) return;

            Rigidbody2D body = other.attachedRigidbody;
            if (body == null) return;

            float speed = Mathf.Abs(body.linearVelocity.y);
            if (speed < minImpactSpeed) return;

            float weight = Mathf.Sqrt(Mathf.Max(1f, body.mass));
            float force = speed * rippleStrength * weight * scale;

            // Something dropping in pushes the surface down, something jumping out lifts it.
            _water.Splash(other, entering ? force : -force);

            SpawnSplash(other, speed, scale);
        }

        private void SpawnSplash(Collider2D other, float speed, float scale)
        {
            if (splashPrefab == null) return;

            float t = Mathf.InverseLerp(minImpactSpeed, fullImpactSpeed, speed);
            float size = Mathf.Lerp(minSplashScale, maxSplashScale, t) * scale;

            Vector3 point = new Vector3(other.bounds.center.x, _water.SurfaceLevel, transform.position.z - 0.01f);

            GameObject fx = Instantiate(splashPrefab, point, Quaternion.identity, transform.parent);
            fx.transform.localScale = Vector3.one * size;

            Destroy(fx, splashLifetime);
        }
    }
}
