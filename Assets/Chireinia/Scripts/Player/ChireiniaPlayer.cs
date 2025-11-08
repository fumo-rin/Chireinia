using FumoCore.Input;
using FumoCore.Tools;
using Fumorin;
using Fumorin.Units;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Chireinia
{
    public class ChireiniaPlayer : FumoUnit
    {
        [SerializeField] ParticleSystem _testLoot;
        [SerializeField] string animatorJumpKey = "JUMP";
        [SerializeField] string animatorGroundedKey = "ISGROUNDED";
        [SerializeField] string swingKey = "SWING";
        [SerializeField] string swingPerSecondKey = "SWINGSPERSECOND";
        [SerializeField] BoxCollider2D groundCheckOwnerCollider;
        [SerializeField] LayerMask groundLayer;
        public bool IsGrounded { get; private set; }
        public bool IsGroundedWithoutCoyote { get; private set; }
        float coyoteEnd;
        public static bool PlayerAs(out ChireiniaPlayer player)
        {
            player = null;
            if (FumoUnit.Player is ChireiniaPlayer p)
            {
                player = p;
            }
            return player != null;
        }
        protected override bool CalculateAlive()
        {
            return gameObject != null && gameObject.activeInHierarchy;
        }
        protected override void WhenAwake()
        {

        }
        protected override void WhenDestroy()
        {

        }
        protected override void WhenStart()
        {

        }
        private void LateUpdate()
        {
            if (jumpEndTime > Time.time && RB.linearVelocity.y.Absolute() > 0.25f)
            {
                IsGroundedWithoutCoyote = false;
                IsGrounded = false;
                return;
            }
            bool grounded = groundCheckOwnerCollider.TryBoxcastBottom(0.1f, groundLayer, out _);
            IsGroundedWithoutCoyote = grounded;
            if (grounded)
            {
                coyoteEnd = Time.time + 0.125f;
            }
            grounded = grounded || Time.time <= coyoteEnd;
            IsGrounded = grounded;
        }
        Coroutine jumpRoutine;
        Vector2 lastPos;
        float jumpEndTime;
        [SerializeField] InputActionReference jumpInput, swingInput;
        float nextAttackTime;
        protected override void WhenUpdate()
        {
            void Move()
            {
                float moveSpeed = 11f;
                float acceleration = IsGroundedWithoutCoyote ? 80f : 22f;
                float friction = IsGroundedWithoutCoyote ? 120f : 12f;
                Vector2 input = GenericInput.Move * 2f;
                float mag = input.magnitude;
                float xSpeed = input.x.Sign() * mag.Clamp(0f, 1f) * moveSpeed.Max(RB.linearVelocity.x);
                if (input.x.Absolute() < 0.01f)
                {
                    RB.VelocityTowardsX(Vector2.zero, friction);
                }
                else
                {
                    RB.VelocityTowardsX(new(xSpeed, 0f), acceleration);
                }
                Vector2 diff = CurrentPosition - lastPos;
                lastPos = CurrentPosition;
                FlipWithMovement(diff);
                if (IsGrounded && moveAnimator != null)
                {
                    moveAnimator.SetFloat(moveAnimatorStringKey, input.magnitude / Time.deltaTime.Max(0.001f));
                }
            }
            void Jump()
            {
                IEnumerator CO_Jump(float cancelMultiplier = 0.6f)
                {
                    while (RB.linearVelocity.y > 0f)
                    {
                        if (!jumpInput.IsPressed())
                        {
                            RB.linearVelocityY *= cancelMultiplier;
                            jumpRoutine = null;
                            yield break;
                        }
                        yield return null;
                    }
                    jumpRoutine = null;
                }
                moveAnimator.SetBool(animatorGroundedKey, IsGrounded && Time.time >= jumpEndTime);
                if (IsGrounded && (jumpRoutine == null || Time.time > jumpEndTime) && jumpInput.IsPressed() && !jumpInput.PressedLongerThan(0.125f))
                {
                    float jumpForce = 22f;
                    ChireiniaState.AddScore(333d, "Jump lol");

                    RB.linearVelocity = new(RB.linearVelocity.x * 1.25f, jumpForce);
                    if (jumpRoutine != null)
                    {
                        StopCoroutine(jumpRoutine);
                    }
                    jumpRoutine = StartCoroutine(CO_Jump(0.6f));
                    jumpEndTime = Time.time + 0.4f;
                    moveAnimator.SetTrigger(animatorJumpKey);
                }
            }
            void Swing()
            {
                if (swingInput.IsPressed() && Time.time >= nextAttackTime)
                {
                    float swingsPerSecond = 1.75f;
                    nextAttackTime = Time.time + (1f / swingsPerSecond);
                    moveAnimator.SetTrigger(swingKey);
                    moveAnimator.SetFloat(swingPerSecondKey, swingsPerSecond);
                }
            }
            Move();
            Jump();
            Swing();
        }
    }
}
