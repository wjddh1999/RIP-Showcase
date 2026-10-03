using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;
using RIP.Weapons.Equip;

namespace RIP.Services
{
    public enum UgsPlayerState
    {
        Uninitialized,
        Initializing,
        SignedOut,
        SigningIn,
        SignedIn,
        Error
    }

    // Selected members; other lifecycle, UI and helper members are omitted.
    public sealed class UgsPlayerService : MonoBehaviour
    {

        public const string RankedLeaderboardId = "ranked-elo";

        public const int DefaultElo = 1000;

        private const string LoadoutCloudSaveKey = "player-loadout-v1";

        private readonly SemaphoreSlim _loadoutSaveGate = new(1, 1);

        private int _lifecycleEpoch;

        private bool _isSigningIn;

        private bool _isSigningOut;

        private bool _isGuest;

        private float _lastUserActivityAt;

        private bool _inactivityLogoutInProgress;

        private LocalPlayerLoadoutHolder _loadoutHolder;

        public event Action StateChanged;

        public event Action ProfileChanged;

        public UgsPlayerState State { get; private set; } =
            UgsPlayerState.Uninitialized;
        public PlayerProfile CurrentProfile { get; private set; }
        public string StatusMessage { get; private set; } =
            "UGS is not initialized.";
        public bool IsSignedIn =>
            State == UgsPlayerState.SignedIn &&
            !_isSigningOut &&
            CurrentProfile != null &&
            CurrentProfile.IsValid;
        public bool IsGuest => IsSignedIn && _isGuest;

        private async Task<bool> SignInWithGoogleLockedAsync()
        {
            _isSigningIn = true;
            int lifecycleEpoch = ++_lifecycleEpoch;

            SetState(
                UgsPlayerState.SigningIn,
                AuthenticationService.Instance.IsSignedIn
                    ? "Completing sign-in..."
                    : "Complete Google sign-in in the secure browser.");

            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await EnsurePlayerAccountSignedInAsync();
                    if (!IsLifecycleCurrent(lifecycleEpoch))
                        return false;

                    string accessToken = PlayerAccountService.Instance.AccessToken;
                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        throw new InvalidOperationException(
                            "Unity Player Accounts did not return an access token.");
                    }

                    await AuthenticationService.Instance.SignInWithUnityAsync(
                        accessToken);
                    if (!IsLifecycleCurrent(lifecycleEpoch))
                        return false;
                }

                _isGuest = false;
                return await CompleteAuthenticatedSignInAsync(lifecycleEpoch);
            }
            catch (Exception exception)
            {
                if (IsLifecycleCurrent(lifecycleEpoch))
                {
                    SignOutAuthentication(signOutPlayerAccount: false);
                    ClearProfile();
                    SetError("Google sign-in failed", exception);
                }

                return false;
            }
            finally
            {
                if (IsLifecycleCurrent(lifecycleEpoch))
                    _isSigningIn = false;
            }
        }

        private static async Task EnsurePlayerAccountSignedInAsync()
        {
            IPlayerAccountService playerAccounts =
                PlayerAccountService.Instance;
            if (playerAccounts.IsSignedIn)
                return;

            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void HandleSignedIn()
            {
                completion.TrySetResult(true);
            }

            void HandleSignInFailed(RequestFailedException exception)
            {
                completion.TrySetException(exception);
            }

            playerAccounts.SignedIn += HandleSignedIn;
            playerAccounts.SignInFailed += HandleSignInFailed;

            try
            {
                await playerAccounts.StartSignInAsync();

                if (playerAccounts.IsSignedIn)
                    completion.TrySetResult(true);

                await completion.Task;
            }
            finally
            {
                playerAccounts.SignedIn -= HandleSignedIn;
                playerAccounts.SignInFailed -= HandleSignInFailed;
            }
        }

        private async Task<bool> CompleteAuthenticatedSignInAsync(
            int lifecycleEpoch)
        {
            if (!IsLifecycleCurrent(lifecycleEpoch) ||
                !AuthenticationService.Instance.IsSignedIn)
            {
                return false;
            }

            string playerId = AuthenticationService.Instance.PlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            PlayerProfile profile = await LoadProfileAsync(
                playerId,
                lifecycleEpoch);
            if (!IsAuthenticatedPlayerCurrent(lifecycleEpoch, playerId))
                return false;

            await LoadOrCreateCloudLoadoutAsync(lifecycleEpoch, playerId);
            if (!IsAuthenticatedPlayerCurrent(lifecycleEpoch, playerId))
                return false;

            CurrentProfile = profile;
            _lastUserActivityAt = Time.realtimeSinceStartup;
            _inactivityLogoutInProgress = false;
            ProfileChanged?.Invoke();
            SetState(
                UgsPlayerState.SignedIn,
                $"Signed in as {profile.Nickname}.");
            return true;
        }

        private bool IsLifecycleCurrent(int lifecycleEpoch)
        {
            return _lifecycleEpoch == lifecycleEpoch && !_isSigningOut;
        }

        private bool IsAuthenticatedPlayerCurrent(
            int lifecycleEpoch,
            string playerId)
        {
            if (!IsLifecycleCurrent(lifecycleEpoch) ||
                UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                return false;
            }

            IAuthenticationService authentication =
                AuthenticationService.Instance;
            return authentication.IsSignedIn &&
                   string.Equals(
                       authentication.PlayerId,
                       playerId,
                       StringComparison.Ordinal);
        }

        private void EnsureAuthenticatedPlayerCurrent(
            int lifecycleEpoch,
            string playerId)
        {
            if (!IsAuthenticatedPlayerCurrent(lifecycleEpoch, playerId))
            {
                throw new OperationCanceledException(
                    "The authenticated player changed.");
            }
        }

        private async Task LoadOrCreateCloudLoadoutAsync(
            int lifecycleEpoch,
            string playerId)
        {
            PlayerLoadoutData loadout = await LoadCloudLoadoutAsync();
            EnsureAuthenticatedPlayerCurrent(lifecycleEpoch, playerId);

            if (loadout.version != PlayerLoadoutData.CurrentVersion)
            {
                loadout = LocalPlayerLoadoutHolder.CreateInitialData();
                await SaveCloudLoadoutAsync(loadout);
                EnsureAuthenticatedPlayerCurrent(lifecycleEpoch, playerId);
            }

            LocalPlayerLoadoutHolder holder =
                LocalPlayerLoadoutHolder.GetOrCreate();
            holder.ApplyData(loadout);
            SubscribeToLoadoutChanges(holder);
        }

        private static async Task<PlayerLoadoutData> LoadCloudLoadoutAsync()
        {
            try
            {
                var values = await CloudSaveService.Instance.Data.Player
                    .LoadAsync(new HashSet<string> { LoadoutCloudSaveKey });
                if (!values.TryGetValue(LoadoutCloudSaveKey, out var item))
                    return default;

                return JsonUtility.FromJson<PlayerLoadoutData>(
                    item.Value.GetAs<string>());
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[UGS] Loadout could not be loaded: {exception.Message}");
                return default;
            }
        }

        private static async Task SaveCloudLoadoutAsync(PlayerLoadoutData loadout)
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(
                    new Dictionary<string, object>
                    {
                        {
                            LoadoutCloudSaveKey,
                            JsonUtility.ToJson(loadout)
                        }
                    });
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[UGS] Loadout could not be saved: {exception.Message}");
            }
        }

        private async Task SaveCurrentLoadoutAsync()
        {
            if (!IsSignedIn || IsGuest || _loadoutHolder == null)
                return;

            await _loadoutSaveGate.WaitAsync();
            try
            {
                if (!IsSignedIn || IsGuest || _loadoutHolder == null)
                    return;

                await SaveCloudLoadoutAsync(_loadoutHolder.CurrentData);
            }
            finally
            {
                _loadoutSaveGate.Release();
            }
        }

        private static async Task<int> LoadOrCreateEloAsync()
        {
            try
            {
                var entry = await LeaderboardsService.Instance
                    .GetPlayerScoreAsync(RankedLeaderboardId);
                return Mathf.RoundToInt((float)entry.Score);
            }
            catch (LeaderboardsException exception) when (
                exception.Reason == LeaderboardsExceptionReason.EntryNotFound ||
                exception.Reason == LeaderboardsExceptionReason.ScoreSubmissionRequired)
            {
                try
                {
                    var entry = await LeaderboardsService.Instance
                        .AddPlayerScoreAsync(RankedLeaderboardId, DefaultElo);
                    int elo = Mathf.RoundToInt((float)entry.Score);
                    return elo;
                }
                catch (Exception createException)
                {
                    Debug.LogWarning(
                        $"[UGS] Initial ELO entry could not be created: {createException.Message}");
                    return DefaultElo;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[UGS] ELO could not be loaded: {exception.Message}");
                return DefaultElo;
            }
        }

        private void SetState(UgsPlayerState state, string message)
        {
            State = state;
            StatusMessage = message;
            StateChanged?.Invoke();
        }

        private void SetError(string prefix, Exception exception)
        {
            Debug.LogException(exception);
            SetState(
                UgsPlayerState.Error,
                $"{prefix}: {exception.Message}");
        }

    }
}
