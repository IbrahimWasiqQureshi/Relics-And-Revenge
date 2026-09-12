using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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

        [Tooltip("How quickly the player's animator blend (MoveX/MoveY) reacts to strafe input changes. Higher = snappier, lower = smoother.")]
        public float LockOnAnimationDampTime = 0.15f;

        [Tooltip("How quickly the camera swings to frame the locked-on target.")]
        public float LockOnCameraSpeed = 6f;

        [Tooltip("Extra downward pitch (degrees) applied while locked on, so the camera doesn't stare straight at the target's head.")]
        public float LockOnPitchOffset = 5f;

        [Tooltip("Extra horizontal yaw (degrees) added while locked on. Positive = right, negative = left.")]
        public float LockOnYawOffset = 0f;

        [Header("Lock-On Camera Position")]

        public float LockOnCameraDistance = 3.5f;

        public float LockOnVerticalArmLength = 0.3f;

        public float LockOnShoulderOffsetX = 0.5f;


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

        public GameObject PlayerFollowCamera;

        public float TopClamp = 70.0f;

        public float BottomClamp = -30.0f;

        public float CameraAngleOverride = 0.0f;

        public bool LockCameraPosition = false;


        // =========================================================
        // CAMERA VARIABLES
        // =========================================================

        private float _cinemachineTargetYaw;

        private float _cinemachineTargetPitch;

        private Cinemachine3rdPersonFollow _thirdPersonFollow;

        private float _normalCameraDistance;

        private float _normalVerticalArmLength;

        private Vector3 _normalShoulderOffset;


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

        

        private Coroutine _dodgeCoroutine;
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

            if (PlayerFollowCamera != null)
            {
                _thirdPersonFollow =
                    PlayerFollowCamera.GetComponentInChildren<Cinemachine3rdPersonFollow>();
            }

            if (_thirdPersonFollow != null)
            {
                _normalCameraDistance = _thirdPersonFollow.CameraDistance;
                _normalVerticalArmLength = _thirdPersonFollow.VerticalArmLength;
                _normalShoulderOffset = _thirdPersonFollow.ShoulderOffset;
            }

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

            // NOTE: GroundedCheck() must run before JumpAndGravity(),
            // since JumpAndGravity() reads the Grounded flag. It was
            // previously called after, so JumpAndGravity() was always
            // acting on last frame's grounded state - a stale read
            // that could let gravity silently accumulate for a frame
            // here and there (most noticeable during the fast lateral
            // movement of a dodge roll, causing the falling/stuck
            // feeling).
            GroundedCheck();

            JumpAndGravity();

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
            if (IsLockedOn())
            {
                UpdateCameraPosition(true);

                LockOnCameraRotation();

                return;
            }

            UpdateCameraPosition(false);

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
        // LOCK-ON CAMERA
        //
        // Souls-style: while locked on, the camera automatically
        // swings to keep the target framed instead of relying on
        // free-look input. Yaw points from the pivot toward the
        // target; pitch follows the target's height (plus a small
        // downward bias) so both player and enemy stay in view.
        // =========================================================

        private void UpdateCameraPosition(bool lockedOn)
        {
            if (_thirdPersonFollow == null)
                return;

            float t =
                1f -
                Mathf.Exp(
                    -LockOnCameraSpeed *
                    Time.deltaTime
                );

            float targetDistance =
                lockedOn ? LockOnCameraDistance : _normalCameraDistance;

            float targetArmLength =
                lockedOn ? LockOnVerticalArmLength : _normalVerticalArmLength;

            Vector3 targetShoulderOffset =
                lockedOn
                    ? new Vector3(LockOnShoulderOffsetX, _normalShoulderOffset.y, _normalShoulderOffset.z)
                    : _normalShoulderOffset;

            _thirdPersonFollow.CameraDistance =
                Mathf.Lerp(_thirdPersonFollow.CameraDistance, targetDistance, t);

            _thirdPersonFollow.VerticalArmLength =
                Mathf.Lerp(_thirdPersonFollow.VerticalArmLength, targetArmLength, t);

            _thirdPersonFollow.ShoulderOffset =
                Vector3.Lerp(_thirdPersonFollow.ShoulderOffset, targetShoulderOffset, t);
        }


private void LockOnCameraRotation()
        {
            // Camera yaw still swings to face the target so the enemy
            // stays framed horizontally, but pitch is held at a fixed
            // offset (LockOnPitchOffset, set in the Inspector) instead
            // of being recalculated from the target's height/distance.
            // That height-based calculation was the cause of the
            // camera tilting upward as the player closed the distance
            // to the target (smaller horizontal distance -> steeper
            // angle needed to "aim" at the target's exact position).

            float desiredYaw =
                transform.eulerAngles.y + LockOnYawOffset;

            float desiredPitch =
                -LockOnPitchOffset;

            if (lockOnManager != null &&
                lockOnManager.currentTarget != null &&
                CinemachineCameraTarget != null)
            {
                Transform targetPoint =
                    lockOnManager.currentTarget.targetPoint != null
                        ? lockOnManager.currentTarget.targetPoint
                        : lockOnManager.currentTarget.transform;

                Vector3 pivotPosition =
                    CinemachineCameraTarget.transform.position;

                Vector3 toTarget =
                    targetPoint.position - pivotPosition;

                float horizontalDistance =
                    new Vector3(toTarget.x, 0f, toTarget.z).magnitude;

                if (horizontalDistance > 0.001f)
                {
                    desiredYaw =
                        Mathf.Atan2(toTarget.x, toTarget.z) *
                        Mathf.Rad2Deg +
                        LockOnYawOffset;
                }
            }

            float lerpFactor =
                1f -
                Mathf.Exp(
                    -LockOnCameraSpeed *
                    Time.deltaTime
                );

            _cinemachineTargetYaw =
                Mathf.LerpAngle(
                    _cinemachineTargetYaw,
                    desiredYaw,
                    lerpFactor
                );

            _cinemachineTargetPitch =
                Mathf.Clamp(
                    Mathf.LerpAngle(
                        _cinemachineTargetPitch,
                        desiredPitch,
                        lerpFactor
                    ),
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
            RotateTowardsLockOnTarget();

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
            //
            // MoveX/MoveY feed a 2D freeform blend tree, so raw
            // input snaps the blend instantly between clips.
            // Damping them here gives smooth directional blending
            // (souls-style) instead of jump-cutting between
            // strafe animations.
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
                    horizontal,
                    LockOnAnimationDampTime,
                    Time.deltaTime
                );


                // W / S
                // +1 = forward
                // -1 = backward

                _animator.SetFloat(
                    _animIDMoveY,
                    vertical,
                    LockOnAnimationDampTime,
                    Time.deltaTime
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
        // ROTATE TOWARDS LOCK-ON TARGET
        //
        // Souls-style: the player always faces the locked target
        // (yaw only) so strafing left/right/back reads correctly
        // against the directional blend tree. Uses framerate-
        // independent exponential smoothing for a natural turn
        // instead of an instant snap.
        // =========================================================

        private void RotateTowardsLockOnTarget()
        {
            if (!IsLockedOn())
                return;

            LockOnTarget currentTarget =
                lockOnManager.currentTarget;

            Transform targetPoint =
                currentTarget.targetPoint != null
                    ? currentTarget.targetPoint
                    : currentTarget.transform;

            Vector3 direction =
                targetPoint.position -
                transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized
                );

            float lerpFactor =
                1f -
                Mathf.Exp(
                    -LockOnRotationSpeed *
                    Time.deltaTime
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    lerpFactor
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
            if (_dodgeCoroutine != null)
            {
                StopCoroutine(_dodgeCoroutine);
            }

            _dodgeCoroutine =
                StartCoroutine(
                    DodgeMovement(
                        direction,
                        distance,
                        duration
                    )
                );
        }


        // =========================================================
        // STOP DODGE
        //
        // Called when the roll is interrupted early (e.g. the
        // player gets hit mid-roll and PlayerController cancels
        // action states). Prevents the coroutine from continuing
        // to slide the character after control has been taken
        // away from it.
        // =========================================================

        public void StopDodge()
        {
            if (_dodgeCoroutine != null)
            {
                StopCoroutine(_dodgeCoroutine);
                _dodgeCoroutine = null;
            }
        }


private IEnumerator DodgeMovement(
            Vector3 direction,
            float distance,
            float duration)
        {
            float elapsed = 0f;


            Vector3 dodgeDirection =
                direction.normalized;


            // Keep rolling only while isDodging is still true and
            // we're inside the intended window - if PlayerController
            // cancels the roll early (e.g. got hit), this coroutine
            // is also stopped directly via StopDodge(), but the
            // isDodging check is a second safety net.
            while (elapsed < duration &&
                   playerController != null &&
                   playerController.isDodging)
            {
                float speed =
                    distance / duration;


                Vector3 horizontalMove =
                    dodgeDirection *
                    speed *
                    Time.deltaTime;


                // Dodging only ever starts while Grounded (gated in
                // PlayerController.Dodge()), and it's a ground roll,
                // not a jump - so while grounded we stick to the
                // floor with the same small constant value
                // JumpAndGravity() uses (-2f), instead of trusting
                // the accumulated _verticalVelocity outright.
                // If we do end up airborne mid-roll (e.g. rolling
                // off a ledge), fall back to the real vertical
                // velocity so the player still falls naturally.
                float verticalSpeed =
                    Grounded
                        ? -2f
                        : _verticalVelocity;

                Vector3 verticalMove =
                    Vector3.up *
                    verticalSpeed *
                    Time.deltaTime;


                _controller.Move(
                    horizontalMove +
                    verticalMove
                );


                elapsed +=
                    Time.deltaTime;


                yield return null;
            }


            // =====================================================
            // HAND CONTROL BACK IMMEDIATELY
            //
            // The "Standing Dive Forward" clip has its own
            // ResetDodge() animation event baked in near the very
            // end (~1s in), well after the roll's actual translation
            // (this duration, ~0.7s) and even after the Animator's
            // own exit transition out of the dive state (~0.75s).
            // Waiting for that event left isDodging (and therefore
            // all movement/input) locked for an extra ~0.3-0.5s
            // after the character had already stopped moving -
            // which is what read as the player getting stuck/
            // sinking after the roll. Resetting it here as soon as
            // this coroutine's own roll duration completes keeps
            // control handed back in sync with the movement instead.
            // The animation event still fires later too, but that's
            // harmless - isDodging is already false by then.
            if (playerController != null)
            {
                playerController.ResetDodge();
            }


            _dodgeCoroutine = null;
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