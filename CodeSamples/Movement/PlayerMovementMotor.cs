using RIP.Player.Energy;
using UnityEngine;

namespace RIP.Player.Move
{
    internal sealed class PlayerMovementMotor
    {
        private readonly CharacterController _characterController;
        private readonly PlayerEnergy _energy;
        private readonly PlayerMovementStats _stats;
        private readonly Transform _transform;

        public bool AlwaysFaceMovementDirection { get; set; }

        public PlayerMovementMotor(
            CharacterController characterController,
            PlayerEnergy energy,
            PlayerMovementStats stats,
            Transform transform)
        {
            _characterController = characterController;
            _energy = energy;
            _stats = stats;
            _transform = transform;
        }

        public CollisionFlags MoveNormally(
            ref PlayerControllerData data,
            Vector3 direction,
            bool hardLockActive,
            Vector3 hardLockDirection,
            bool jumpPressed,
            bool ascendHeld,
            Vector3 velocity,
            float deltaTime)
        {
            ApplyVerticalMovement(
                ref data,
                jumpPressed,
                ascendHeld,
                ref velocity,
                deltaTime);
            ApplyHorizontalMovement(
                ref data,
                direction,
                ref velocity,
                deltaTime);

            RotateForLocomotion(
                ref data,
                direction,
                hardLockActive,
                hardLockDirection,
                deltaTime);

            return _characterController.Move(velocity * deltaTime);
        }

        public void RotateTowards(
            Vector3 direction,
            float deltaTime)
        {
            Vector3 facingDirection =
                Vector3.ProjectOnPlane(direction, Vector3.up);
            if (facingDirection.sqrMagnitude > 0.0001f)
            {
                _transform.rotation = Quaternion.Slerp(
                    _transform.rotation,
                    Quaternion.LookRotation(facingDirection),
                    _stats.RotationSpeed * deltaTime);
            }
        }

        public void RotateTowardsAtSpeed(
            Vector3 direction,
            float rotationSpeedDegrees,
            float deltaTime)
        {
            Vector3 facingDirection =
                Vector3.ProjectOnPlane(direction, Vector3.up);
            if (facingDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _transform.rotation = Quaternion.RotateTowards(
                _transform.rotation,
                Quaternion.LookRotation(facingDirection.normalized),
                rotationSpeedDegrees * deltaTime);
        }

        public void RotateForLocomotion(
            ref PlayerControllerData data,
            Vector3 movementDirection,
            bool hardLockActive,
            Vector3 hardLockDirection,
            float deltaTime)
        {
            bool hardLockFacingActive =
                hardLockActive &&
                hardLockDirection.sqrMagnitude > 0.0001f;
            bool fireFacingActive =
                data.FireFacingTicks > 0 &&
                data.FireFacingDirection.sqrMagnitude > 0.0001f;
            if (AlwaysFaceMovementDirection)
            {
                RotateTowards(movementDirection, deltaTime);
                return;
            }

            Vector3 facingDirection = hardLockFacingActive
                ? hardLockDirection
                : fireFacingActive
                    ? data.FireFacingDirection
                    : movementDirection;
            RotateTowards(facingDirection, deltaTime);
        }

        private void ApplyVerticalMovement(
            ref PlayerControllerData data,
            bool jumpPressed,
            bool ascendHeld,
            ref Vector3 velocity,
            float deltaTime)
        {
            if (data.Grounded)
            {
                data.HoverActive = false;

                if (jumpPressed)
                    velocity.y = _stats.JumpImpulse;
                else if (velocity.y < 0f)
                    velocity.y = 0f;
            }
            else if (ascendHeld)
            {
                if (_energy.ConsumeContinuous(
                    _stats.AirAscendEnergyCostPerSecond * deltaTime))
                {
                    data.HoverActive = true;
                    velocity.y = Mathf.Min(
                        velocity.y + _stats.AscendAcceleration * deltaTime,
                        _stats.MaxAscendSpeed);
                }
                else
                {
                    data.HoverActive = false;
                }
            }
            else
            {
                data.HoverActive = false;
            }

            velocity.y += _stats.Gravity * deltaTime;
        }

        private void ApplyHorizontalMovement(
            ref PlayerControllerData data,
            Vector3 direction,
            ref Vector3 velocity,
            float deltaTime)
        {
            Vector3 horizontalVelocity =
                new Vector3(velocity.x, 0f, velocity.z);
            bool quickBoostRecovering =
                data.QuickBoostTicks <= 0 &&
                data.QuickBoostCooldownTicks > 0;

            if (direction == Vector3.zero)
            {
                float braking = quickBoostRecovering
                    ? _stats.QuickBoostExitBraking
                    : _stats.Braking;
                if (data.AssaultBoostExitTicks > 0)
                    braking = _stats.AssaultBoostExitBraking;

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    braking * deltaTime);
            }
            else
            {
                float speed = data.BoostActive
                    ? _stats.BoostSpeed
                    : _stats.BaseSpeed;
                if (data.BoostActive &&
                    data.BoostIgnitionTicks > 0)
                {
                    speed *= GetBoostIgnitionSpeedMultiplier(
                        data.BoostIgnitionTicks,
                        deltaTime);
                }

                float acceleration = data.BoostActive
                    ? _stats.BoostAcceleration
                    : _stats.Acceleration;
                if (!data.BoostActive &&
                    horizontalVelocity.magnitude > _stats.BaseSpeed)
                {
                    acceleration = _stats.BoostReleaseAcceleration;
                }

                if (quickBoostRecovering)
                    acceleration = _stats.QuickBoostExitBraking;
                if (data.AssaultBoostExitTicks > 0)
                    acceleration = _stats.AssaultBoostExitBraking;

                Vector3 horizontalDirection =
                    new Vector3(direction.x, 0f, direction.z).normalized;
                Vector3 targetVelocity = horizontalDirection * speed;
                if (data.BoostActive &&
                    !quickBoostRecovering &&
                    horizontalVelocity.sqrMagnitude > 0.0001f)
                {
                    float directionDot = Vector3.Dot(
                        horizontalVelocity.normalized,
                        horizontalDirection);
                    float directionChangeWeight = Mathf.InverseLerp(
                        0.7f,
                        -1f,
                        directionDot);
                    acceleration = Mathf.Lerp(
                        acceleration,
                        _stats.DirectionChangeAcceleration,
                        directionChangeWeight);
                }

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    acceleration * deltaTime);
            }

            if (data.AssaultBoostExitTicks > 0)
                data.AssaultBoostExitTicks--;
            if (data.BoostIgnitionTicks > 0)
                data.BoostIgnitionTicks--;

            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }

        private float GetBoostIgnitionSpeedMultiplier(
            int remainingTicks,
            float deltaTime)
        {
            int totalTicks = Mathf.Max(
                1,
                Mathf.CeilToInt(_stats.BoostIgnitionDuration / deltaTime));
            float remainingRatio =
                Mathf.Clamp01((float)remainingTicks / totalTicks);

            return Mathf.Lerp(
                1f,
                _stats.BoostIgnitionSpeedMultiplier,
                remainingRatio);
        }
    }
}
