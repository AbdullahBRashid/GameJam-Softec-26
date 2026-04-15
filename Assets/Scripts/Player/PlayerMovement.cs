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
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnJump()
    {
        if (isGrounded)
        {
            // Jump formula: v = sqrt(h * -2 * g)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keeps character glued to slopes
        }

        // Calculate Movement
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        // Transform move direction based on player's rotation
        move = transform.TransformDirection(move);
        
        controller.Move(move * speed * Time.deltaTime);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(move);
    }

    void UpdateAnimator(Vector3 move)
    {
        anim.SetBool("isMoving", move.magnitude > 0.1f);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("verticalVelocity", velocity.y);
    }
}