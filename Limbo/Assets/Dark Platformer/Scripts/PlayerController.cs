using UnityEngine;

namespace GameSeed.DarkPlatformer
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float movingSpeed = 5f;
        public float jumpForce = 8f;

        [Header("Ladder")]
        public float climbSpeed = 4f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.3f;

        private Rigidbody2D rb;
        private Animator animator;

        private float moveInput;
        private float verticalInput;

        private bool isGrounded;
        private bool isNearLadder;
        private bool isClimbing;

        private Collider2D currentLadder;

        private float normalGravity;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();

            // Save original gravity
            normalGravity = rb.gravityScale;
        }

        private void Update()
        {
            // =========================
            // INPUT
            // =========================

            moveInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            // =========================
            // GROUND CHECK
            // =========================

            CheckGround();

            // =========================
            // START CLIMBING
            // =========================

            if (!isClimbing && isNearLadder && currentLadder != null)
            {
                if (verticalInput != 0)
                {
                    StartClimbing();
                }
            }

            // =========================
            // NORMAL MOVEMENT
            // =========================

            if (!isClimbing)
            {
                HandleNormalMovement();
            }

            // =========================
            // ANIMATION
            // =========================

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            if (isClimbing)
            {
                ClimbMovement();
            }
            else
            {
                NormalMovementPhysics();
            }
        }

        // =====================================================
        // NORMAL MOVEMENT
        // =====================================================

        private void HandleNormalMovement()
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    0f
                );

                rb.AddForce(
                    Vector2.up * jumpForce,
                    ForceMode2D.Impulse
                );
            }
        }

        private void NormalMovementPhysics()
        {
            rb.linearVelocity = new Vector2(
                moveInput * movingSpeed,
                rb.linearVelocity.y
            );
        }

        // =====================================================
        // START CLIMBING
        // =====================================================

        private void StartClimbing()
        {
            // Safety check
            if (currentLadder == null)
                return;

            isClimbing = true;

            // Disable gravity
            rb.gravityScale = 0f;

            // Stop current velocity
            rb.linearVelocity = Vector2.zero;

            // Snap player to ladder center
            Vector3 playerPosition = transform.position;

            playerPosition.x = currentLadder.bounds.center.x;

            transform.position = playerPosition;
        }

        // =====================================================
        // CLIMB MOVEMENT
        // =====================================================

        private void ClimbMovement()
        {
            // IMPORTANT:
            // Ladder no longer exists / player exited ladder
            if (currentLadder == null)
            {
                StopClimbing();
                return;
            }

            // Keep player centered on ladder
            Vector2 currentPosition = rb.position;

            currentPosition.x = currentLadder.bounds.center.x;

            rb.position = currentPosition;

            // Up / Down movement
            rb.linearVelocity = new Vector2(
                0f,
                verticalInput * climbSpeed
            );

            // Left / Right exits climbing
            if (moveInput != 0)
            {
                StopClimbing();
            }
        }

        // =====================================================
        // STOP CLIMBING
        // =====================================================

        private void StopClimbing()
        {
            isClimbing = false;

            // Restore gravity
            rb.gravityScale = normalGravity;

            // Stop vertical climbing movement
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                0f
            );
        }

        // =====================================================
        // LADDER ENTER
        // =====================================================

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Ladder"))
                return;

            isNearLadder = true;
            currentLadder = other;
        }

        // =====================================================
        // LADDER EXIT
        // =====================================================

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Ladder"))
                return;

            if (currentLadder == other)
            {
                isNearLadder = false;

                // IMPORTANT:
                // Stop climbing BEFORE removing ladder reference
                if (isClimbing)
                {
                    StopClimbing();
                }

                currentLadder = null;
            }
        }

        // =====================================================
        // GROUND CHECK
        // =====================================================

        private void CheckGround()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(
                groundCheck.position,
                groundCheckRadius
            );

            isGrounded = false;

            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject != gameObject)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // =====================================================
        // ANIMATION
        // =====================================================

        private void UpdateAnimation()
        {
            if (isClimbing)
            {
                animator.SetInteger("playerState", 3);
            }
            else if (!isGrounded)
            {
                animator.SetInteger("playerState", 2);
            }
            else if (moveInput != 0)
            {
                animator.SetInteger("playerState", 1);
            }
            else
            {
                animator.SetInteger("playerState", 0);
            }
        }

        // =====================================================
        // GIZMOS
        // =====================================================

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }
}