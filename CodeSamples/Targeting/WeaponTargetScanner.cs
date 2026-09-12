using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace RIP.Weapons.Targeting
{
    public sealed class WeaponTargetScanner : NetworkBehaviour
    {
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private float targetSearchRadius = 80f;
        [SerializeField, Range(1f, 360f)] private float targetSearchAngle = 60f;
        [SerializeField] private float fallbackRange = 80f;

        private readonly List<LagCompensatedHit> _lagCompensatedHits = new List<LagCompensatedHit>(32);

        public Vector3 GetTargetPosition(
            NetworkRunner runner,
            PlayerRef sourcePlayer,
            NetworkObject sourceObject,
            Vector3 origin,
            Vector3 direction)
        {
            if (TryFindTargetPosition(runner, sourcePlayer, sourceObject, origin, direction, out Vector3 targetPosition))
            {
                return targetPosition;
            }

            return GetFallbackTargetPosition(origin, direction);
        }

        public bool TryFindTargetPosition(
            NetworkRunner runner,
            PlayerRef sourcePlayer,
            NetworkObject sourceObject,
            Vector3 origin,
            Vector3 direction,
            out Vector3 targetPosition)
        {
            targetPosition = default;

            if (runner == null || runner.LagCompensation == null)
            {
                return false;
            }

            int hitCount = runner.LagCompensation.OverlapSphere(
                origin,
                targetSearchRadius,
                sourcePlayer,
                _lagCompensatedHits,
                targetLayers,
                HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority,
                true,
                QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                return false;
            }

            float nearestSqrDistance = float.MaxValue;
            Vector3 forward = GetSearchForward(direction);
            float minDot = GetSearchMinDot();

            for (int i = 0; i < hitCount; i++)
            {
                LagCompensatedHit hit = _lagCompensatedHits[i];

                if (IsSourceHit(hit, sourceObject))
                {
                    continue;
                }

                Vector3 hitPosition = GetHitPosition(hit);
                Vector3 toTarget = hitPosition - origin;

                if (!IsInsideSearchAngle(forward, toTarget, minDot))
                {
                    continue;
                }

                float sqrDistance = (hitPosition - origin).sqrMagnitude;

                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                targetPosition = hitPosition;
            }

            return nearestSqrDistance < float.MaxValue;
        }

        private Vector3 GetFallbackTargetPosition(Vector3 origin, Vector3 direction)
        {
            Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : transform.forward;

            return origin + normalizedDirection * fallbackRange;
        }

        private Vector3 GetSearchForward(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            return transform.forward;
        }

        private float GetSearchMinDot()
        {
            if (targetSearchAngle >= 359.9f)
            {
                return -1f;
            }

            return Mathf.Cos(targetSearchAngle * 0.5f * Mathf.Deg2Rad);
        }

        private static bool IsInsideSearchAngle(Vector3 forward, Vector3 toTarget, float minDot)
        {
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float dot = Vector3.Dot(forward, toTarget.normalized);
            return dot >= minDot;
        }

        private static bool IsSourceHit(LagCompensatedHit hit, NetworkObject sourceObject)
        {
            if (sourceObject == null || hit.GameObject == null)
            {
                return false;
            }

            NetworkObject hitNetworkObject = hit.GameObject.GetComponentInParent<NetworkObject>();
            return hitNetworkObject == sourceObject;
        }

        private static Vector3 GetHitPosition(LagCompensatedHit hit)
        {
            if (hit.Hitbox != null)
            {
                return hit.Hitbox.Position;
            }

            return hit.Point != default
                ? hit.Point
                : hit.GameObject != null ? hit.GameObject.transform.position : Vector3.zero;
        }

    }

}
