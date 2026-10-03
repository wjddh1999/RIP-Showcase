using Fusion;
using RIP.Weapons.Equip;
using UnityEngine;

namespace RIP.Player.Modular
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FrameAssemblyController))]
    public sealed class NetworkModularPlayer : NetworkBehaviour
    {
        [Networked] public NetworkString<_64> UpperBodyPartId { get; private set; }
        [Networked] public NetworkString<_64> LowerBodyPartId { get; private set; }
        [Networked] public int PaintColorRgba { get; private set; }
        [Networked] public int UpperBodyPaintColorRgba { get; private set; }
        [Networked] public int LowerBodyPaintColorRgba { get; private set; }
        [Networked] public int BuildVersion { get; private set; }

        private int _appliedBuildVersion = -1;
        private FrameAssemblyController _assemblyController;
        private FramePaintController _paintController;

        private void Awake()
        {
            _assemblyController = GetComponent<FrameAssemblyController>();
        }

        public override void Spawned()
        {
            ResolveReferences();

            if (HasStateAuthority && BuildVersion == 0)
            {
                CommitLoadout(
                    _assemblyController.InitialUpperBodyPartId,
                    _assemblyController.InitialLowerBodyPartId,
                    FramePaintColorUtility.LegacyDefaultPacked,
                    FramePaintColorUtility.LegacyDefaultPacked,
                    FramePaintColorUtility.LegacyDefaultPacked);
            }

            ApplyNetworkedBuildIfChanged();
        }

        public override void FixedUpdateNetwork()
        {
            ApplyNetworkedBuildIfChanged();
        }

        public void InitializeBeforeSpawn(PlayerLoadoutData loadout)
        {
            ResolveReferences();
            CommitLoadout(
                loadout.upperBodyPartId,
                loadout.lowerBodyPartId,
                loadout.paintColorRgba,
                loadout.GetPaintColorRgba(FramePartSlot.UpperBody),
                loadout.GetPaintColorRgba(FramePartSlot.LowerBody));

            // Spawned에서 다른 런타임 컴포넌트가 초기화된 뒤 동일 빌드를 다시 적용한다.
            _appliedBuildVersion = -1;
        }

        private void CommitLoadout(
            string upperBodyPartId,
            string lowerBodyPartId,
            int paintColorRgba,
            int upperBodyPaintColorRgba,
            int lowerBodyPaintColorRgba)
        {
            if (_assemblyController == null)
            {
                return;
            }

            FramePartDatabaseSO database = _assemblyController.Database;
            FramePartSO upper = database != null
                ? database.GetPartById(upperBodyPartId, FramePartSlot.UpperBody)
                : null;
            FramePartSO lower = database != null
                ? database.GetPartById(lowerBodyPartId, FramePartSlot.LowerBody)
                : null;
            upper ??= database != null
                ? database.GetDefault(FramePartSlot.UpperBody) ??
                  database.GetFirstPart(FramePartSlot.UpperBody)
                : null;
            lower ??= database != null
                ? database.GetDefault(FramePartSlot.LowerBody) ??
                  database.GetFirstPart(FramePartSlot.LowerBody)
                : null;

            if (upper == null || lower == null)
            {
                Debug.LogError("Cannot commit a modular build without valid frame parts.", this);
                return;
            }

            UpperBodyPartId = upper.PartId;
            LowerBodyPartId = lower.PartId;
            PaintColorRgba = paintColorRgba;
            UpperBodyPaintColorRgba = upperBodyPaintColorRgba;
            LowerBodyPaintColorRgba = lowerBodyPaintColorRgba;
            BuildVersion++;
            ApplyNetworkedBuildIfChanged();
        }

        private void ApplyNetworkedBuildIfChanged()
        {
            if (_assemblyController == null || BuildVersion <= 0 || BuildVersion == _appliedBuildVersion)
            {
                return;
            }

            _paintController?.SetPaintColors(
                FramePaintColorUtility.Unpack(UpperBodyPaintColorRgba),
                FramePaintColorUtility.Unpack(LowerBodyPaintColorRgba));

            if (_assemblyController.ApplyLoadout(
                    UpperBodyPartId.ToString(),
                    LowerBodyPartId.ToString()))
            {
                _appliedBuildVersion = BuildVersion;
            }
        }

        private void ResolveReferences()
        {
            if (_assemblyController == null)
            {
                _assemblyController = GetComponent<FrameAssemblyController>();
            }

            if (_paintController == null)
            {
                _paintController = GetComponent<FramePaintController>();
            }
        }
    }
}
