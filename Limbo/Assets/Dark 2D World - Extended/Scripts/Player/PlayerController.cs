using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    /// <summary>
    /// Weighty platformer controller in the style of the classic dark puzzle-platformers:
    /// the character builds up speed instead of snapping to it, the jump arcs and can be
    /// cut short, and there is a little forgiveness on both sides of a ledge.
    /// Also handles ladders, dragging crates and swimming.
    ///
    /// Every value below is safe to change while the game is running - find a feel you
    /// like in Play mode, then copy the component and paste the values back after.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark 2D World/Player Controller")]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // =====================================================
        [Header("Input")]
        // =====================================================

        [Tooltip("Key used to jump, swim upwards, and jump off ladders.")]
        public KeyCode jumpKey = KeyCode.Space;

        [Tooltip("Hold to grab a crate, then walk to push or pull it.")]
        public KeyCode grabKey = KeyCode.E;

        [Tooltip("Input Manager axis for walking. Leave as Horizontal unless you renamed it.")]
        public string horizontalAxis = "Horizontal";

        [Tooltip("Input Manager axis for climbing ladders.")]
        public string verticalAxis = "Vertical";

        // =====================================================
        [Header("Movement")]
        // =====================================================

        [Tooltip("Top walking speed, in units per second.")]
        [Min(0f)] public float movingSpeed = 6.5f;

        [Tooltip("How fast the character reaches full speed on the ground. Lower = more sluggish.")]
        [Min(0f)] public float groundAcceleration = 55f;

        [Tooltip("How fast it slides to a stop. Lower = heavier, more momentum.")]
        [Min(0f)] public float groundDeceleration = 40f;

        [Tooltip("Steering while airborne. Keep it below the ground values.")]
        [Min(0f)] public float airAcceleration = 28f;

        [Tooltip("How much the character coasts in the air when you let go of the stick.")]
        [Min(0f)] public float airDeceleration = 12f;

        // =====================================================
        [Header("Jump")]
        // =====================================================

        [Tooltip("Upward impulse. Applied against the Rigidbody mass, so a heavier body jumps lower.")]
        [Min(0f)] public float jumpForce = 40f;

        [Tooltip("Extra gravity on the way down - the reason the jump does not feel floaty.")]
        [Range(1f, 4f)] public float fallGravityMultiplier = 1.25f;

        [Tooltip("Gravity used when the jump button is released early, for short hops.")]
        [Range(1f, 6f)] public float lowJumpMultiplier = 2.2f;

        [Tooltip("Terminal velocity, so long drops stay readable.")]
        [Min(1f)] public float maxFallSpeed = 32f;

        [Tooltip("Grace period after walking off a ledge where a jump still works.")]
        [Range(0f, 0.3f)] public float coyoteTime = 0.12f;

        [Tooltip("A jump pressed this early before landing still fires.")]
        [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;

        // =====================================================
        [Header("Ladder")]
        // =====================================================

        [Tooltip("Climb speed, in units per second.")]
        [Min(0f)] public float climbSpeed = 4f;

        [Tooltip("How much of a normal jump you get when jumping off a ladder.")]
        [Range(0.2f, 1f)] public float ladderJumpScale = 0.85f;

        [Tooltip("Sideways shove when you jump off while holding a direction. 0 = straight up.")]
        [Min(0f)] public float ladderJumpPush = 4.5f;

        [Tooltip("Pause before the same ladder can be grabbed again, so you do not snap straight back on.")]
        [Range(0f, 1f)] public float ladderRegrabDelay = 0.25f;

        [Tooltip("Walking left or right lets go of the ladder.")]
        public bool stepOffLadderSideways = true;

        // =====================================================
        [Header("Ground Check")]
        // =====================================================

        [Tooltip("Empty child object at the character's feet. Required.")]
        public Transform groundCheck;

        [Tooltip("Radius of the circle tested at the feet. Roughly half the character's width.")]
        [Min(0.01f)] public float groundCheckRadius = 0.3f;

        [Tooltip("What counts as solid ground. Triggers are ignored either way.")]
        public LayerMask groundLayers = ~0;

        // =====================================================
        [Header("Pushing Crates")]
        // =====================================================

        [Tooltip("How far in front the character can reach for a crate.")]
        [Min(0f)] public float grabDistance = 1.1f;

        [Tooltip("Let go once the crate ends up this far away.")]
        [Min(0f)] public float grabBreakDistance = 2.2f;

        [Tooltip("Layers holding crates. Must not include the player's own layer.")]
        public LayerMask grabLayers = ~0;

        // =====================================================
        [Header("Water")]
        // =====================================================

        [Tooltip("Colliders on these layers count as water.")]
        public LayerMask waterLayers = 0;

        [Tooltip("Fraction of walking speed kept while swimming.")]
        [Range(0.1f, 1f)] public float swimSpeedFactor = 0.55f;

        [Tooltip("Gravity while submerged. The rest of the lift comes from buoyancy below.")]
        [Range(0f, 1f)] public float swimGravityScale = 0.3f;

        [Tooltip("How strongly the character is pushed back up to the surface.")]
        [Min(0f)] public float buoyancy = 14f;

        [Tooltip("Water resistance. Higher feels thicker and slows the character sooner.")]
        [Min(0f)] public float waterDrag = 2.5f;

        [Tooltip("Upward kick when the jump button is tapped while swimming.")]
        [Min(0f)] public float swimStrokeForce = 14f;

        [Tooltip("Seconds between strokes, so holding jump does not rocket you out.")]
        [Range(0.05f, 1f)] public float strokeInterval = 0.22f;

        [Tooltip("Jump power kept when pushing off the bottom, a boat, or the surface.")]
        [Range(0.2f, 1f)] public float wetJumpScale = 0.8f;

        [Tooltip("Within this depth of the surface, jump vaults you out instead of stroking.")]
        [Min(0f)] public float surfaceJumpDepth = 0.9f;

        // =====================================================
        // STATE - readable from other scripts and the inspector
        // =====================================================

        public bool IsGrounded { get { return isGrounded; } }
        public bool IsInWater { get { return waterContacts > 0; } }
        public bool IsClimbing { get { return isClimbing; } }
        public bool IsPushing { get { return grabbed != null; } }
        public int Facing { get { return facing; } }
        public Vector2 Velocity { get { return rb != null ? rb.linearVelocity : Vector2.zero; } }

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private float moveInput;
        private float verticalInput;
        private bool jumpHeld;
        private bool jumpPressed;

        private bool isGrounded;
        private bool isNearLadder;
        private bool isClimbing;
        private Collider2D currentLadder;

        private float normalGravity;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private float ladderLockout;
        private int facing = 1;

        private PushableProp grabbed;

        private int waterContacts;
        private float waterSurface;
        private float strokeCooldown;

        private ContactFilter2D groundFilter;
        private readonly Collider2D[] groundHits = new Collider2D[8];

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

            ValidateAxes();

            if (groundCheck == null)
                Debug.LogWarning("[PlayerController] No Ground Check assigned - the character will never be grounded.", this);
        }

        /// <summary>Falls back to the built-in axis names if the ones typed in do not exist.</summary>
        private void ValidateAxes()
        {
            try { Input.GetAxisRaw(horizontalAxis); }
            catch
            {
                Debug.LogWarning("[PlayerController] No input axis named '" + horizontalAxis + "', using 'Horizontal'.", this);
                horizontalAxis = "Horizontal";
            }

            try { Input.GetAxisRaw(verticalAxis); }
            catch
            {
                Debug.LogWarning("[PlayerController] No input axis named '" + verticalAxis + "', using 'Vertical'.", this);
                verticalAxis = "Vertical";
            }
        }

        private void Update()
        {
            ReadInput();
            CheckGround();
            TickTimers();

            if (isClimbing)
            {
                // Jump is read here rather than in FixedUpdate. GetKeyDown is only true for
                // one frame, and FixedUpdate can miss that frame entirely or run twice in it.
                if (jumpBufferCounter > 0f) LadderJump();
                else if (stepOffLadderSideways && Mathf.Abs(moveInput) > 0.01f) LeaveLadder();
            }
            else
            {
                TryStartClimbing();
                HandleJump();
            }

            HandleGrab();
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

        private void ReadInput()
        {
            moveInput = Input.GetAxisRaw(horizontalAxis);
            verticalInput = Input.GetAxisRaw(verticalAxis);
            jumpHeld = Input.GetKey(jumpKey);
            jumpPressed = Input.GetKeyDown(jumpKey);
        }

        private void TickTimers()
        {
            if (jumpPressed) jumpBufferCounter = jumpBufferTime;
            else jumpBufferCounter -= Time.deltaTime;

            coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
            strokeCooldown -= Time.deltaTime;
            ladderLockout -= Time.deltaTime;
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

        private void TryStartClimbing()
        {
            if (ladderLockout > 0f) return;
            if (!isNearLadder || currentLadder == null) return;
            if (Mathf.Abs(verticalInput) < 0.01f) return;

            StartClimbing();
        }

        private void StartClimbing()
        {
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
        }

        /// <summary>Push off the ladder into a real jump, angled if a direction is held.</summary>
        private void LadderJump()
        {
            jumpBufferCounter = 0f;
            LeaveLadder();

            rb.linearVelocity = new Vector2(moveInput * ladderJumpPush, 0f);
            rb.AddForce(Vector2.up * jumpForce * ladderJumpScale, ForceMode2D.Impulse);

            coyoteCounter = 0f;
        }

        /// <summary>Let go on purpose, with a short pause before the ladder can be re-grabbed.</summary>
        private void LeaveLadder()
        {
            if (!isClimbing) return;

            StopClimbing();
            ladderLockout = ladderRegrabDelay;
        }

        private void StopClimbing()
        {
            if (!isClimbing) return;

            isClimbing = false;
            rb.gravityScale = normalGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // A moment of grace, so a jump pressed just after letting go still fires.
            coyoteCounter = coyoteTime;
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
            if (groundCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * facing * grabDistance);
        }
    }
}
