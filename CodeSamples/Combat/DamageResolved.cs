using Fusion;
using RIP.Weapons.Core;
using UnityEngine;


namespace RIP.Combat
{
    public readonly struct DamageResolved
    {
        public readonly NetworkObject SourceObject;
        public readonly NetworkObject TargetObject;
        public readonly Vector3 HitPosition;
        public readonly DamageType DamageType;
        public readonly float RequestedDamage;
        public readonly float RequestedImpact;
        public readonly float ShieldDamage;
        public readonly float ArmorMitigatedDamage;
        public readonly float ApDamage;
        public readonly float ApBefore;
        public readonly float ApAfter;
        public readonly float AcsStrainBefore;
        public readonly float AcsStrainAfter;
        public readonly NetworkBool IsAcsOverloaded;
        public readonly NetworkBool StartedAcsOverload;
        public readonly NetworkBool RecoveredAcsOverload;
        public readonly NetworkBool Destroyed;
        public readonly NetworkBool WasNullified;
        public readonly float NullificationFieldDamage;
        public readonly int AttackId;


        public DamageResolved(
            NetworkObject sourceObject,
            NetworkObject targetObject,
            Vector3 hitPosition,
            DamageType damageType,
            float requestedDamage,
            float requestedImpact,
            float shieldDamage,
            float armorMitigatedDamage,
            float apDamage,
            float apBefore,
            float apAfter,
            float acsStrainBefore,
            float acsStrainAfter,
            NetworkBool isAcsOverloaded,
            NetworkBool startedAcsOverload,
            NetworkBool recoveredAcsOverload,
            NetworkBool destroyed,
            NetworkBool wasNullified = default,
            float nullificationFieldDamage = 0f,
            int attackId = 0)
        {
            SourceObject = sourceObject;
            TargetObject = targetObject;
            HitPosition = hitPosition;
            DamageType = damageType;
            RequestedDamage = requestedDamage;
            RequestedImpact = requestedImpact;
            ShieldDamage = shieldDamage;
            ArmorMitigatedDamage = armorMitigatedDamage;
            ApDamage = apDamage;
            ApBefore = apBefore;
            ApAfter = apAfter;
            AcsStrainBefore = acsStrainBefore;
            AcsStrainAfter = acsStrainAfter;
            IsAcsOverloaded = isAcsOverloaded;
            StartedAcsOverload = startedAcsOverload;
            RecoveredAcsOverload = recoveredAcsOverload;
            Destroyed = destroyed;
            WasNullified = wasNullified;
            NullificationFieldDamage = nullificationFieldDamage;
            AttackId = attackId;
        }
    }
}
