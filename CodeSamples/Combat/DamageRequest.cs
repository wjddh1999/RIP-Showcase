using Fusion;
using RIP.Weapons.Core;
using UnityEngine;


namespace RIP.Combat
{
    public readonly struct DamageRequest
    {
        public readonly NetworkObject SourceObject;
        public readonly NetworkObject ProjectileObject;
        public readonly NetworkObject TargetObject;
        public readonly Vector3 HitPosition;
        public readonly float Damage;
        public readonly float Impact;
        public readonly DamageType DamageType;
        public readonly int AttackId;


        public DamageRequest(
            NetworkObject sourceObject,
            NetworkObject projectileObject,
            NetworkObject targetObject,
            Vector3 hitPosition,
            float damage,
            float impact,
            DamageType damageType,
            int attackId = 0)
        {
            SourceObject = sourceObject;
            ProjectileObject = projectileObject;
            TargetObject = targetObject;
            HitPosition = hitPosition;
            Damage = damage;
            Impact = impact;
            DamageType = damageType;
            AttackId = attackId;
        }


        public PlayerRef Attacker => SourceObject != null
            ? SourceObject.InputAuthority
            : PlayerRef.None;


        public static int CreateAttackId(
            NetworkObject sourceObject,
            NetworkObject attackObject,
            int simulationTick)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (sourceObject != null ? sourceObject.Id.GetHashCode() : 0);
                hash = hash * 31 + (attackObject != null ? attackObject.Id.GetHashCode() : 0);
                hash = hash * 31 + simulationTick;
                return hash != 0 ? hash : int.MinValue;
            }
        }
    }
}
