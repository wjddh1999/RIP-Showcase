using System.Collections.Generic;
using UnityEngine;

namespace RIP.PlayerCamera
{
    [DisallowMultipleComponent]
    public sealed class HardLockTarget : MonoBehaviour
    {
        private static readonly HashSet<HardLockTarget> ActiveTargets = new();

        [SerializeField] private Transform _aimPoint;

        public static IEnumerable<HardLockTarget> All => ActiveTargets;

        public Transform AimPoint =>
            _aimPoint != null ? _aimPoint : transform;

        private void OnEnable()
        {
            ActiveTargets.Add(this);
        }

        private void OnDisable()
        {
            ActiveTargets.Remove(this);
        }
    }
}
