using Microsoft.Xna.Framework;
using System;

namespace Snow.Engine
{
    public class PhysicsComponent
    {
        public Vector2 Velocity { get; set; }
        public bool IsGrounded { get; set; }

        public float Gravity { get; set; } = 1400f;
        public float MaxFallSpeed { get; set; } = 320f;
        public float MoveSpeed { get; set; } = 120f;
        public float Acceleration { get; set; } = 800f;
        public float AirAcceleration { get; set; } = 700f;
        public float Friction { get; set; } = 900f;
        public float AirFriction { get; set; } = 400f;

        public float JumpSpeed { get; set; } = 240f;
        public float JumpCutMultiplier { get; set; } = 0.5f;
        public float CoyoteTime { get; set; } = 0.1f;
        public float JumpBufferTime { get; set; } = 0.1f;

        public float DashSpeed { get; set; } = 280f;
        public float DashTime { get; set; } = 0.18f;
        public float DashEndSpeedMult { get; set; } = 0.65f;
        public float DashCooldown { get; set; } = 0.25f;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private Vector2 _dashDirection;
        private bool _isDashing;
        private bool _dashesLeft;

        public bool IsDashing => _isDashing;
        public bool CanDash => _dashesLeft && _dashCooldownTimer <= 0;
        public Vector2 DashDirection => _dashDirection;

        public PhysicsComponent()
        {
            _dashesLeft = true;
        }

        public void Update(float deltaTime, float moveInput, float verticalInput, bool jumpPressed, bool jumpReleased, bool dashPressed)
        {
            UpdateTimers(deltaTime);

            if (_isDashing)
            {
                UpdateDash(deltaTime);
                return;
            }

            ApplyGravity(deltaTime);
            ApplyMovement(deltaTime, moveInput);
            HandleJump(jumpPressed, jumpReleased);
            HandleDash(dashPressed, moveInput, verticalInput);

            if (IsGrounded)
            {
                _dashesLeft = true;
            }
        }

        private void UpdateTimers(float deltaTime)
        {
            if (IsGrounded)
                _coyoteTimer = CoyoteTime;
            else
                _coyoteTimer -= deltaTime;

            if (_jumpBufferTimer > 0)
                _jumpBufferTimer -= deltaTime;

            if (_dashCooldownTimer > 0)
                _dashCooldownTimer -= deltaTime;
        }

        private void ApplyGravity(float deltaTime)
        {
            Vector2 vel = Velocity;
            vel.Y += Gravity * deltaTime;
            if (vel.Y > MaxFallSpeed)
                vel.Y = MaxFallSpeed;
            Velocity = vel;
        }

        private void ApplyMovement(float deltaTime, float moveInput)
        {
            float accel = IsGrounded ? Acceleration : AirAcceleration;
            float fric = IsGrounded ? Friction : AirFriction;
            Vector2 vel = Velocity;

            if (Math.Abs(moveInput) > 0.01f)
            {
                vel.X += moveInput * accel * deltaTime;
                if (Math.Abs(vel.X) > MoveSpeed)
                    vel.X = Math.Sign(vel.X) * MoveSpeed;
            }
            else
            {
                if (Math.Abs(vel.X) > 0.01f)
                {
                    float drag = fric * deltaTime;
                    if (Math.Abs(vel.X) < drag)
                        vel.X = 0;
                    else
                        vel.X -= Math.Sign(vel.X) * drag;
                }
            }

            Velocity = vel;
        }

        private void HandleJump(bool jumpPressed, bool jumpReleased)
        {
            if (jumpPressed)
                _jumpBufferTimer = JumpBufferTime;

            if (_jumpBufferTimer > 0 && _coyoteTimer > 0)
            {
                Vector2 vel = Velocity;
                vel.Y = -JumpSpeed;
                Velocity = vel;
                _jumpBufferTimer = 0;
                _coyoteTimer = 0;
            }

            if (jumpReleased && Velocity.Y < 0)
            {
                Vector2 vel = Velocity;
                vel.Y *= JumpCutMultiplier;
                Velocity = vel;
            }
        }

        private void HandleDash(bool dashPressed, float moveInput, float verticalInput)
        {
            if (dashPressed && CanDash)
            {
                _isDashing = true;
                _dashTimer = DashTime;
                _dashCooldownTimer = DashCooldown;
                _dashesLeft = false;

                Vector2 dir = new Vector2(moveInput, verticalInput);
                
                if (dir.LengthSquared() < 0.01f)
                {
                    dir = Vector2.UnitX;
                }
                else
                {
                    dir.Normalize();
                }

                _dashDirection = dir;
                Velocity = _dashDirection * DashSpeed;
            }
        }

        private void UpdateDash(float deltaTime)
        {
            _dashTimer -= deltaTime;
            if (_dashTimer <= 0)
            {
                _isDashing = false;
                Velocity *= DashEndSpeedMult;
            }
        }

        public void ResetCoyoteTime()
        {
            _coyoteTimer = 0;
        }
    }
}




