using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        // =========================================================
        // PLAYER
        // =========================================================

        [Header("Player")]

        public float MoveSpeed = 2.0f;

        public float SprintSpeed = 5.335f;

        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        public float SpeedChangeRate = 10.0f;


        // =========================================================
        // LOCK-ON
        // =========================================================

        [Header("Lock-On Movement")]

        public float LockOnRotationSpeed = 12f;


        // =========================================================
        // AUDIO
        // =========================================================

        [Header("Audio")]

        public AudioClip LandingAudioClip;

        public AudioClip[] FootstepAudioClips;

        [Range(0, 1)]
        public float FootstepAudioVolume = 0.5f;


        // =========================================================
        // JUMP / GRAVITY
        // =========================================================

        [Header("Jump / Gravity")]

        public float JumpHeight = 1.2f;

        public float Gravity = -15.0f;

        public float JumpTimeout = 0.50f;

        public float FallTimeout = 0.15f;


        // =========================================================
        // GROUNDED
        // =========================================================

        [Header("Player Grounded")]

        public bool Grounded = true;

        public float GroundedOffset = -0.14f;

        public float GroundedRadius = 0.28f;

        public LayerMask GroundLayers;


        // =========================================================
        // CINEMACHINE
        // =========================================================

        [Header("Cinemachine")]

        public GameObject CinemachineCameraTarget;

        public float TopClamp = 70.0f;

        public float BottomClamp = -30.0f;

        public float CameraAngleOverride = 0.0f;

        public bool LockCameraPosition = false;


        // =========================================================
        // CAMERA VARIABLES
        // =========================================================

        private float _cinemachineTargetYaw;

        private float _cinemachineTargetPitch;


        // =========================================================
        // PLAYER VARIABLES
        // =========================================================

        private float _speed;

        private float _animationBlend;

        private float _targetRotation;

        private float _rotationVelocity;

        private float _verticalVelocity;

        private float _terminalVelocity = 53.0f;


        // =========================================================
        // TIMEOUTS
        // =========================================================

        private float _jumpTimeoutDelta;

        private float _fallTimeoutDelta;


        // =========================================================
        // ANIMATION IDS
        // =========================================================

        private int _animIDSpeed;

        private int _animIDGrounded;

        private int _animIDJump;

        private int _animIDFreeFall;

        private int _animIDMotionSpeed;

        private int _animIDLockedOn;

        private int _animIDMoveX;

        private int _animIDMoveY;


        // =========================================================
        // REFERENCES
        // =========================================================

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private Animator _animator;

        private CharacterController _controller;

        private StarterAssetsInputs _input;

        private GameObject _mainCamera;

        private PlayerController playerController;

        private LockOnManager lockOnManager;


        // =========================================================
        // CONSTANTS
        // =========================================================

        private const float _threshold = 0.01f;


        // =========================================================
        // ANIMATOR
        // =========================================================

        private bool _hasAnimator;


        // =========================================================
        // INPUT DEVICE
        // =========================================================

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }


        // =========================================================
        // AWAKE
        // =========================================================

        private void Awake()
        {
            playerController =
                GetComponent<PlayerController>();

            lockOnManager =
                FindObjectOfType<LockOnManager>();

            if (_mainCamera == null)
            {
                _mainCamera =
                    GameObject.FindGameObjectWithTag("MainCamera");
            }
        }


        // =========================================================
        // START
        // =========================================================

        private void Start()
        {
            _cinemachineTargetYaw =
                CinemachineCameraTarget
                .transform
                .rotation
                .eulerAngles
                .y;

            _hasAnimator =
                TryGetComponent(out _animator);

            _controller =
                GetComponent<CharacterController>();

            _input =
                GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
            _playerInput =
                GetComponent<PlayerInput>();
#else
            Debug.LogError(
                "Starter Assets package is missing dependencies."
            );
#endif

            AssignAnimationIDs();

            _jumpTimeoutDelta =
                JumpTimeout;

            _fallTimeoutDelta =
                FallTimeout;
        }


        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            _hasAnimator =
                TryGetComponent(out _animator);

            if (lockOnManager == null)
            {
                lockOnManager =
                    FindObjectOfType<LockOnManager>();
            }

            JumpAndGravity();

            GroundedCheck();

            Move();
        }


        // =========================================================
        // LATE UPDATE
        // =========================================================

        private void LateUpdate()
        {
            CameraRotation();
        }


        // =========================================================
        // ANIMATION IDS
        // =========================================================

        private void AssignAnimationIDs()
        {
            _animIDSpeed =
                Animator.StringToHash("Speed");

            _animIDGrounded =
                Animator.StringToHash("Grounded");

            _animIDJump =
                Animator.StringToHash("Jump");

            _animIDFreeFall =
                Animator.StringToHash("FreeFall");

            _animIDMotionSpeed =
                Animator.StringToHash("MotionSpeed");

            _animIDLockedOn =
                Animator.StringToHash("IsLockedOn");

            _animIDMoveX =
                Animator.StringToHash("MoveX");

            _animIDMoveY =
                Animator.StringToHash("MoveY");
        }


        // =========================================================
        // LOCK-ON CHECK
        // =========================================================

        private bool IsLockedOn()
        {
            return lockOnManager != null &&
                   lockOnManager.IsLockedOn &&
                   lockOnManager.currentTarget != null;
        }


        // =========================================================
        // GROUNDED CHECK
        // =========================================================

        private void GroundedCheck()
        {
            Vector3 spherePosition =
                new Vector3(
                    transform.position.x,
                    transform.position.y - GroundedOffset,
                    transform.position.z
                );

            Grounded =
                Physics.CheckSphere(
                    spherePosition,
                    GroundedRadius,
                    GroundLayers,
                    QueryTriggerInteraction.Ignore
                );

            if (_hasAnimator)
            {
                _animator.SetBool(
                    _animIDGrounded,
                    Grounded
                );
            }
        }


        // =========================================================
        // CAMERA ROTATION
        // =========================================================

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold &&
                !LockCameraPosition)
            {
                float deltaTimeMultiplier =
                    IsCurrentDeviceMouse
                        ? 1.0f
                        : Time.deltaTime;

                _cinemachineTargetYaw +=
                    _input.look.x *
                    deltaTimeMultiplier;

                _cinemachineTargetPitch +=
                    _input.look.y *
                    deltaTimeMultiplier;
            }

            _cinemachineTargetYaw =
                ClampAngle(
                    _cinemachineTargetYaw,
                    float.MinValue,
                    float.MaxValue
                );

            _cinemachineTargetPitch =
                ClampAngle(
                    _cinemachineTargetPitch,
                    BottomClamp,
                    TopClamp
                );

            CinemachineCameraTarget.transform.rotation =
                Quaternion.Euler(
                    _cinemachineTargetPitch +
                    CameraAngleOverride,
                    _cinemachineTargetYaw,
                    0.0f
                );
        }


        // =========================================================
        // MOVEMENT
        // =========================================================

        private void Move()
        {
            // =====================================================
            // ATTACK LOCK
            // =====================================================

            if (playerController.isAttacking)
            {
                _speed = 0f;

                _animationBlend = 0f;

                if (_hasAnimator)
                {
                    _animator.SetFloat(
                        _animIDSpeed,
                        0f
                    );

                    _animator.SetFloat(
                        _animIDMotionSpeed,
                        0f
                    );

                    _animator.SetFloat(
                        _animIDMoveX,
                        0f
                    );

                    _animator.SetFloat(
                        _animIDMoveY,
                        0f
                    );
                }

                _controller.Move(
                    new Vector3(
                        0f,
                        _verticalVelocity,
                        0f
                    ) * Time.deltaTime
                );

                return;
            }


            // =====================================================
            // OTHER ACTION LOCKS
            // =====================================================

            if (playerController.isEquipping ||
                playerController.isBlocking ||
                playerController.isKicking ||
                playerController.isDodging)
            {
                return;
            }


            // =====================================================
            // LOCK-ON MOVEMENT
            // =====================================================

            if (IsLockedOn())
            {
                MoveWhileLockedOn();

                return;
            }


            // =====================================================
            // NORMAL MOVEMENT
            // =====================================================

            MoveNormally();
        }


        // =========================================================
        // NORMAL MOVEMENT
        // =========================================================

        private void MoveNormally()
        {
            float targetSpeed =
                _input.sprint
                    ? SprintSpeed
                    : MoveSpeed;


            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0f;
            }


            float currentHorizontalSpeed =
                new Vector3(
                    _controller.velocity.x,
                    0f,
                    _controller.velocity.z
                ).magnitude;


            float speedOffset = 0.1f;


            float inputMagnitude =
                _input.analogMovement
                    ? _input.move.magnitude
                    : 1f;


            if (currentHorizontalSpeed <
                    targetSpeed - speedOffset ||
                currentHorizontalSpeed >
                    targetSpeed + speedOffset)
            {
                _speed =
                    Mathf.Lerp(
                        currentHorizontalSpeed,
                        targetSpeed *
                        inputMagnitude,
                        Time.deltaTime *
                        SpeedChangeRate
                    );

                _speed =
                    Mathf.Round(
                        _speed * 1000f
                    ) / 1000f;
            }
            else
            {
                _speed =
                    targetSpeed;
            }


            _animationBlend =
                Mathf.Lerp(
                    _animationBlend,
                    targetSpeed,
                    Time.deltaTime *
                    SpeedChangeRate
                );


            if (_animationBlend < 0.01f)
            {
                _animationBlend = 0f;
            }


            // -----------------------------------------------------
            // INPUT DIRECTION
            // -----------------------------------------------------

            Vector3 inputDirection =
                new Vector3(
                    _input.move.x,
                    0f,
                    _input.move.y
                ).normalized;


            // -----------------------------------------------------
            // PLAYER ROTATION
            //
            // Original camera-relative rotation.
            // -----------------------------------------------------

            if (_input.move != Vector2.zero)
            {
                _targetRotation =
                    Mathf.Atan2(
                        inputDirection.x,
                        inputDirection.z
                    ) *
                    Mathf.Rad2Deg +
                    _mainCamera
                    .transform
                    .eulerAngles
                    .y;


                float rotation =
                    Mathf.SmoothDampAngle(
                        transform.eulerAngles.y,
                        _targetRotation,
                        ref _rotationVelocity,
                        RotationSmoothTime
                    );


                transform.rotation =
                    Quaternion.Euler(
                        0f,
                        rotation,
                        0f
                    );
            }


            // -----------------------------------------------------
            // MOVEMENT DIRECTION
            // -----------------------------------------------------

            Vector3 targetDirection =
                Quaternion.Euler(
                    0f,
                    _targetRotation,
                    0f
                ) *
                Vector3.forward;


            // -----------------------------------------------------
            // MOVE
            // -----------------------------------------------------

            _controller.Move(
                targetDirection.normalized *
                (_speed * Time.deltaTime)
                +
                new Vector3(
                    0f,
                    _verticalVelocity,
                    0f
                ) *
                Time.deltaTime
            );


            // -----------------------------------------------------
            // ANIMATOR
            // -----------------------------------------------------

            if (_hasAnimator)
            {
                _animator.SetFloat(
                    _animIDSpeed,
                    _animationBlend
                );

                _animator.SetFloat(
                    _animIDMotionSpeed,
                    inputMagnitude
                );

                _animator.SetFloat(
                    _animIDMoveX,
                    0f
                );

                _animator.SetFloat(
                    _animIDMoveY,
                    0f
                );

                _animator.SetBool(
                    _animIDLockedOn,
                    false
                );
            }
        }


        // =========================================================
        // LOCK-ON MOVEMENT
        //
        // IMPORTANT:
        // PLAYER DOES NOT ROTATE HERE.
        //
        // A = move left
        // D = move right
        // W = move forward
        // S = move backward
        //
        // Player keeps whatever direction he is currently facing.
        // =========================================================

        private void MoveWhileLockedOn()
        {
            float horizontal =
                _input.move.x;

            float vertical =
                _input.move.y;


            // -----------------------------------------------------
            // MOVEMENT RELATIVE TO PLAYER
            // -----------------------------------------------------

            Vector3 movement =
                transform.right * horizontal +
                transform.forward * vertical;


            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }


            // -----------------------------------------------------
            // SPEED
            // -----------------------------------------------------

            float targetSpeed =
                _input.sprint
                    ? SprintSpeed
                    : MoveSpeed;


            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0f;
            }


            float inputMagnitude =
                _input.analogMovement
                    ? _input.move.magnitude
                    : 1f;


            float currentHorizontalSpeed =
                new Vector3(
                    _controller.velocity.x,
                    0f,
                    _controller.velocity.z
                ).magnitude;


            if (currentHorizontalSpeed <
                    targetSpeed - 0.1f ||
                currentHorizontalSpeed >
                    targetSpeed + 0.1f)
            {
                _speed =
                    Mathf.Lerp(
                        currentHorizontalSpeed,
                        targetSpeed *
                        inputMagnitude,
                        Time.deltaTime *
                        SpeedChangeRate
                    );
            }
            else
            {
                _speed =
                    targetSpeed;
            }


            // -----------------------------------------------------
            // ANIMATION BLEND
            // -----------------------------------------------------

            _animationBlend =
                Mathf.Lerp(
                    _animationBlend,
                    targetSpeed,
                    Time.deltaTime *
                    SpeedChangeRate
                );


            if (_animationBlend < 0.01f)
            {
                _animationBlend = 0f;
            }


            // -----------------------------------------------------
            // LOCK-ON ANIMATOR
            // -----------------------------------------------------

            if (_hasAnimator)
            {
                _animator.SetBool(
                    _animIDLockedOn,
                    true
                );


                // A / D
                // -1 = left
                // +1 = right

                _animator.SetFloat(
                    _animIDMoveX,
                    horizontal
                );


                // W / S
                // +1 = forward
                // -1 = backward

                _animator.SetFloat(
                    _animIDMoveY,
                    vertical
                );


                _animator.SetFloat(
                    _animIDSpeed,
                    _animationBlend
                );


                _animator.SetFloat(
                    _animIDMotionSpeed,
                    inputMagnitude
                );
            }


            // -----------------------------------------------------
            // ACTUAL MOVEMENT
            // -----------------------------------------------------

            Vector3 finalMovement =
                movement.normalized *
                (_speed * Time.deltaTime);


            finalMovement.y =
                _verticalVelocity *
                Time.deltaTime;


            _controller.Move(
                finalMovement
            );
        }


        // =========================================================
        // JUMP + GRAVITY
        // =========================================================

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta =
                    FallTimeout;


                if (_hasAnimator)
                {
                    _animator.SetBool(
                        _animIDJump,
                        false
                    );

                    _animator.SetBool(
                        _animIDFreeFall,
                        false
                    );
                }


                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }


                if (_input.jump &&
                    _jumpTimeoutDelta <= 0f &&
                    !playerController.isBlocking &&
                    !playerController.isAttacking)
                {
                    _verticalVelocity =
                        Mathf.Sqrt(
                            JumpHeight *
                            -2f *
                            Gravity
                        );


                    if (_hasAnimator)
                    {
                        _animator.SetBool(
                            _animIDJump,
                            true
                        );
                    }
                }


                if (_jumpTimeoutDelta >= 0f)
                {
                    _jumpTimeoutDelta -=
                        Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta =
                    JumpTimeout;


                if (_fallTimeoutDelta >= 0f)
                {
                    _fallTimeoutDelta -=
                        Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(
                            _animIDFreeFall,
                            true
                        );
                    }
                }


                _input.jump = false;
            }


            if (_verticalVelocity <
                _terminalVelocity)
            {
                _verticalVelocity +=
                    Gravity *
                    Time.deltaTime;
            }
        }


        // =========================================================
        // DODGE
        // =========================================================

        public void PerformDodge(
            Vector3 direction,
            float distance,
            float duration)
        {
            StartCoroutine(
                DodgeMovement(
                    direction,
                    distance,
                    duration
                )
            );
        }


        private IEnumerator DodgeMovement(
            Vector3 direction,
            float distance,
            float duration)
        {
            float elapsed = 0f;


            Vector3 dodgeDirection =
                direction.normalized;


            while (elapsed < duration)
            {
                float speed =
                    distance / duration;


                _controller.Move(
                    dodgeDirection *
                    speed *
                    Time.deltaTime
                );


                elapsed +=
                    Time.deltaTime;


                yield return null;
            }
        }


        // =========================================================
        // CLAMP ANGLE
        // =========================================================

        private static float ClampAngle(
            float lfAngle,
            float lfMin,
            float lfMax)
        {
            if (lfAngle < -360f)
            {
                lfAngle += 360f;
            }


            if (lfAngle > 360f)
            {
                lfAngle -= 360f;
            }


            return Mathf.Clamp(
                lfAngle,
                lfMin,
                lfMax
            );
        }


        // =========================================================
        // GIZMOS
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen =
                new Color(
                    0f,
                    1f,
                    0f,
                    0.35f
                );


            Color transparentRed =
                new Color(
                    1f,
                    0f,
                    0f,
                    0.35f
                );


            Gizmos.color =
                Grounded
                    ? transparentGreen
                    : transparentRed;


            Gizmos.DrawSphere(
                new Vector3(
                    transform.position.x,
                    transform.position.y -
                    GroundedOffset,
                    transform.position.z
                ),
                GroundedRadius
            );
        }


        // =========================================================
        // FOOTSTEP
        // =========================================================

        private void OnFootstep(
            AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index =
                        Random.Range(
                            0,
                            FootstepAudioClips.Length
                        );


                    AudioSource.PlayClipAtPoint(
                        FootstepAudioClips[index],
                        transform.TransformPoint(
                            _controller.center
                        ),
                        FootstepAudioVolume
                    );
                }
            }
        }


        // =========================================================
        // LAND
        // =========================================================

        private void OnLand(
            AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (LandingAudioClip != null)
                {
                    AudioSource.PlayClipAtPoint(
                        LandingAudioClip,
                        transform.TransformPoint(
                            _controller.center
                        ),
                        FootstepAudioVolume
                    );
                }
            }
        }
    }
}