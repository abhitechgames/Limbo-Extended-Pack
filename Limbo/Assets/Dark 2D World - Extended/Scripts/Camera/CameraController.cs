using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Lazy follow camera. It leads slightly in the direction of travel and lets the
    /// player move a little inside a dead zone before it reacts, which keeps the
    /// parallax layers from twitching on every small hop.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Camera Controller")]
    public class CameraController : MonoBehaviour
    {
        [Header("Follow")]
        [Tooltip("Higher = the camera catches up faster.")]
        public float damping = 1.5f;

        [Tooltip("How far the camera leads the player. X is the look-ahead.")]
        public Vector3 offset = new Vector3(2f, 1f, 0f);

        [Tooltip("Mirrors the offset when the player is walking left.")]
        public bool faceLeft;

        [Tooltip("Vertical follow is slower than horizontal - set 1 to match it.")]
        [Range(0.1f, 1f)] public float verticalDampingScale = 0.6f;

        [Header("Dead Zone")]
        [Tooltip("The player can move this far vertically before the camera bothers.")]
        [Min(0f)] public float verticalDeadZone = 1.5f;

        private Transform player;
        private float lastX;
        private float aheadX;
        private Vector3 velocity;

        private void Start()
        {
            offset = new Vector3(Mathf.Abs(offset.x), offset.y, offset.z);
            FindPlayer(faceLeft);
        }

        public void FindPlayer(bool playerFaceLeft)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null)
            {
                Debug.LogWarning("[CameraController] No GameObject tagged 'Player' in the scene.", this);
                return;
            }

            player = found.transform;
            lastX = player.position.x;
            faceLeft = playerFaceLeft;
            aheadX = faceLeft ? -offset.x : offset.x;

            transform.position = new Vector3(player.position.x + aheadX, player.position.y + offset.y, transform.position.z);
        }

        private void LateUpdate()
        {
            if (player == null) return;

            // Only flip the look-ahead on a real direction change, not on tiny wobble.
            float moved = player.position.x - lastX;
            if (Mathf.Abs(moved) > 0.02f)
            {
                faceLeft = moved < 0f;
                lastX = player.position.x;
            }

            float wantedAhead = faceLeft ? -offset.x : offset.x;
            aheadX = Mathf.Lerp(aheadX, wantedAhead, damping * 0.5f * Time.deltaTime);

            float targetY = player.position.y + offset.y;
            if (Mathf.Abs(targetY - transform.position.y) < verticalDeadZone)
                targetY = transform.position.y;

            Vector3 target = new Vector3(player.position.x + aheadX, targetY, transform.position.z);

            float smoothTime = 1f / Mathf.Max(0.01f, damping);

            transform.position = new Vector3(
                Mathf.SmoothDamp(transform.position.x, target.x, ref velocity.x, smoothTime),
                Mathf.SmoothDamp(transform.position.y, target.y, ref velocity.y, smoothTime / Mathf.Max(0.01f, verticalDampingScale)),
                transform.position.z);
        }
    }
}
