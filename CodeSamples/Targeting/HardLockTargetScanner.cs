using System.Collections.Generic;
using Fusion;
using RIP.Player.Health;
using UnityEngine;


namespace RIP.PlayerCamera
{
    internal static class HardLockTargetScanner
    {
        public static void Scan(
            List<HardLockTarget> results,
            Vector3 origin,
            float radius,
            NetworkObject sourceObject,
            NetworkRunner runner)
        {
            results.Clear();
            float radiusSquared = radius * radius;


            foreach (HardLockTarget candidate in HardLockTarget.All)
            {
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;


                NetworkObject candidateObject =
                    candidate.GetComponentInParent<NetworkObject>();
                if (candidateObject == sourceObject ||
                    (candidateObject != null &&
                     candidateObject.Runner != runner))
                {
                    continue;
                }


                PlayerHealth health =
                    candidate.GetComponentInParent<PlayerHealth>();
                if (health != null && health.IsDead)
                    continue;


                Vector3 offset = candidate.AimPoint.position - origin;
                if (offset.sqrMagnitude <= radiusSquared)
                    results.Add(candidate);
            }
        }
    }
}
