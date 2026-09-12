using RIP.Player.Health;
using RIP.Multiplayer;
using UnityEngine;


namespace RIP.Combat
{
    public static class DamageResolver
    {
        public static DamageResolved Resolve(UnitCombatState target, DamageRequest request)
        {
            if (target == null || !target.HasStateAuthority)
            {
                return default;
            }


            float apBefore = target.CurrentAp;
            float acsBefore = target.AcsStrain;
            float requestedDamage = Mathf.Max(0f, request.Damage) *
                MultiplayerMatchManager.GetDamageMultiplier(target.Runner);
            float requestedImpact = Mathf.Max(0f, request.Impact);
            float unshieldedArmorMitigatedDamage = target.GetArmorMitigatedDamage(
                requestedDamage,
                request.DamageType);
            float unshieldedApDamage = target.IsAcsOverloaded
                ? unshieldedArmorMitigatedDamage * target.AcsOverloadDamageMultiplier
                : unshieldedArmorMitigatedDamage;


            // Resolve the deployed field before personal shields so invulnerability preserves shield durability.
            if (target.TryNullifyDamage(
                    request,
                    unshieldedApDamage,
                    out float nullificationFieldDamage))
            {
                return new DamageResolved(
                    request.SourceObject,
                    target.Object,
                    request.HitPosition,
                    request.DamageType,
                    requestedDamage,
                    requestedImpact,
                    0f,
                    unshieldedArmorMitigatedDamage,
                    0f,
                    apBefore,
                    apBefore,
                    acsBefore,
                    acsBefore,
                    target.IsAcsOverloaded,
                    false,
                    false,
                    target.IsDestroyed,
                    true,
                    nullificationFieldDamage,
                    request.AttackId);
            }


            float shieldDamage = target.AbsorbShieldDamage(requestedDamage, request.DamageType);
            float armorMitigatedDamage = target.GetArmorMitigatedDamage(
                Mathf.Max(0f, requestedDamage - shieldDamage),
                request.DamageType);
            float apDamage = target.IsAcsOverloaded
                ? armorMitigatedDamage * target.AcsOverloadDamageMultiplier
                : armorMitigatedDamage;


            target.ApplyApDamage(apDamage, request.Attacker);
            bool startedOverload = target.ApplyImpact(requestedImpact);
            float apAfter = target.CurrentAp;


            return new DamageResolved(
                request.SourceObject,
                target.Object,
                request.HitPosition,
                request.DamageType,
                requestedDamage,
                requestedImpact,
                shieldDamage,
                armorMitigatedDamage,
                apDamage,
                apBefore,
                apAfter,
                acsBefore,
                target.AcsStrain,
                target.IsAcsOverloaded,
                startedOverload,
                false,
                target.IsDestroyed,
                false,
                0f,
                request.AttackId);
        }
    }
}
