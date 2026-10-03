namespace RIP.Player.Modular
{
    public readonly struct ResolvedFrameStats
    {
        public readonly float MaxHealth;
        public readonly float Defense;
        public readonly float MaxEnergy;
        public readonly float EnergyRechargeRate;
        public readonly float BoostThrust;
        public readonly float BoostAcceleration;
        public readonly float WeaponLoadLimit;
        public readonly float FirearmControl;
        public readonly float MeleePowerModifier;
        public readonly float TotalWeight;
        public readonly float LoadLimit;
        public readonly float LoadRatio;
        public readonly float GroundSpeed;
        public readonly float GroundAcceleration;
        public readonly float Braking;
        public readonly float RotationSpeed;
        public readonly float JumpImpulse;
        public readonly float QuickBoostDuration;
        public readonly float QuickBoostCooldown;
        public readonly float BoostSpeedMultiplier;
        public readonly bool AlwaysFaceMovementDirection;

        public ResolvedFrameStats(
            FramePartStats upper,
            FramePartStats lower,
            bool alwaysFaceMovementDirection)
        {
            MaxHealth = upper.apContribution + lower.apContribution;
            Defense = upper.defense + lower.defense;
            MaxEnergy = upper.maxEnergy + lower.maxEnergy;
            EnergyRechargeRate = upper.energyRechargeRate + lower.energyRechargeRate;
            BoostThrust = upper.boostThrust + lower.boostThrust;
            BoostAcceleration =
                upper.boostAccelerationContribution +
                lower.boostAccelerationContribution;
            WeaponLoadLimit = upper.weaponLoadLimit + lower.weaponLoadLimit;
            FirearmControl = upper.firearmControl + lower.firearmControl;
            MeleePowerModifier = upper.meleePowerModifier + lower.meleePowerModifier;
            TotalWeight = upper.weight + lower.weight;
            LoadLimit = upper.loadLimit + lower.loadLimit;
            LoadRatio = LoadLimit > 0f ? TotalWeight / LoadLimit : 0f;
            float upperGroundSpeedMultiplier =
                upper.groundSpeedMultiplier > 0f
                    ? upper.groundSpeedMultiplier
                    : 1f;
            GroundSpeed =
                (upper.groundSpeed + lower.groundSpeed) *
                upperGroundSpeedMultiplier;
            GroundAcceleration = upper.groundAcceleration + lower.groundAcceleration;
            Braking = upper.braking + lower.braking;
            RotationSpeed = upper.rotationSpeed + lower.rotationSpeed;
            JumpImpulse = upper.jumpImpulse + lower.jumpImpulse;
            QuickBoostDuration = upper.quickBoostDuration + lower.quickBoostDuration;
            QuickBoostCooldown = upper.quickBoostCooldown + lower.quickBoostCooldown;
            BoostSpeedMultiplier = lower.boostSpeedMultiplier > 0f
                ? lower.boostSpeedMultiplier
                : 1f;
            AlwaysFaceMovementDirection = alwaysFaceMovementDirection;
        }
    }
}
