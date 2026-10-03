using System;
using Fusion;
using RIP.Weapons.Core;
using UnityEngine;

namespace RIP.Player.Modular
{
    public enum FramePartSlot
    {
        UpperBody,
        LowerBody
    }

    public enum LowerBodyType
    {
        Biped,
        Tracked
    }

    public enum FrameBoostPointType
    {
        Rear,
        Front,
        Left,
        Right,
        Foot
    }

    public enum FrameBoostPointSide
    {
        Unspecified,
        Left,
        Right
    }

    [Serializable]
    public struct FramePartStats
    {
        [Min(0f)] public float weight;
        [Min(0f)] public float apContribution;
        [Min(0f)] public float defense;

        [Header("Upper Body")]
        [Min(0f)] public float maxEnergy;
        [Min(0f)] public float energyRechargeRate;
        [Min(0f)] public float boostThrust;
        [Min(0f)] public float boostAccelerationContribution;
        [Min(0f)] public float weaponLoadLimit;
        [Min(0f)] public float firearmControl;
        [Min(0f)] public float meleePowerModifier;

        [Header("Lower Body")]
        [Min(0f)] public float loadLimit;
        [Min(0f)] public float groundSpeed;
        [Min(0.01f)] public float groundSpeedMultiplier;
        [Min(0f)] public float groundAcceleration;
        [Min(0f)] public float braking;
        [Min(0f)] public float rotationSpeed;
        [Min(0f)] public float jumpImpulse;
        [Min(0f)] public float quickBoostDuration;
        [Min(0f)] public float quickBoostCooldown;
        [Min(0.01f)] public float boostSpeedMultiplier;
    }

    [Serializable]
    public struct FrameWeaponBoneBinding
    {
        public WeaponSlotType slot;
        [Tooltip("A Transform that belongs to this part's visual prefab asset.")]
        public Transform referenceBone;
    }

    [Serializable]
    public struct FrameBoostPointBinding
    {
        public FrameBoostPointType type;
        public FrameBoostPointSide side;
        [Tooltip("A Transform that belongs to this part's visual prefab asset.")]
        public Transform referencePoint;
        [Tooltip("The boost VFX baked under this point in the visual prefab.")]
        public GameObject effectRoot;
    }

    [Serializable]
    public struct FrameLegRigBinding
    {
        public Transform root;
        public Transform mid;
        public Transform tip;
        [Tooltip("Optional toe bone that belongs to this part's visual prefab asset.")]
        public Transform toe;
        [Tooltip("Optional sole marker that belongs to this part's visual prefab asset.")]
        public Transform sole;
        [Tooltip("An explicitly authored knee hint; never inferred from the bind pose.")]
        public Transform kneeHint;
    }

    [Serializable]
    public struct FrameLowerBodyRigBinding
    {
        public Transform hips;
        public FrameLegRigBinding leftLeg;
        public FrameLegRigBinding rightLeg;
    }

    public readonly struct ResolvedFrameLegRig
    {
        public readonly Transform Root;
        public readonly Transform Mid;
        public readonly Transform Tip;
        public readonly Transform Toe;
        public readonly Transform Sole;
        public readonly Transform KneeHint;

        public ResolvedFrameLegRig(FrameLegRigBinding binding)
        {
            Root = binding.root;
            Mid = binding.mid;
            Tip = binding.tip;
            Toe = binding.toe;
            Sole = binding.sole;
            KneeHint = binding.kneeHint;
        }
    }

    public readonly struct ResolvedLowerBodyRig
    {
        public readonly Transform Hips;
        public readonly ResolvedFrameLegRig LeftLeg;
        public readonly ResolvedFrameLegRig RightLeg;

        public ResolvedLowerBodyRig(FrameLowerBodyRigBinding binding)
        {
            Hips = binding.hips;
            LeftLeg = new ResolvedFrameLegRig(binding.leftLeg);
            RightLeg = new ResolvedFrameLegRig(binding.rightLeg);
        }
    }

    [Serializable]
    public struct FrameHitboxBinding
    {
        [Tooltip("A Transform that belongs to this part's visual prefab asset.")]
        public Transform referenceBone;
        public HitboxTypes type;
        public Vector3 offset;
        public Vector3 boxExtents;
        [Min(0f)] public float sphereRadius;
        [Min(0f)] public float capsuleRadius;
        [Min(0f)] public float capsuleExtents;
    }

    public readonly struct ResolvedFrameHitbox
    {
        public readonly Transform Bone;
        public readonly HitboxTypes Type;
        public readonly Vector3 Offset;
        public readonly Vector3 BoxExtents;
        public readonly float SphereRadius;
        public readonly float CapsuleRadius;
        public readonly float CapsuleExtents;

        public ResolvedFrameHitbox(
            Transform bone,
            FrameHitboxBinding binding)
        {
            Bone = bone;
            Type = binding.type;
            Offset = binding.offset;
            BoxExtents = binding.boxExtents;
            SphereRadius = binding.sphereRadius;
            CapsuleRadius = binding.capsuleRadius;
            CapsuleExtents = binding.capsuleExtents;
        }
    }

    [CreateAssetMenu(
        fileName = "FramePartDefinition",
        menuName = "RIP/Player/Frame Part")]
    public sealed class FramePartSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string partId;
        [SerializeField] private string displayName;
        [SerializeField] private FramePartSlot slot;

        [Header("Visual")]
        [SerializeField] private GameObject visualPrefab;

        [Header("Stats")]
        [SerializeField] private FramePartStats stats;

        [Header("Lower Body Behavior")]
        [SerializeField] private LowerBodyType lowerBodyType;
        [SerializeField] private bool alwaysFaceMovementDirection;

        public string PartId => partId;
        public string DisplayName => displayName;
        public FramePartSlot Slot => slot;
        public GameObject VisualPrefab => visualPrefab;
        public FramePartStats Stats => stats;
        public LowerBodyType LowerBodyType => lowerBodyType;
        public bool AlwaysFaceMovementDirection =>
            slot == FramePartSlot.LowerBody &&
            alwaysFaceMovementDirection;

        public bool ValidateDefinition(out string error)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                error = "Part ID is empty.";
                return false;
            }

            if (visualPrefab == null)
            {
                error = $"Visual prefab is missing for '{partId}'.";
                return false;
            }

            FramePartAuthoring[] authorings =
                visualPrefab.GetComponentsInChildren<FramePartAuthoring>(true);
            if (authorings.Length != 1)
            {
                error =
                    $"'{visualPrefab.name}' must contain exactly one FramePartAuthoring; found {authorings.Length}.";
                return false;
            }

            if (!authorings[0].Validate(
                    visualPrefab,
                    slot,
                    lowerBodyType,
                    out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
