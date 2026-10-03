using Fusion;
using UnityEngine;

namespace RIP.PlayerCamera
{
    // Selected members; other lifecycle, UI and helper members are omitted.
    public sealed class PlayerHardLockController : NetworkBehaviour
    {

        [SerializeField] private LayerMask _lineOfSightLayers =
            Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] _lineOfSightHits = new RaycastHit[16];

        private bool HasLineOfSight(HardLockTarget target)
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null)
                return true;

            Vector3 origin = camera.transform.position;
            Vector3 toAimPoint = target.AimPoint.position - origin;
            float distance = toAimPoint.magnitude;
            if (distance <= 0.0001f)
                return true;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                toAimPoint / distance,
                _lineOfSightHits,
                distance,
                _lineOfSightLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _lineOfSightHits[i].collider;
                if (hitCollider == null ||
                    IsOwnCollider(hitCollider) ||
                    IsTargetCollider(hitCollider, target))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool IsOwnCollider(Collider collider)
        {
            NetworkObject colliderObject =
                collider.GetComponentInParent<NetworkObject>();
            return colliderObject == Object;
        }

        private static bool IsTargetCollider(
            Collider collider,
            HardLockTarget target)
        {
            if (collider.GetComponentInParent<HardLockTarget>() == target)
                return true;

            NetworkObject targetObject =
                target.GetComponentInParent<NetworkObject>();
            return targetObject != null &&
                   collider.GetComponentInParent<NetworkObject>() == targetObject;
        }

    }
}
