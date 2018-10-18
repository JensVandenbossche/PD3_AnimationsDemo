using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class CharacterControllerBehaviour : MonoBehaviour
{
    [Header("Locomotion Parameters")]
    [SerializeField]
    private float _mass = 75; // [kg]

    [SerializeField]
    private float _acceleration = 3; // [m/s^2]

    [SerializeField]
    private float _dragOnGround = 1; // []

    [SerializeField]
    private float _maxRunningSpeed = (30.0f * 1000) / (60 * 60); // [m/s], 30 km/h

    [SerializeField]
    private float _jumpHeight = 1; // [m]

    [Header("Dependencies")]
    [SerializeField, Tooltip("What should determine the absolute forward when a player presses forward.")]
    private Transform _absoluteForward;

    private CharacterController _characterController;
    private Animator _animator;

    private Vector3 _velocity = Vector3.zero;

    private Vector3 _movement;
    private bool _jump;

    private int _verticalVelocityAnimationParameter = Animator.StringToHash("VerticalVelocity");
    private int _horizontalVelocityAnimationParameter = Animator.StringToHash("HorizontalVelocity");
    private int _jumpRollAnimationParameter = Animator.StringToHash("JumpRoll");
    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

#if DEBUG
        Assert.IsNotNull(_characterController, "Dependency Error: This component needs a CharachterController to work.");
        Assert.IsNotNull(_animator, "Dependency Error: This component needs an Animator to work.");
        Assert.IsNotNull(_absoluteForward, "Dependency Error: Set the Absolute Forward field.");
#endif
    }

    void Update()
    {
        _movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        if (Input.GetButtonDown("Jump"))
        {
            _jump = true;
        }

        Vector3 velocityXZ = Vector3.Scale(_velocity, new Vector3(1, 0, 1));
        Vector3 localVelocity = gameObject.transform.InverseTransformVector(velocityXZ);

        _animator.SetFloat(_verticalVelocityAnimationParameter, localVelocity.z);
        _animator.SetFloat(_horizontalVelocityAnimationParameter, localVelocity.x);

        //jumpRoll
        if (Input.GetKeyDown(KeyCode.V))
        {
            _animator.SetTrigger(_jumpRollAnimationParameter);
        }        
    }
    
    void FixedUpdate()
    {
        Debug.Log(Time.time + " : " + _characterController.isGrounded);

        ApplyGround();
        ApplyGravity();
        ApplyMovement();
        ApplyGroundDrag();
        ApplyJump();

        LimitMaximumRunningSpeed();

        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void ApplyGround()
    {
        if (_characterController.isGrounded)
        {
            _velocity -= Vector3.Project(_velocity, Physics.gravity.normalized);
        }
    }

    private void ApplyGravity()
    {
        //if (!_characterController.isGrounded)
        {
            _velocity += Physics.gravity * Time.deltaTime; // g[m/s^2] * t[s]
        }
    }

    private void ApplyMovement()
    {
        if (_characterController.isGrounded)
        {
            Vector3 xzAbsoluteForward = Vector3.Scale(_absoluteForward.forward, new Vector3(1, 0, 1));

            Quaternion forwardRotation =
                Quaternion.LookRotation(xzAbsoluteForward);

            Vector3 relativeMovement = forwardRotation * _movement;

            _velocity += relativeMovement * _mass * _acceleration * Time.deltaTime; // F(= m.a) [m/s^2] * t [s]
        }
    }

    private void ApplyGroundDrag()
    {
        if (_characterController.isGrounded)
        {
            _velocity = _velocity * (1 - Time.deltaTime * _dragOnGround);
        }
    }

    private void ApplyJump()
    {
        //https://en.wikipedia.org/wiki/Equations_of_motion
        //v^2 = v0^2  + 2*a(r - r0)
        //v = 0
        //v0 = ?
        //a = 9.81
        //r = 1
        //r0 = 0
        //v0 = sqrt(2 * 9.81 * 1) 
        //but => g is inverted

        if (_jump && _characterController.isGrounded)
        {
            _velocity += -Physics.gravity.normalized * Mathf.Sqrt(2 * Physics.gravity.magnitude * _jumpHeight);
            _jump = false;

        }

    }

    private void LimitMaximumRunningSpeed()
    {
        Vector3 yVelocity = Vector3.Scale(_velocity, new Vector3(0, 1, 0));

        Vector3 xzVelocity = Vector3.Scale(_velocity, new Vector3(1, 0, 1));
        Vector3 clampedXzVelocity = Vector3.ClampMagnitude(xzVelocity, _maxRunningSpeed);

        _velocity = yVelocity + clampedXzVelocity;
    }
}
