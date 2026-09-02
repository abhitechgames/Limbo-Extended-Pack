using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Weighty platformer controller in the style of the classic dark puzzle-platformers:
    /// the character builds up speed instead of snapping to it, the jump arcs and can be
    /// cut short, and there is a little forgiveness on both sides of a ledge.
    /// Also handles ladders, dragging crates and swimming.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Player Controller")]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float movingSpeed = 6.5f;

        [Tooltip("How fast the character reaches full speed on the ground.")]
        public float groundAcceleration = 55f;

        [Tooltip("How fast it slides to a stop. Lower = heavier, more momentum.")]
        public float groundDeceleration = 40f;

        [Tooltip("Steering while airborne. Keep it below the ground values.")]
        public float airAcceleration = 28f;
        public float airDeceleration = 12f;

        [Header("Jump")]
        [Tooltip("Upward impulse. Applied against the Rigidbody mass, so heavier = lower jump.")]
        public float jumpForce = 40f;

        [Tooltip("Extra gravity on the way down - the reason the jump does not feel floaty.")]
        [Min(1f)] public float fallGravityMultiplier = 1.25f;

        [Tooltip("Gravity used when the jump button is released early, for short hops.")]
        [Min(1f)] public float lowJumpMultiplier = 2.2f;

        [Tooltip("Terminal velocity, so long drops stay readable.")]
        public float maxFallSpeed = 32f;

        [Tooltip("Grace period after walking off a ledge where a jump still works.")]
        [Range(0f, 0.3f)] public float coyoteTime = 0.12f;

        [Tooltip("A jump pressed this early before landing still fires.")]
        [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;

        [Header("Ladder")]
        public float climbSpeed = 4f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.3f;

        [Tooltip("What counts as solid ground. Triggers are ignored either way.")]
        public LayerMask groundLayers = ~0;

        [Header("Pushing")]
        [Tooltip("Hold to grab a crate, then walk to push or pull it.")]
        public KeyCode grabKey = KeyCode.E;

        [Tooltip("How far in front the character can reach for a crate.")]
        public float grabDistance = 1.1f;

        [Tooltip("Let go once the crate ends up this far away.")]
        public float grabBreakDistance = 2.2f;

        public LayerMask grabLayers = ~0;

        [Header("Water")]
        [Tooltip("Colliders on these layers count as water.")]
        public LayerMask waterLayers = 0;

        [Range(0.1f, 1f)] public float swimSpeedFactor = 0.55f;

        [Tooltip("Gravity while submerged. The rest of the lift comes from buoyancy below.")]
        [Range(0f, 1f)] public float swimGravityScale = 0.3f;

        [Tooltip("How strongly the character is pushed back up to the surface.")]
        public float buoyancy = 14f;

        public float waterDrag = 2.5f;

        [Tooltip("Upward kick when the jump button is tapped while swimming.")]
        public float swimStrokeForce = 14f;

        [Tooltip("Seconds between strokes, so holding jump does not rocket you out.")]
        public float strokeInterval = 0.22f;

        [Tooltip("Jump power kept when pushing off the bottom, a boat, or the surface.")]
        [Range(0.2f, 1f)] public float wetJumpScale = 0.8f;

        [Tooltip("Within this depth of the surface, jump vaults you out instead of stroking.")]
        public float surfaceJumpDepth = 0.9f;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private float moveInput;
        private float verticalInput;
        private bool jumpHeld;

        private bool isGrounded;
        private bool isNearLadder;
        private bool isClimbing;
        private Collider2D currentLadder;

        private float normalGravity;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private int facing = 1;

        private PushableProp grabbed;

        private int waterContacts;
        private float waterSurface;
        private float strokeCooldown;

        private ContactFilter2D groundFilter;
        private readonly Collider2D[] groundHits = new Collider2D[8];

        public bool IsGrounded { get { return isGrounded; } }
        public bool IsInWater { get { return waterContacts > 0; } }

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            normalGravity = rb.gravityScale;

            groundFilter = new ContactFilter2D();
            groundFilter.useTriggers = false;
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.useLayerMask = true;
        }

        private void Update()
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            jumpHeld = Input.GetKey(KeyCode.Space);

            CheckGround();

            if (Input.GetKeyDown(KeyCode.Space)) jumpBufferCounter = jumpBufferTime;
            else jumpBufferCounter -= Time.deltaTime;

            coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
            strokeCooldown -= Time.deltaTime;

            if (!isClimbing && isNearLadder && currentLadder != null && verticalInput != 0f)
                StartClimbing();

            HandleGrab();

            if (!isClimbing)
                HandleJump();

            UpdateFacing();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            if (isClimbing)
            {
                ClimbMovement();
                return;
            }

            HorizontalMovement();
            ApplyGravityFeel();
            DragGrabbed();
        }

        // =====================================================
        // MOVEMENT
        // =====================================================

        private void HorizontalMovement()
        {
            float speed = movingSpeed;
            if (grabbed != null) speed *= grabbed.SpeedFactor;
            if (IsInWater) speed *= swimSpeedFactor;

            float target = moveInput * speed;
            bool wantsToMove = Mathf.Abs(target) > 0.01f;

            float rate = isGrounded
                ? (wantsToMove ? groundAcceleration : groundDeceleration)
                : (wantsToMove ? airAcceleration : airDeceleration);

            float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        private void HandleJump()
        {
            if (jumpBufferCounter <= 0f) return;

            if (IsInWater)
            {
                // Feet on the riverbed or a boat, or just bobbing at the top - all give a
                // real jump so you can get out. Deeper down you kick upwards instead.
                bool nearSurface = (waterSurface - transform.position.y) <= surfaceJumpDepth;

                if (coyoteCounter > 0f || nearSurface)
                {
                    jumpBufferCounter = 0f;
                    coyoteCounter = 0f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * jumpForce * wetJumpScale, ForceMode2D.Impulse);
                }
                else if (strokeCooldown <= 0f)
                {
                    jumpBufferCounter = 0f;
                    strokeCooldown = strokeInterval;
                    rb.AddForce(Vector2.up * swimStrokeForce, ForceMode2D.Impulse);
                }
                return;
            }

            if (coyoteCounter <= 0f) return;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        private void ApplyGravityFeel()
        {
            if (IsInWater)
            {
                rb.gravityScale = normalGravity * swimGravityScale;
                rb.linearDamping = waterDrag;

                // Push back towards the surface, harder the deeper we are.
                float depth = waterSurface - transform.position.y;
                if (depth > 0f)
                    rb.AddForce(Vector2.up * buoyancy * Mathf.Clamp01(depth), ForceMode2D.Force);

                return;
            }

            rb.linearDamping = 0f;

            if (isGrounded)
                rb.gravityScale = normalGravity;
            else if (rb.linearVelocity.y < -0.01f)
                rb.gravityScale = normalGravity * fallGravityMultiplier;
            else if (rb.linearVelocity.y > 0.01f && !jumpHeld)
                rb.gravityScale = normalGravity * lowJumpMultiplier;
            else
                rb.gravityScale = normalGravity;

            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }

        // =====================================================
        // PUSHING CRATES
        // =====================================================

        private void HandleGrab()
        {
            if (!Input.GetKey(grabKey) || isClimbing)
            {
                ReleaseGrab();
                return;
            }

            // Feet on something, or treading water - both let you shove a crate or a boat.
            bool canHold = isGrounded || IsInWater;

            if (grabbed != null)
            {
                float gap = Vector2.Distance(transform.position, grabbed.transform.position);
                if (gap > grabBreakDistance || !canHold) ReleaseGrab();
                return;
            }

            if (!canHold) return;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * facing, grabDistance, grabLayers);
            if (hit.collider == null) return;

            PushableProp prop = hit.collider.GetComponentInParent<PushableProp>();
            if (prop == null) return;

            grabbed = prop;
            grabbed.OnGrabbed();
        }

        private void ReleaseGrab()
        {
            if (grabbed == null) return;

            grabbed.OnReleased();
            grabbed = null;
        }

        private void DragGrabbed()
        {
            if (grabbed == null) return;

            Rigidbody2D body = grabbed.Body;
            body.linearVelocity = new Vector2(rb.linearVelocity.x, body.linearVelocity.y);
        }

        // =====================================================
        // LADDERS
        // =====================================================

        private void StartClimbing()
        {
            if (currentLadder == null) return;

            ReleaseGrab();

            isClimbing = true;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.linearVelocity = Vector2.zero;

            Vector3 p = transform.position;
            p.x = currentLadder.bounds.center.x;
            transform.position = p;
        }

        private void ClimbMovement()
        {
            if (currentLadder == null)
            {
                StopClimbing();
                return;
            }

            Vector2 p = rb.position;
            p.x = currentLadder.bounds.center.x;
            rb.position = p;

            rb.linearVelocity = new Vector2(0f, verticalInput * climbSpeed);

            // Stepping off sideways, or jumping, drops you back into normal movement.
            if (moveInput != 0f || Input.GetKeyDown(KeyCode.Space))
                StopClimbing();
        }

        private void StopClimbing()
        {
            if (!isClimbing) return;

            isClimbing = false;
            rb.gravityScale = normalGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        // =====================================================
        // TRIGGERS
        // =====================================================

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Ladder"))
            {
                isNearLadder = true;
                currentLadder = other;
            }

            if (IsWater(other))
            {
                waterContacts++;
                waterSurface = other.bounds.max.y;
                StopClimbing();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsWater(other)) waterSurface = other.bounds.max.y;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Ladder") && currentLadder == other)
            {
                isNearLadder = false;
                StopClimbing();
                currentLadder = null;
            }

            if (IsWater(other)) waterContacts = Mathf.Max(0, waterContacts - 1);
        }

        private bool IsWater(Collider2D other)
        {
            return (waterLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        // =====================================================
        // GROUND CHECK
        // =====================================================

        private void CheckGround()
        {
            isGrounded = false;
            if (groundCheck == null) return;

            int count = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundFilter, groundHits);

            for (int i = 0; i < count; i++)
            {
                if (groundHits[i] == null || groundHits[i].transform.IsChildOf(transform)) continue;

                isGrounded = true;
                return;
            }
        }

        // =====================================================
        // PRESENTATION
        // =====================================================

        private void UpdateFacing()
        {
            // Keep facing the crate you are dragging, otherwise face where you walk.
            if (grabbed != null || Mathf.Abs(moveInput) < 0.01f) return;

            facing = moveInput > 0f ? 1 : -1;

            if (spriteRenderer != null)
                spriteRenderer.flipX = facing < 0;
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            int state;

            if (isClimbing) state = 3;
            else if (!isGrounded && !IsInWater) state = 2;
            else if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) state = 1;
            else state = 0;

            animator.SetInteger("playerState", state);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * facing * grabDistance);
        }
    }
}
