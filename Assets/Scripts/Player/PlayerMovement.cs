using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Animator anim;
    private PlayerInput playerInput;

    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Movement Settings")]
    public float speed = 6f;
    public float gravity = -25f; 
    public float jumpHeight = 1.5f;

    [Header("Feel Good Mechanics")]
    public float coyoteTime = 0.1f;
    private float _coyoteTimeCounter;
    
    public float jumpBufferTime = 0.1f;
    private float _jumpBufferCounter;

    [Header("World Bounds")]
    [Tooltip("If the player falls below this world Y position, they die immediately.")]
    [SerializeField] private float fallDeathY = -25f;

    // ── Volatility Bug State ──
    private bool _controlsInverted = false;
    private bool _gravityReversed = false;
    private float _baseGravity;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        // Look for the animator on the child object
        anim = GetComponentInChildren<Animator>();
        _baseGravity = gravity;
    }

    private void OnEnable()
    {
        // Subscribe to Volatility Bug events
        GameEventManager.OnControlsInverted += HandleControlsInverted;
        GameEventManager.OnGravityReversed += HandleGravityReversed;
    }

    private void OnDisable()
    {
        GameEventManager.OnControlsInverted -= HandleControlsInverted;
        GameEventManager.OnGravityReversed -= HandleGravityReversed;
    }

    // ── Volatility Bug Handlers ──
    private void HandleControlsInverted(bool inverted)
    {
        _controlsInverted = inverted;
        Debug.Log($"[PlayerMovement] Controls inverted: {inverted}");
    }

    private void HandleGravityReversed(bool reversed)
    {
        _gravityReversed = reversed;
        gravity = reversed ? Mathf.Abs(_baseGravity) : -Mathf.Abs(_baseGravity);
        Debug.Log($"[PlayerMovement] Gravity reversed: {reversed} (gravity = {gravity})");
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnJump()
    {
        _jumpBufferCounter = jumpBufferTime;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            _coyoteTimeCounter = coyoteTime;

            if (_gravityReversed ? velocity.y > 0 : velocity.y < 0)
            {
                velocity.y = _gravityReversed ? 2f : -2f;
            }
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }

        // ── Jump Buffer & Coyote Time Resolution ──
        _jumpBufferCounter -= Time.deltaTime;
        
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            float jumpDir = _gravityReversed ? -1f : 1f;
            velocity.y = jumpDir * Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(gravity));
            
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f; // Consume to prevent double jumping
        }

        // Calculate Movement (apply inversion if active)
        float inputX = _controlsInverted ? -moveInput.x : moveInput.x;
        float inputY = _controlsInverted ? -moveInput.y : moveInput.y;
        Vector3 move = new Vector3(inputX, 0, inputY);
        // Transform move direction based on player's rotation
        move = transform.TransformDirection(move);
        
        controller.Move(move * speed * Time.deltaTime);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        CheckFallDeath();

        UpdateAnimator(move);
    }

    private void CheckFallDeath()
    {
        if (transform.position.y > fallDeathY) return;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Kill();
        }
    }

    void UpdateAnimator(Vector3 move)
    {
        anim.SetBool("isMoving", move.magnitude > 0.1f);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("verticalVelocity", velocity.y);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only bounce if we land on top of the object
        if (hit.normal.y > 0.5f)
        {
            // Check if the surface has a physics material with bounciness
            if (hit.collider.sharedMaterial != null && hit.collider.sharedMaterial.bounciness > 0.1f)
            {
                float bounciness = hit.collider.sharedMaterial.bounciness;
                float bounceHeight = 8f * bounciness; // High bounce for max bounciness

                float jumpDir = _gravityReversed ? -1f : 1f;
                velocity.y = jumpDir * Mathf.Sqrt(bounceHeight * 2f * Mathf.Abs(gravity));
            }
        }

        // Lava damage check
        LavaHazard hazard = hit.gameObject.GetComponent<LavaHazard>();
        if (hazard != null)
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(hazard.damagePerTick);
            }
        }

        // Heal station check
        HealStation healObj = hit.gameObject.GetComponent<HealStation>();
        if (healObj != null)
        {
            healObj.HealPlayer(gameObject);
        }

        // Fan Hazard check
        FanHazard fan = hit.gameObject.GetComponent<FanHazard>();
        if (fan != null)
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                // We let the FanHazard handle the condition check and apply damage
                fan.HitByPlayer(health); 
            }
        }
    }
}