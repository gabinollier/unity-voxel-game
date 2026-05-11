using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerValues PlayerValues;

    [ShowNonSerializedField] bool _isSprinting;
    [ShowNonSerializedField] bool _isSneaking;
    [ShowNonSerializedField] bool _isJumping;
    [ShowNonSerializedField] bool _isTryingToJump;

    Camera _cam;
    float _fovVelocity;
    Vector2 _moveInput;
    Vector2 _mouseDeltaInput;
    Entity _entity;


    void Awake()
    {
        _cam = Camera.main;
        _entity = GetComponent<Entity>();
    }

    void Update()
    {
        // Camera
        _mouseDeltaInput = InputsManager.Actions.Player.MoveCamera.ReadValue<Vector2>() * (PlayerValues.CameraSensibility / Time.deltaTime);

        // inputs
        _moveInput = InputsManager.Actions.Player.Move.ReadValue<Vector2>();
        _moveInput = InputsManager.Actions.Player.Move.ReadValue<Vector2>();
        _isSprinting = InputsManager.Actions.Player.Sprint.ReadValue<float>() == 1 && _moveInput.y > 0;
        _isSneaking = InputsManager.Actions.Player.Sneak.ReadValue<float>() == 1;
        if (_isSneaking) _isSprinting = false;
        _isTryingToJump = InputsManager.Actions.Player.Jump.ReadValue<float>() == 1;
        _isJumping = _isTryingToJump && _entity.IsGrounded;

        // FOV
        float desiredFOV = PlayerValues.BaseFOV * (_isSprinting ? PlayerValues.SprintFOVMultiplier : 1);
        _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, desiredFOV, ref _fovVelocity, PlayerValues.FOVChangeSmoothTime);
    }

    void FixedUpdate()
    {
        // character rotation
        transform.Rotate(Vector3.up * _mouseDeltaInput.x);

        // camera rotation
        float camDelta = -_mouseDeltaInput.y;
        if (_cam.transform.localRotation.eulerAngles.x + camDelta is < 269 and > 89) 
            camDelta = 0f;
        _cam.transform.Rotate(Vector3.right * camDelta);


        // Calculate movement (change _velocity)
        if (PlayerValues.CurrentGamemode == PlayerValues.Gamemode.Creative)
            CalculateCreativeMovement(); // freecam movement
        else
            CalculateSurvivalMovement(); // normal movement
    }
    
    void CalculateCreativeMovement()
    {
        // Calculate desired direction
        Vector3 forward = _cam.transform.forward;
        Vector3 right = _cam.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        Vector3 desiredDirection = forward * _moveInput.y + right * _moveInput.x;

        if (_isTryingToJump)
        {
            desiredDirection += Vector3.up;
            _isTryingToJump = false;
        }

        if (_isSneaking)
            desiredDirection -= Vector3.up;

        Vector3 velocity = (_isSprinting ? PlayerValues.FlySprintMultiplier : 1)
            * PlayerValues.FlySpeed
            * Time.fixedDeltaTime
            * desiredDirection;

        transform.position += velocity * Time.fixedDeltaTime;
    }

    void CalculateSurvivalMovement()
    {
        // Calculate desired direction
        Vector3 forward = _cam.transform.forward;
        Vector3 right = _cam.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        Vector3 desiredDirection = forward * _moveInput.y + right * _moveInput.x;

        // Vertical movement
        _entity.AddVelocity(Physics.gravity * Time.fixedDeltaTime);

        if (_isJumping)
        {

            _entity.AddVelocity(Vector3.up * PlayerValues.JumpForce);
            _isJumping = false;
            _entity.IsGrounded = false;
            if (_isSprinting) 
                _entity.AddVelocity(PlayerValues.JumpBoostForce * forward);
        }

        // Horizontal movement
        float multiplier = (
            _isSprinting ? PlayerValues.SprintMultiplier :
            _isSneaking ? PlayerValues.SneakMultiplier : 1)
            * (!_entity.IsGrounded ? PlayerValues.AirMultiplier : 1);

        Vector3 acceleration = (PlayerValues.BaseAcceleration * multiplier) * desiredDirection;
        Vector3 friction = -(_entity.IsGrounded ? PlayerValues.GroundFriction : PlayerValues.AirFriction) * new Vector3(_entity.Velocity.x, 0f, _entity.Velocity.z);
        _entity.AddVelocity((acceleration + friction) * Time.fixedDeltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (PlayerValues.HitboxHeight/2), new Vector3(PlayerValues.HitboxWidth * 2, PlayerValues.HitboxHeight , PlayerValues.HitboxWidth * 2));
    }
}
