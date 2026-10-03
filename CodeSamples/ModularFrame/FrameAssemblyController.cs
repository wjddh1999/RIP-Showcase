using System;
using RIP.Weapons.Core;
using RIP.Player.Energy;
using RIP.Player.Health;
using RIP.Player.Move;
using UnityEngine;

namespace RIP.Player.Modular
{
    [DisallowMultipleComponent]
    public sealed class FrameAssemblyController : MonoBehaviour
    {
        [SerializeField] private FramePartDatabaseSO database;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private string initialUpperBodyPartId;
        [SerializeField] private string initialLowerBodyPartId;

        private GameObject _upperBodyInstance;
        private GameObject _lowerBodyInstance;
        private FramePartSO _activeUpperBody;
        private FramePartSO _activeLowerBody;
        private FramePartAuthoring _upperBodyAuthoring;
        private FramePartAuthoring _lowerBodyAuthoring;
        private Animator _upperBodyAnimator;
        private Animator _lowerBodyAnimator;
        private string _activeUpperBodyPartId = string.Empty;
        private string _activeLowerBodyPartId = string.Empty;
        private Transform _upperBodyAimPivot;

        public event Action AssemblyChanged;

        public FramePartDatabaseSO Database => database;
        public string InitialUpperBodyPartId => initialUpperBodyPartId;
        public string InitialLowerBodyPartId => initialLowerBodyPartId;
        public string ActiveUpperBodyPartId => _activeUpperBodyPartId;
        public string ActiveLowerBodyPartId => _activeLowerBodyPartId;
        public Animator UpperBodyAnimator => _upperBodyAnimator;
        public Animator LowerBodyAnimator => _lowerBodyAnimator;
        public Transform UpperBodyAimPivot => _upperBodyAimPivot;
        public bool AllowsIndependentUpperBodyYaw =>
            _activeLowerBody != null &&
            _activeLowerBody.AlwaysFaceMovementDirection;
        public ResolvedFrameStats ResolvedStats { get; private set; }

        private void Awake()
        {
            EnsureVisualRoot();
        }

        private void Start()
        {
            if (_upperBodyInstance == null && _lowerBodyInstance == null)
            {
                ApplyLoadout(initialUpperBodyPartId, initialLowerBodyPartId);
            }
        }

        public bool ApplyLoadout(string upperBodyPartId, string lowerBodyPartId)
        {
            if (database == null)
            {
                Debug.LogError("Frame part database is not assigned.", this);
                return false;
            }

            FramePartSO upperDefinition = ResolvePart(
                upperBodyPartId,
                FramePartSlot.UpperBody);
            FramePartSO lowerDefinition = ResolvePart(
                lowerBodyPartId,
                FramePartSlot.LowerBody);

            if (upperDefinition == null || lowerDefinition == null)
            {
                Debug.LogError("A complete upper/lower frame loadout could not be resolved.", this);
                return false;
            }

            if (_activeUpperBody == upperDefinition && _activeLowerBody == lowerDefinition)
            {
                ResolveAndApplyStats(upperDefinition, lowerDefinition);
                AssemblyChanged?.Invoke();
                return true;
            }

            bool upperIsValid = upperDefinition.ValidateDefinition(out string upperError);
            bool lowerIsValid = lowerDefinition.ValidateDefinition(out string lowerError);
            if (!upperIsValid || !lowerIsValid)
            {
                Debug.LogError(
                    $"Frame definition validation failed. Upper='{upperError}' Lower='{lowerError}'",
                    this);
                return false;
            }

            EnsureVisualRoot();
            GameObject preparedLower = Instantiate(
                lowerDefinition.VisualPrefab,
                visualRoot);
            GameObject preparedUpper = Instantiate(
                upperDefinition.VisualPrefab,
                visualRoot);
            SetLayerRecursively(preparedLower, gameObject.layer);
            SetLayerRecursively(preparedUpper, gameObject.layer);
            preparedLower.name = lowerDefinition.PartId;
            preparedUpper.name = upperDefinition.PartId;
            preparedLower.SetActive(false);
            preparedUpper.SetActive(false);

            FramePartAuthoring preparedLowerAuthoring =
                preparedLower.GetComponentInChildren<FramePartAuthoring>(true);
            FramePartAuthoring preparedUpperAuthoring =
                preparedUpper.GetComponentInChildren<FramePartAuthoring>(true);
            Animator preparedUpperAnimator =
                preparedUpper.GetComponentInChildren<Animator>(true);
            Animator preparedLowerAnimator =
                preparedLower.GetComponentInChildren<Animator>(true);
            Transform lowerMount = preparedLowerAuthoring != null
                ? preparedLowerAuthoring.AssemblyBone
                : null;
            Transform upperAnchor = preparedUpperAuthoring != null
                ? preparedUpperAuthoring.AssemblyBone
                : null;
            if (lowerMount == null || upperAnchor == null)
            {
                Destroy(preparedUpper);
                Destroy(preparedLower);
                Debug.LogError(
                    "Prepared frame is missing its authored assembly bone.",
                    this);
                return false;
            }

            AlignUpperBody(preparedUpper.transform, upperAnchor, lowerMount);
            Transform preparedAimPivot = CreateUpperBodyAimPivot(
                lowerMount,
                preparedUpper.transform);
            preparedLower.SetActive(true);
            preparedUpper.SetActive(true);

            GameObject previousUpper = _upperBodyInstance;
            GameObject previousLower = _lowerBodyInstance;
            _upperBodyInstance = preparedUpper;
            _lowerBodyInstance = preparedLower;
            _activeUpperBody = upperDefinition;
            _activeLowerBody = lowerDefinition;
            _upperBodyAuthoring = preparedUpperAuthoring;
            _lowerBodyAuthoring = preparedLowerAuthoring;
            _upperBodyAnimator = preparedUpperAnimator;
            _lowerBodyAnimator = preparedLowerAnimator;
            _activeUpperBodyPartId = upperDefinition.PartId;
            _activeLowerBodyPartId = lowerDefinition.PartId;
            _upperBodyAimPivot = preparedAimPivot;
            ResolveAndApplyStats(upperDefinition, lowerDefinition);

            AssemblyChanged?.Invoke();

            if (previousUpper != null)
            {
                Destroy(previousUpper);
            }

            if (previousLower != null)
            {
                Destroy(previousLower);
            }

            return true;
        }

        public bool TryGetWeaponBone(WeaponSlotType slot, out Transform bone)
        {
            bone = null;

            if (_upperBodyAuthoring != null &&
                _upperBodyAuthoring.TryGetWeaponBone(slot, out bone))
            {
                return true;
            }

            return _lowerBodyAuthoring != null &&
                   _lowerBodyAuthoring.TryGetWeaponBone(slot, out bone);
        }

        public bool UseDefaultLeftHandForwardEulerOffset =>
            _upperBodyAuthoring == null ||
            _upperBodyAuthoring.UseDefaultLeftHandForwardEulerOffset;

        public bool TryGetArmChain(
            WeaponSlotType slot,
            out Transform root,
            out Transform mid,
            out Transform tip)
        {
            if (_upperBodyAuthoring != null &&
                _upperBodyAuthoring.TryGetArmChain(
                    slot,
                    out root,
                    out mid,
                    out tip))
            {
                return true;
            }

            root = null;
            mid = null;
            tip = null;
            return false;
        }

        public int GetBoostPoints(
            FrameBoostPointType type,
            Transform[] destination)
        {
            if (destination == null || destination.Length == 0)
            {
                return 0;
            }

            int count = 0;
            if (type == FrameBoostPointType.Rear &&
                destination.Length >= 2 &&
                _lowerBodyAuthoring != null &&
                _lowerBodyAuthoring.TryGetOutermostBoostPointPair(
                    type,
                    out Transform leftRear,
                    out Transform rightRear))
            {
                destination[0] = leftRear;
                destination[1] = rightRear;
                return 2;
            }

            if (_upperBodyAuthoring != null)
            {
                count += _upperBodyAuthoring.CopyBoostPoints(
                    type,
                    destination,
                    count);
            }

            if (_lowerBodyAuthoring != null &&
                count < destination.Length &&
                type != FrameBoostPointType.Rear)
            {
                count += _lowerBodyAuthoring.CopyBoostPoints(
                    type,
                    destination,
                    count);
            }

            return count;
        }

        public int GetBoostEffects(
            FrameBoostPointType type,
            GameObject[] destination)
        {
            if (destination == null || destination.Length == 0)
            {
                return 0;
            }

            int count = 0;
            if (type == FrameBoostPointType.Rear &&
                destination.Length >= 2 &&
                _lowerBodyAuthoring != null &&
                _lowerBodyAuthoring.TryGetOutermostBoostEffectPair(
                    type,
                    out GameObject leftRear,
                    out GameObject rightRear))
            {
                destination[0] = leftRear;
                destination[1] = rightRear;
                return 2;
            }

            if (_upperBodyAuthoring != null)
            {
                count += _upperBodyAuthoring.CopyBoostEffects(
                    type,
                    destination,
                    count);
            }

            if (_lowerBodyAuthoring != null &&
                count < destination.Length &&
                type != FrameBoostPointType.Rear)
            {
                count += _lowerBodyAuthoring.CopyBoostEffects(
                    type,
                    destination,
                    count);
            }

            return count;
        }

        public bool TryResolveHitbox(
            FramePartSlot slot,
            out ResolvedFrameHitbox hitbox)
        {
            FramePartAuthoring authoring =
                slot == FramePartSlot.UpperBody
                    ? _upperBodyAuthoring
                    : _lowerBodyAuthoring;
            if (authoring != null &&
                authoring.TryResolveHitbox(out hitbox))
            {
                return true;
            }

            hitbox = default;
            return false;
        }

        public bool TryGetLowerBodyRig(out ResolvedLowerBodyRig rig)
        {
            if (_activeLowerBody != null &&
                _activeLowerBody.LowerBodyType == LowerBodyType.Biped &&
                _lowerBodyAuthoring != null &&
                _lowerBodyAuthoring.TryResolveLowerBodyRig(out rig))
            {
                return true;
            }

            rig = default;
            return false;
        }

        public bool TryGetUpperBodyPoseBone(out Transform bone)
        {
            if (_upperBodyAuthoring != null &&
                _upperBodyAuthoring.TryGetUpperBodyPoseBone(out bone))
            {
                return true;
            }

            bone = null;
            return false;
        }

        private FramePartSO ResolvePart(string partId, FramePartSlot slot)
        {
            FramePartSO part = database.GetPartById(partId, slot);
            return part != null
                ? part
                : database.GetDefault(slot) ?? database.GetFirstPart(slot);
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            Transform existingRoot = transform.Find("VisualAssemblyRoot");
            if (existingRoot != null)
            {
                visualRoot = existingRoot;
                return;
            }

            GameObject rootObject = new GameObject("VisualAssemblyRoot");
            visualRoot = rootObject.transform;
            visualRoot.SetParent(transform, false);
        }

        private static void AlignUpperBody(
            Transform upperRoot,
            Transform upperAnchor,
            Transform lowerMount)
        {
            Quaternion rotationDelta =
                lowerMount.rotation * Quaternion.Inverse(upperAnchor.rotation);
            upperRoot.rotation = rotationDelta * upperRoot.rotation;
            upperRoot.position += lowerMount.position - upperAnchor.position;
            upperRoot.SetParent(lowerMount, true);
        }

        private static Transform CreateUpperBodyAimPivot(
            Transform lowerMount,
            Transform upperRoot)
        {
            GameObject pivotObject = new GameObject("UpperBodyAimPivot");
            Transform pivot = pivotObject.transform;
            pivot.SetParent(lowerMount, false);
            upperRoot.SetParent(pivot, true);
            return pivot;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] descendants =
                root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                descendants[i].gameObject.layer = layer;
            }
        }

        private void ApplyResolvedStats()
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.ApplyFrameStats(
                    ResolvedStats.MaxHealth,
                    ResolvedStats.Defense);
            }

            PlayerEnergy energy = GetComponent<PlayerEnergy>();
            if (energy != null)
            {
                energy.ApplyFrameStats(
                    ResolvedStats.MaxEnergy,
                    ResolvedStats.EnergyRechargeRate);
            }

            PlayerNetworkController movement =
                GetComponent<PlayerNetworkController>();
            if (movement != null)
            {
                movement.ApplyFrameStats(ResolvedStats);
            }
        }

        private void ResolveAndApplyStats(
            FramePartSO upperDefinition,
            FramePartSO lowerDefinition)
        {
            ResolvedStats = new ResolvedFrameStats(
                upperDefinition.Stats,
                lowerDefinition.Stats,
                lowerDefinition.AlwaysFaceMovementDirection);
            ApplyResolvedStats();
        }

    }
}
