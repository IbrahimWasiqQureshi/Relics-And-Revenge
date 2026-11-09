
using UnityEngine;
using Gamekit3D.Message;
using System.Collections;
using Unity.Cinemachine;

namespace Gamekit3D
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour, IMessageReceiver
    {
        protected static PlayerController s_Instance;
        public static PlayerController instance { get { return s_Instance; } }

        public bool respawning { get { return m_Respawning; } }

        public float maxForwardSpeed = 8f;
        public float gravity = 20f;
        public float jumpSpeed = 10f;
        public float minTurnSpeed = 400f;
        public float maxTurnSpeed = 1200f;
        public float idleTimeout = 5f;
        public bool canAttack;

        public CameraSettings cameraSettings;
        public MeleeWeapon meleeWeapon;
        public RandomAudioPlayer footstepPlayer;
        public RandomAudioPlayer hurtAudioPlayer;
        public RandomAudioPlayer landingPlayer;
        public RandomAudioPlayer emoteLandingPlayer;
        public RandomAudioPlayer emoteDeathPlayer;
        public RandomAudioPlayer emoteAttackPlayer;
        public RandomAudioPlayer emoteJumpPlayer;

        protected AnimatorStateInfo m_CurrentStateInfo;
        protected AnimatorStateInfo m_NextStateInfo;
        protected bool m_IsAnimatorTransitioning;
        protected AnimatorStateInfo m_PreviousCurrentStateInfo;
        protected AnimatorStateInfo m_PreviousNextStateInfo;
        protected bool m_PreviousIsAnimatorTransitioning;
        protected bool m_IsGrounded = true;
        protected bool m_PreviouslyGrounded = true;
        protected bool m_ReadyToJump;
        protected float m_DesiredForwardSpeed;
        protected float m_ForwardSpeed;
        protected float m_VerticalSpeed;
        protected PlayerInput m_Input;
        protected CharacterController m_CharCtrl;
        protected Animator m_Animator;
        protected Material m_CurrentWalkingSurface;
        protected Quaternion m_TargetRotation;
        protected float m_AngleDiff;
        protected Collider[] m_OverlapResult = new Collider[8];
        protected bool m_InAttack;
        protected bool m_InCombo;
        protected Damageable m_Damageable;
        protected Renderer[] m_Renderers;
        protected Checkpoint m_CurrentCheckpoint;
        protected bool m_Respawning;
        protected float m_IdleTimer;

        const float k_AirborneTurnSpeedProportion = 5.4f;
        const float k_GroundedRayDistance = 1f;
        const float k_JumpAbortSpeed = 10f;
        const float k_MinEnemyDotCoeff = 0.2f;
        const float k_InverseOneEighty = 1f / 180f;
        const float k_StickingGravityProportion = 0.3f;
        const float k_GroundAcceleration = 20f;
        const float k_GroundDeceleration = 25f;

        readonly int m_HashAirborneVerticalSpeed = Animator.StringToHash("AirborneVerticalSpeed");
        readonly int m_HashForwardSpeed = Animator.StringToHash("ForwardSpeed");
        readonly int m_HashAngleDeltaRad = Animator.StringToHash("AngleDeltaRad");
        readonly int m_HashTimeoutToIdle = Animator.StringToHash("TimeoutToIdle");
        readonly int m_HashGrounded = Animator.StringToHash("Grounded");
        readonly int m_HashInputDetected = Animator.StringToHash("InputDetected");
        readonly int m_HashMeleeAttack = Animator.StringToHash("MeleeAttack");
        readonly int m_HashHurt = Animator.StringToHash("Hurt");
        readonly int m_HashDeath = Animator.StringToHash("Death");
        readonly int m_HashRespawn = Animator.StringToHash("Respawn");
        readonly int m_HashHurtFromX = Animator.StringToHash("HurtFromX");
        readonly int m_HashHurtFromY = Animator.StringToHash("HurtFromY");
        readonly int m_HashStateTime = Animator.StringToHash("StateTime");
        readonly int m_HashFootFall = Animator.StringToHash("FootFall");

        readonly int m_HashLocomotion = Animator.StringToHash("Locomotion");
        readonly int m_HashAirborne = Animator.StringToHash("Airborne");
        readonly int m_HashLanding = Animator.StringToHash("Landing");
        readonly int m_HashEllenCombo1 = Animator.StringToHash("EllenCombo1");
        readonly int m_HashEllenCombo2 = Animator.StringToHash("EllenCombo2");
        readonly int m_HashEllenCombo3 = Animator.StringToHash("EllenCombo3");
        readonly int m_HashEllenCombo4 = Animator.StringToHash("EllenCombo4");
        readonly int m_HashEllenDeath = Animator.StringToHash("EllenDeath");

        readonly int m_HashBlockInput = Animator.StringToHash("BlockInput");

        protected bool IsMoveInput
        {
            get { return !Mathf.Approximately(m_Input.MoveInput.sqrMagnitude, 0f); }
        }

        public void SetCanAttack(bool canAttack)
        {
            this.canAttack = canAttack;
        }

        void Reset()
        {
            meleeWeapon = GetComponentInChildren<MeleeWeapon>();

            Transform footStepSource = transform.Find("FootstepSource");
            if (footStepSource != null)
                footstepPlayer = footStepSource.GetComponent<RandomAudioPlayer>();

            Transform hurtSource = transform.Find("HurtSource");
            if (hurtSource != null)
                hurtAudioPlayer = hurtSource.GetComponent<RandomAudioPlayer>();

            Transform landingSource = transform.Find("LandingSource");
            if (landingSource != null)
                landingPlayer = landingSource.GetComponent<RandomAudioPlayer>();

            cameraSettings = FindFirstObjectByType<CameraSettings>();

            if (cameraSettings != null)
            {
                if (cameraSettings.follow == null)
                    cameraSettings.follow = transform;

                if (cameraSettings.lookAt == null)
                    cameraSettings.follow = transform.Find("HeadTarget");
            }
        }

        void Awake()
        {
            m_Input = GetComponent<PlayerInput>();
            m_Animator = GetComponent<Animator>();
            m_CharCtrl = GetComponent<CharacterController>();

            meleeWeapon.SetOwner(gameObject);

            s_Instance = this;
        }

        void OnEnable()
        {
            SceneLinkedSMB<PlayerController>.Initialise(m_Animator, this);

            m_Damageable = GetComponent<Damageable>();
            m_Damageable.onDamageMessageReceivers.Add(this);

            m_Damageable.isInvulnerable = true;

            EquipMeleeWeapon(false);

            m_Renderers = GetComponentsInChildren<Renderer>();
        }

        void OnDisable()
        {
            m_Damageable.onDamageMessageReceivers.Remove(this);

            for (int i = 0; i < m_Renderers.Length; ++i)
            {
                m_Renderers[i].enabled = true;
            }
        }

        void FixedUpdate()
        {
            CacheAnimatorState();

            UpdateInputBlocking();

            EquipMeleeWeapon(IsWeaponEquiped());

            m_Animator.SetFloat(
                m_HashStateTime,
                Mathf.Repeat(m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f)
            );

            m_Animator.ResetTrigger(m_HashMeleeAttack);

            if (m_Input.Attack && canAttack)
                m_Animator.SetTrigger(m_HashMeleeAttack);

            CalculateForwardMovement();
            CalculateVerticalMovement();

            SetTargetRotation();

            if (IsOrientationUpdated() && IsMoveInput)
                UpdateOrientation();

            PlayAudio();

            TimeoutToIdle();

            m_PreviouslyGrounded = m_IsGrounded;
        }

        void CacheAnimatorState()
        {
            m_PreviousCurrentStateInfo = m_CurrentStateInfo;
            m_PreviousNextStateInfo = m_NextStateInfo;
            m_PreviousIsAnimatorTransitioning = m_IsAnimatorTransitioning;

            m_CurrentStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
            m_NextStateInfo = m_Animator.GetNextAnimatorStateInfo(0);
            m_IsAnimatorTransitioning = m_Animator.IsInTransition(0);
        }

        void UpdateInputBlocking()
        {
            bool inputBlocked =
                m_CurrentStateInfo.tagHash == m_HashBlockInput &&
                !m_IsAnimatorTransitioning;

            inputBlocked |= m_NextStateInfo.tagHash == m_HashBlockInput;

            m_Input.playerControllerInputBlocked = inputBlocked;
        }

        bool IsWeaponEquiped()
        {
            bool equipped =
                m_NextStateInfo.shortNameHash == m_HashEllenCombo1 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo1;

            equipped |=
                m_NextStateInfo.shortNameHash == m_HashEllenCombo2 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo2;

            equipped |=
                m_NextStateInfo.shortNameHash == m_HashEllenCombo3 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo3;

            equipped |=
                m_NextStateInfo.shortNameHash == m_HashEllenCombo4 ||
                m_CurrentStateInfo.shortNameHash == m_HashEllenCombo4;

            return equipped;
        }

        void EquipMeleeWeapon(bool equip)
        {
            meleeWeapon.gameObject.SetActive(equip);
            m_InAttack = false;
            m_InCombo = equip;

            if (!equip)
                m_Animator.ResetTrigger(m_HashMeleeAttack);
        }

        void CalculateForwardMovement()
        {
            Vector2 moveInput = m_Input.MoveInput;

            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            m_DesiredForwardSpeed = moveInput.magnitude * maxForwardSpeed;

            float acceleration =
                IsMoveInput ? k_GroundAcceleration : k_GroundDeceleration;

            m_ForwardSpeed = Mathf.MoveTowards(
                m_ForwardSpeed,
                m_DesiredForwardSpeed,
                acceleration * Time.deltaTime
            );

            m_Animator.SetFloat(m_HashForwardSpeed, m_ForwardSpeed);
        }

        void CalculateVerticalMovement()
        {
            if (!m_Input.JumpInput && m_IsGrounded)
                m_ReadyToJump = true;

            if (m_IsGrounded)
            {
                m_VerticalSpeed =
                    -gravity * k_StickingGravityProportion;

                if (m_Input.JumpInput &&
                    m_ReadyToJump &&
                    !m_InCombo)
                {
                    m_VerticalSpeed = jumpSpeed;
                    m_IsGrounded = false;
                    m_ReadyToJump = false;
                }
            }
            else
            {
                if (!m_Input.JumpInput && m_VerticalSpeed > 0.0f)
                {
                    m_VerticalSpeed -=
                        k_JumpAbortSpeed * Time.deltaTime;
                }

                if (Mathf.Approximately(m_VerticalSpeed, 0f))
                    m_VerticalSpeed = 0f;

                m_VerticalSpeed -= gravity * Time.deltaTime;
            }
        }

        void SetTargetRotation()
        {
            Vector2 moveInput = m_Input.MoveInput;

            Vector3 localMovementDirection =
                new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            InputAxis axis =
                cameraSettings.CurrentOrbitalFollow.HorizontalAxis;

            Vector3 forward =
                Quaternion.Euler(0f, axis.Value, 0f) *
                Vector3.forward;

            forward.y = 0f;
            forward.Normalize();

            Quaternion targetRotation;

            if (Mathf.Approximately(
                Vector3.Dot(localMovementDirection, Vector3.forward),
                -1.0f))
            {
                targetRotation =
                    Quaternion.LookRotation(-forward);
            }
            else
            {
                Quaternion cameraToInputOffset =
                    Quaternion.FromToRotation(
                        Vector3.forward,
                        localMovementDirection);

                targetRotation =
                    Quaternion.LookRotation(
                        cameraToInputOffset * forward);
            }

            Vector3 resultingForward =
                targetRotation * Vector3.forward;

            if (m_InAttack)
            {
                Vector3 centre =
                    transform.position +
                    transform.forward * 2.0f +
                    transform.up;

                Vector3 halfExtents =
                    new Vector3(3.0f, 1.0f, 2.0f);

                int layerMask =
                    1 << LayerMask.NameToLayer("Enemy");

                int count =
                    Physics.OverlapBoxNonAlloc(
                        centre,
                        halfExtents,
                        m_OverlapResult,
                        targetRotation,
                        layerMask);

                float closestDot = 0.0f;
                Vector3 closestForward = Vector3.zero;
                int closest = -1;

                for (int i = 0; i < count; ++i)
                {
                    Vector3 playerToEnemy =
                        m_OverlapResult[i].transform.position -
                        transform.position;

                    playerToEnemy.y = 0;
                    playerToEnemy.Normalize();

                    float d =
                        Vector3.Dot(
                            resultingForward,
                            playerToEnemy);

                    if (d > k_MinEnemyDotCoeff &&
                        d > closestDot)
                    {
                        closestForward = playerToEnemy;
                        closestDot = d;
                        closest = i;
                    }
                }

                if (closest != -1)
                {
                    resultingForward = closestForward;

                    transform.rotation =
                        Quaternion.LookRotation(resultingForward);
                }
            }

            float angleCurrent =
                Mathf.Atan2(
                    transform.forward.x,
                    transform.forward.z) *
                Mathf.Rad2Deg;

            float targetAngle =
                Mathf.Atan2(
                    resultingForward.x,
                    resultingForward.z) *
                Mathf.Rad2Deg;

            m_AngleDiff =
                Mathf.DeltaAngle(
                    angleCurrent,
                    targetAngle);

            m_TargetRotation = targetRotation;
        }

        bool IsOrientationUpdated()
        {
            bool updateOrientationForLocomotion =
                !m_IsAnimatorTransitioning &&
                m_CurrentStateInfo.shortNameHash ==
                m_HashLocomotion ||
                m_NextStateInfo.shortNameHash ==
                m_HashLocomotion;

            bool updateOrientationForAirborne =
                !m_IsAnimatorTransitioning &&
                m_CurrentStateInfo.shortNameHash ==
                m_HashAirborne ||
                m_NextStateInfo.shortNameHash ==
                m_HashAirborne;

            bool updateOrientationForLanding =
                !m_IsAnimatorTransitioning &&
                m_CurrentStateInfo.shortNameHash ==
                m_HashLanding ||
                m_NextStateInfo.shortNameHash ==
                m_HashLanding;

            return updateOrientationForLocomotion ||
                   updateOrientationForAirborne ||
                   updateOrientationForLanding ||
                   m_InCombo && !m_InAttack;
        }

        void UpdateOrientation()
        {
            m_Animator.SetFloat(
                m_HashAngleDeltaRad,
                m_AngleDiff * Mathf.Deg2Rad);

            Vector3 localInput =
                new Vector3(
                    m_Input.MoveInput.x,
                    0f,
                    m_Input.MoveInput.y);

            float groundedTurnSpeed =
                Mathf.Lerp(
                    maxTurnSpeed,
                    minTurnSpeed,
                    m_ForwardSpeed / m_DesiredForwardSpeed);

            float actualTurnSpeed =
                m_IsGrounded
                    ? groundedTurnSpeed
                    : Vector3.Angle(
                        transform.forward,
                        localInput) *
                      k_InverseOneEighty *
                      k_AirborneTurnSpeedProportion *
                      groundedTurnSpeed;

            m_TargetRotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    m_TargetRotation,
                    actualTurnSpeed * Time.deltaTime);

            transform.rotation = m_TargetRotation;
        }

        void PlayAudio()
        {
            float footfallCurve =
                m_Animator.GetFloat(m_HashFootFall);

            if (footfallCurve > 0.01f &&
                !footstepPlayer.playing &&
                footstepPlayer.canPlay)
            {
                footstepPlayer.playing = true;
                footstepPlayer.canPlay = false;

                footstepPlayer.PlayRandomClip(
                    m_CurrentWalkingSurface,
                    m_ForwardSpeed < 4 ? 0 : 1);
            }
            else if (footstepPlayer.playing)
            {
                footstepPlayer.playing = false;
            }
            else if (footfallCurve < 0.01f &&
                     !footstepPlayer.canPlay)
            {
                footstepPlayer.canPlay = true;
            }

            if (m_IsGrounded && !m_PreviouslyGrounded)
            {
                landingPlayer.PlayRandomClip(
                    m_CurrentWalkingSurface,
                    bankId: m_ForwardSpeed < 4 ? 0 : 1);

                emoteLandingPlayer.PlayRandomClip();
            }

            if (!m_IsGrounded &&
                m_PreviouslyGrounded &&
                m_VerticalSpeed > 0f)
            {
                emoteJumpPlayer.PlayRandomClip();
            }

            if (m_CurrentStateInfo.shortNameHash ==
                m_HashHurt &&
                m_PreviousCurrentStateInfo.shortNameHash !=
                m_HashHurt)
            {
                hurtAudioPlayer.PlayRandomClip();
            }

            if (m_CurrentStateInfo.shortNameHash ==
                m_HashEllenDeath &&
                m_PreviousCurrentStateInfo.shortNameHash !=
                m_HashEllenDeath)
            {
                emoteDeathPlayer.PlayRandomClip();
            }

            if (
                m_CurrentStateInfo.shortNameHash ==
                m_HashEllenCombo1 &&
                m_PreviousCurrentStateInfo.shortNameHash !=
                m_HashEllenCombo1 ||

                m_CurrentStateInfo.shortNameHash ==
                m_HashEllenCombo2 &&
                m_PreviousCurrentStateInfo.shortNameHash !=
                m_HashEllenCombo2 ||

                m_CurrentStateInfo.shortNameHash ==
                m_HashEllenCombo3 &&
                m_PreviousCurrentStateInfo.shortNameHash !=
                m_HashEllenCombo3 ||

                m_CurrentStateInfo.shortNameHash ==
                m_HashEllenCombo4 &&
                m_PreviousCurrentStateInfo.shortNameHash !=
                m_HashEllenCombo4)
            {
                emoteAttackPlayer.PlayRandomClip();
            }
        }

        void TimeoutToIdle()
        {
            bool inputDetected =
                IsMoveInput ||
                m_Input.Attack ||
                m_Input.JumpInput;

            if (m_IsGrounded && !inputDetected)
            {
                m_IdleTimer += Time.deltaTime;

                if (m_IdleTimer >= idleTimeout)
                {
                    m_IdleTimer = 0f;
                    m_Animator.SetTrigger(
                        m_HashTimeoutToIdle);
                }
            }
            else
            {
                m_IdleTimer = 0f;
                m_Animator.ResetTrigger(
                    m_HashTimeoutToIdle);
            }

            m_Animator.SetBool(
                m_HashInputDetected,
                inputDetected);
        }

        void OnAnimatorMove()
        {
            Vector3 movement;

            if (m_IsGrounded)
            {
                RaycastHit hit;

                Ray ray =
                    new Ray(
                        transform.position +
                        Vector3.up *
                        k_GroundedRayDistance *
                        0.5f,
                        -Vector3.up);

                if (Physics.Raycast(
                    ray,
                    out hit,
                    k_GroundedRayDistance,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    movement =
                        Vector3.ProjectOnPlane(
                            m_Animator.deltaPosition,
                            hit.normal);

                    Renderer groundRenderer =
                        hit.collider.GetComponentInChildren<Renderer>();

                    m_CurrentWalkingSurface =
                        groundRenderer
                            ? groundRenderer.sharedMaterial
                            : null;
                }
                else
                {
                    movement =
                        m_Animator.deltaPosition;

                    m_CurrentWalkingSurface = null;
                }
            }
            else
            {
                movement =
                    m_ForwardSpeed *
                    transform.forward *
                    Time.deltaTime;
            }

            m_CharCtrl.transform.rotation *=
                m_Animator.deltaRotation;

            movement +=
                m_VerticalSpeed *
                Vector3.up *
                Time.deltaTime;

            m_CharCtrl.Move(movement);

            m_IsGrounded =
                m_CharCtrl.isGrounded;

            if (!m_IsGrounded)
            {
                m_Animator.SetFloat(
                    m_HashAirborneVerticalSpeed,
                    m_VerticalSpeed);
            }

            m_Animator.SetBool(
                m_HashGrounded,
                m_IsGrounded);
        }

        public void MeleeAttackStart(int throwing = 0)
        {
            meleeWeapon.BeginAttack(
                throwing != 0);

            m_InAttack = true;
        }

        public void MeleeAttackEnd()
        {
            meleeWeapon.EndAttack();
            m_InAttack = false;
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint != null)
                m_CurrentCheckpoint = checkpoint;
        }

        public void Respawn()
        {
            StartCoroutine(
                RespawnRoutine());
        }

        protected IEnumerator RespawnRoutine()
        {
            while (
                m_CurrentStateInfo.shortNameHash !=
                m_HashEllenDeath ||
                !m_IsAnimatorTransitioning)
            {
                yield return null;
            }

            yield return StartCoroutine(
                ScreenFader.FadeSceneOut());

            while (ScreenFader.IsFading)
            {
                yield return null;
            }

            EllenSpawn spawn =
                GetComponentInChildren<EllenSpawn>();

            spawn.enabled = true;

            if (m_CurrentCheckpoint != null)
            {
                transform.position =
                    m_CurrentCheckpoint.transform.position;

                transform.rotation =
                    m_CurrentCheckpoint.transform.rotation;
            }
            else
            {
                Debug.LogError(
                    "There is no Checkpoint set, there should always be a checkpoint set. Did you add a checkpoint at the spawn?");
            }

            m_Animator.SetTrigger(
                m_HashRespawn);

            spawn.StartEffect();

            yield return StartCoroutine(
                ScreenFader.FadeSceneIn());

            m_Damageable.ResetDamage();
        }

        public void RespawnFinished()
        {
            m_Respawning = false;

            m_Damageable.isInvulnerable = false;
        }

        public void OnReceiveMessage(
            MessageType type,
            object sender,
            object data)
        {
            switch (type)
            {
                case MessageType.DAMAGED:
                    {
                        Damageable.DamageMessage damageData =
                            (Damageable.DamageMessage)data;

                        Damaged(damageData);
                    }
                    break;

                case MessageType.DEAD:
                    {
                        Damageable.DamageMessage damageData =
                            (Damageable.DamageMessage)data;

                        Die(damageData);
                    }
                    break;
            }
        }

        void Damaged(
            Damageable.DamageMessage damageMessage)
        {
            m_Animator.SetTrigger(
                m_HashHurt);

            Vector3 forward =
                damageMessage.damageSource -
                transform.position;

            forward.y = 0f;

            Vector3 localHurt =
                transform.InverseTransformDirection(
                    forward);

            m_Animator.SetFloat(
                m_HashHurtFromX,
                localHurt.x);

            m_Animator.SetFloat(
                m_HashHurtFromY,
                localHurt.z);

            CameraShake.Shake(
                CameraShake.k_PlayerHitShakeAmount,
                CameraShake.k_PlayerHitShakeTime);

            if (hurtAudioPlayer != null)
            {
                hurtAudioPlayer.PlayRandomClip();
            }
        }

        public void Die(
            Damageable.DamageMessage damageMessage)
        {
            m_Animator.SetTrigger(
                m_HashDeath);

            m_ForwardSpeed = 0f;
            m_VerticalSpeed = 0f;
            m_Respawning = true;

            m_Damageable.isInvulnerable = true;
        }
    }
}

