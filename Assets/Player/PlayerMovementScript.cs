using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    [Header("Movement Settings")]
    /// <summary>
    /// The speed at which the player moves while walking
    /// </summary>
    [SerializeField] private float walkSpeed;

    /// <summary>
    /// The speed at which the player moves while crouched
    /// </summary>
    [SerializeField] private float crouchedSpeed;

    /// <summary>
    /// The speed at which the player moves while sprinting
    /// </summary>
    [SerializeField] private float sprintSpeed;


    [Header("Materials")]
    [SerializeField] private Material walkMat;
    [SerializeField] private Material crouchMat;
    [SerializeField] private Material sprintMat;


    /// <summary>
    /// The player's character controller
    /// </summary>
    private CharacterController characterController;
    private MeshRenderer mesh;

    /// <summary>
    /// The player's movement input
    /// </summary>
    private Vector2 movementInput = Vector2.zero;

    /// <summary>
    /// The working player velocity
    /// </summary>
    private Vector3 playerVelocity = Vector3.zero;

    public bool isCrouching = false;
    public bool isSprinting = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializePlayer();
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        ChangeMaterial();
        OrientPlayer();
        MovePlayer();
        GetCurrentVelocity();
    }

    /// <summary>
    /// InitializePlayer gets the character controller component
    /// </summary>
    private void InitializePlayer()
    {
        characterController = GetComponent<CharacterController>();

        if (!characterController)
        {
            Debug.LogError($"{this.gameObject} is missing a Character Controller");
        }


        mesh = GetComponent<MeshRenderer>();

        if (!mesh)
        {
            Debug.LogError($"{this.gameObject} is missing a Mesh Renderer");
        }
    }

    /// <summary>
    /// GetCurrentVelocity updates the playerVelocity variable to the player's current velocity
    /// </summary>
    private void GetCurrentVelocity()
    {
        playerVelocity = characterController.velocity;
    }

    /// <summary>
    /// HandleMovement changes the player velocity based on the inputs
    /// </summary>
    private void HandleMovement()
    {
        if (isSprinting)
        {
            playerVelocity.x = movementInput.x * sprintSpeed;
            playerVelocity.z = movementInput.y * sprintSpeed;
        }
        else if (isCrouching)
        {
            playerVelocity.x = movementInput.x * crouchedSpeed;
            playerVelocity.z = movementInput.y * crouchedSpeed;
        }
        else
        {
            playerVelocity.x = movementInput.x * walkSpeed;
            playerVelocity.z = movementInput.y * walkSpeed;
        }
        playerVelocity.y = 0;
    }

    /// <summary>
    /// ChangeMaterial alters the player's material while changing state
    /// </summary>
    private void ChangeMaterial()
    {
        if (isSprinting)
        {
            mesh.material = sprintMat;
        }
        else if (isCrouching)
        {
            mesh.material = crouchMat;
        }
        else
        {
            mesh.material = walkMat;
        }
    }

    /// <summary>
    /// OrientPlayer makes the player face the direction of movement
    /// </summary>
    private void OrientPlayer()
    {
        Vector3 moveDirection = new Vector3(movementInput.x, 0f, movementInput.y);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// MovePlayer applys the final velocity to the player
    /// </summary>
    private void MovePlayer()
    {
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    /// <summary>
    /// OnMove reads the player input
    /// </summary>
    /// <param name="value"></param>
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Crouch reads the player input
    /// </summary>
    /// <param name="value"></param>
    public void Crouch(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.1)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
    }

    /// <summary>
    /// Sprint reads the player input
    /// </summary>
    /// <param name="value"></param>
    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.1)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }
    }
}
