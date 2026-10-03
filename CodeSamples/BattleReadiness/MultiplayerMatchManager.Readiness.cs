using System.Collections.Generic;
using Fusion;
using RIP.Player;
using RIP.Player.Health;
using RIP.PlayerCamera;
using UnityEngine;

namespace RIP.Multiplayer
{
    // Readiness members; scene registration and match-result handling are omitted.
    public sealed class MultiplayerMatchManager : NetworkBehaviour
    {

        private const int MaximumParticipants = 4;

        [SerializeField, Min(1f)] private float _matchDurationSeconds = 180f;

        [SerializeField, Min(0.1f)] private float _battleIntroSeconds = 2.5f;

        [Networked] public bool Finished { get; private set; }

        [Networked] public bool Started { get; private set; }

        [Networked] public bool IsBattleIntroPreparing { get; private set; }

        [Networked] public bool IsBattleIntroVisualPreparing { get; private set; }

        [Networked] public bool IsBattleIntroActive { get; private set; }

        [Networked] public TickTimer BattleIntroTimer { get; private set; }

        [Networked] public TickTimer MatchTimer { get; private set; }

        [Networked] public int ParticipantCount { get; private set; }

        private readonly Dictionary<PlayerRef, PlayerHealth> _players = new();

        private readonly HashSet<PlayerRef> _battleIntroReadyPlayers = new();

        private readonly HashSet<PlayerRef> _battleIntroVisualReadyPlayers = new();

        private bool _reportedBattleIntroReady;

        private bool _reportedBattleIntroVisualReady;

        public bool IsFreeForAll => Runner != null && Runner.SessionInfo.MaxPlayers > 2;

        private int MinimumPlayerCount =>
            Runner != null && Runner.GameMode == GameMode.Single
                ? 1
                : IsFreeForAll ? 3 : 2;

        public float BattleIntroProgress
        {
            get
            {
                if (!IsBattleIntroActive || Runner == null)
                    return 0f;

                float remainingSeconds =
                    BattleIntroTimer.RemainingTime(Runner) ?? _battleIntroSeconds;
                return 1f - Mathf.Clamp01(remainingSeconds / _battleIntroSeconds);
            }
        }

        public void BeginBattleIntroPreparation()
        {
            if (!HasStateAuthority || Finished || Started)
                return;

            IsBattleIntroPreparing = true;
            IsBattleIntroVisualPreparing = false;
            IsBattleIntroActive = false;
            BattleIntroTimer = TickTimer.None;
            _battleIntroReadyPlayers.Clear();
            _battleIntroVisualReadyPlayers.Clear();
            _reportedBattleIntroReady = false;
            _reportedBattleIntroVisualReady = false;

            foreach (PlayerHealth player in _players.Values)
                player?.DisableDamage();
        }

        public override void Render()
        {
            if (!IsBattleIntroPreparing ||
                IsBattleIntroActive ||
                Started ||
                _reportedBattleIntroReady ||
                Runner == null ||
                LocalPlayerContext.Current?.Health == null ||
                PlayerCameraRig.Instance == null ||
                !PlayerCameraRig.Instance.HasPlayerBinding)
            {
                return;
            }

            _reportedBattleIntroReady = true;
            if (HasStateAuthority)
                RegisterBattleIntroReady(Runner.LocalPlayer);
            else
                RpcNotifyBattleIntroReady();
        }

        public void NotifyLocalBattleIntroVisualReady()
        {
            if (_reportedBattleIntroVisualReady ||
                !IsBattleIntroVisualPreparing ||
                IsBattleIntroActive ||
                Started ||
                Runner == null)
            {
                return;
            }

            _reportedBattleIntroVisualReady = true;
            if (HasStateAuthority)
                RegisterBattleIntroVisualReady(Runner.LocalPlayer);
            else
                RpcNotifyBattleIntroVisualReady();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RpcNotifyBattleIntroReady(RpcInfo info = default)
        {
            RegisterBattleIntroReady(info.Source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RpcNotifyBattleIntroVisualReady(RpcInfo info = default)
        {
            RegisterBattleIntroVisualReady(info.Source);
        }

        private void RegisterBattleIntroReady(PlayerRef player)
        {
            if (!HasStateAuthority ||
                !IsBattleIntroPreparing ||
                IsBattleIntroActive ||
                Started ||
                player == PlayerRef.None)
            {
                return;
            }

            _battleIntroReadyPlayers.Add(player);
            TryStartBattleIntro();
        }

        private void RegisterBattleIntroVisualReady(PlayerRef player)
        {
            if (!HasStateAuthority ||
                !IsBattleIntroVisualPreparing ||
                IsBattleIntroActive ||
                Started ||
                player == PlayerRef.None)
            {
                return;
            }

            _battleIntroVisualReadyPlayers.Add(player);
            TryStartBattleIntroPlayback();
        }

        private void TryStartBattleIntro()
        {
            if (!HasStateAuthority ||
                !IsBattleIntroPreparing ||
                IsBattleIntroVisualPreparing ||
                IsBattleIntroActive ||
                Started ||
                Runner == null)
            {
                return;
            }

            int activePlayerCount = 0;
            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                activePlayerCount++;

                if (!_players.ContainsKey(player) ||
                    !_battleIntroReadyPlayers.Contains(player))
                {
                    return;
                }
            }

            if (activePlayerCount < MinimumPlayerCount)
                return;

            ParticipantCount = Mathf.Min(_players.Count, MaximumParticipants);
            IsBattleIntroVisualPreparing = true;
        }

        private void TryStartBattleIntroPlayback()
        {
            if (!HasStateAuthority ||
                !IsBattleIntroVisualPreparing ||
                IsBattleIntroActive ||
                Started ||
                Runner == null)
            {
                return;
            }

            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                if (!_battleIntroVisualReadyPlayers.Contains(player))
                    return;
            }

            IsBattleIntroVisualPreparing = false;
            IsBattleIntroActive = true;
            BattleIntroTimer = TickTimer.CreateFromSeconds(
                Runner,
                _battleIntroSeconds);
        }

        private void StartMatch()
        {
            if (!HasStateAuthority || Started || Runner == null)
                return;

            IsBattleIntroVisualPreparing = false;
            IsBattleIntroActive = false;
            Started = true;
            MatchTimer = TickTimer.CreateFromSeconds(Runner, _matchDurationSeconds);
            foreach (PlayerHealth player in _players.Values)
                player?.EnableDamage();
        }

    }
}
