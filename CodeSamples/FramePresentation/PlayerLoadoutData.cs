using System;
using RIP.Player.Modular;
using RIP.Weapons.Core;

namespace RIP.Weapons.Equip
{
    [Serializable]
    public struct PlayerLoadoutData
    {
        public const int CurrentVersion = 1;

        public int version;
        public string upperBodyPartId;
        public string lowerBodyPartId;
        public int paintColorRgba;
        public int upperBodyPaintColorRgba;
        public int lowerBodyPaintColorRgba;
        public bool weaponsInitialized;
        public PlayerWeaponLoadout weapons;

        public string GetPartId(FramePartSlot slot)
        {
            return slot == FramePartSlot.UpperBody
                ? upperBodyPartId
                : lowerBodyPartId;
        }

        public void SetPartId(FramePartSlot slot, string partId)
        {
            if (slot == FramePartSlot.UpperBody)
            {
                upperBodyPartId = partId;
            }
            else
            {
                lowerBodyPartId = partId;
            }
        }

        public int GetPaintColorRgba(FramePartSlot slot)
        {
            int partColor = slot == FramePartSlot.UpperBody
                ? upperBodyPaintColorRgba
                : lowerBodyPaintColorRgba;
            return partColor != FramePaintColorUtility.LegacyDefaultPacked
                ? partColor
                : paintColorRgba;
        }
    }
}
