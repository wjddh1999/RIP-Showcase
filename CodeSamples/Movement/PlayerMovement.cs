using Fusion;
using RIP.Player.Input;
using UnityEngine;

namespace RIP.Player.Move
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerNetworkController))]
    public sealed class PlayerMovement : NetworkBehaviour
    {
        private PlayerNetworkController _controller;

        private void Awake()
        {
            _controller = GetComponent<PlayerNetworkController>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority && !Object.HasInputAuthority)
                return;

            Vector3 direction = Vector3.zero;
            Vector3 quickBoostDirection = Vector3.zero;
            Vector3 cameraForward = Vector3.forward;
            bool hardLockActive = false;
            Vector3 hardLockDirection = Vector3.zero;
            NetworkButtons buttons = default;

            if (!GetInput(out PlayerNetworkInput input))
                return;

            Vector3 localDirection = new Vector3(input.Move.x, 0f, input.Move.y);
            float cameraPitch = PlayerNetworkInput.DecodeAngle(input.CameraPitch);
            float cameraYaw = PlayerNetworkInput.DecodeAngle(input.CameraYaw);
            float movementPitch = _controller.Grounded
                ? 0f
                : cameraPitch;
            Quaternion movementCameraRotation = Quaternion.Euler(
                movementPitch,
                cameraYaw,
                0f);
            Quaternion viewCameraRotation = Quaternion.Euler(
                cameraPitch,
                cameraYaw,
                0f);
            direction = movementCameraRotation * localDirection;
            quickBoostDirection =
                GetQuickBoostDirection(
                    input.Move,
                    movementCameraRotation);
            cameraForward = viewCameraRotation * Vector3.forward;
            hardLockActive = input.HardLockActive;
            hardLockDirection =
                PlayerNetworkInput.DecodeDirection(
                    input.HardLockPitch,
                    input.HardLockYaw);
            buttons = input.Buttons;

            _controller.Move(
                direction,
                quickBoostDirection,
                cameraForward,
                hardLockActive,
                hardLockDirection,
                buttons);
        }

        private static Vector3 GetQuickBoostDirection(
            Vector2 move,
            Quaternion cameraRotation)
        {
            if (move.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            Vector3 cameraSide =
                cameraRotation * new Vector3(Mathf.Sign(move.x), 0f, 0f);
            Vector3 cameraForward =
                cameraRotation * new Vector3(0f, 0f, Mathf.Sign(move.y));

            if (Mathf.Abs(move.x) <= 0.0001f)
                return cameraForward;

            if (Mathf.Abs(move.y) <= 0.0001f)
                return cameraSide;

            return Mathf.Abs(move.x) >= Mathf.Abs(move.y)
                ? cameraSide
                : cameraForward;
        }
    }
}
