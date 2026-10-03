using System.Collections.Generic;
using UnityEngine;

namespace RIP.Player.Modular
{
    [CreateAssetMenu(
        fileName = "FramePartDatabase",
        menuName = "RIP/Player/Frame Part Database")]
    public sealed class FramePartDatabaseSO : ScriptableObject
    {
        [SerializeField] private FramePartSO[] parts;
        [SerializeField] private FramePartSO defaultUpperBody;
        [SerializeField] private FramePartSO defaultLowerBody;

        public FramePartSO DefaultUpperBody => defaultUpperBody;
        public FramePartSO DefaultLowerBody => defaultLowerBody;

        public FramePartSO GetPartById(string partId, FramePartSlot expectedSlot)
        {
            if (string.IsNullOrWhiteSpace(partId) || parts == null)
            {
                return null;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                FramePartSO part = parts[i];
                if (part != null && part.Slot == expectedSlot && part.PartId == partId)
                {
                    return part;
                }
            }

            return null;
        }

        public FramePartSO GetDefault(FramePartSlot slot)
        {
            return slot == FramePartSlot.UpperBody
                ? defaultUpperBody
                : defaultLowerBody;
        }

        public FramePartSO GetFirstPart(FramePartSlot slot)
        {
            if (parts == null)
            {
                return null;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                FramePartSO part = parts[i];
                if (part != null && part.Slot == slot)
                {
                    return part;
                }
            }

            return null;
        }

        public int GetPartsForSlot(
            FramePartSlot slot,
            FramePartSO[] destination)
        {
            if (parts == null || destination == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < parts.Length && count < destination.Length; i++)
            {
                FramePartSO part = parts[i];
                if (part != null && part.Slot == slot)
                {
                    destination[count++] = part;
                }
            }

            return count;
        }

        public bool ValidateDefinitions(out string error)
        {
            HashSet<string> partIds = new HashSet<string>();
            if (parts == null)
            {
                error = "Frame part array is missing.";
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                FramePartSO part = parts[i];
                if (part == null)
                {
                    error = $"Frame part at index {i} is missing.";
                    return false;
                }

                if (!part.ValidateDefinition(out error))
                {
                    error = $"{part.name}: {error}";
                    return false;
                }

                if (!partIds.Add(part.PartId))
                {
                    error = $"Duplicate frame Part ID: '{part.PartId}'.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            if (defaultUpperBody != null && defaultUpperBody.Slot != FramePartSlot.UpperBody)
            {
                Debug.LogError("Default upper-body part has the wrong slot.", this);
            }

            if (defaultLowerBody != null && defaultLowerBody.Slot != FramePartSlot.LowerBody)
            {
                Debug.LogError("Default lower-body part has the wrong slot.", this);
            }

            if (!ValidateDefinitions(out string error))
            {
                Debug.LogError(error, this);
            }
        }
    }
}
