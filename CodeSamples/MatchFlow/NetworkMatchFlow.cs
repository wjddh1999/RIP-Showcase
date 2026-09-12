using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RIP.Multiplayer
{
    internal sealed class NetworkMatchFlow
    {
        private readonly string _playerSceneName;
        private readonly string _defaultMapSceneName;
        private string _mapSceneName;
        private bool _playerSceneLoaded;

        public NetworkMatchFlow(
            string playerSceneName,
            string mapSceneName)
        {
            _playerSceneName = playerSceneName;
            _defaultMapSceneName = mapSceneName;
            _mapSceneName = mapSceneName;
        }

        public bool BattleStarted { get; private set; }

        public void SetMapScene(string mapSceneName)
        {
            _mapSceneName = string.IsNullOrWhiteSpace(mapSceneName)
                ? _defaultMapSceneName
                : mapSceneName;
        }

        public bool CanStartBattle(NetworkRunner runner)
        {
            return runner != null &&
                runner.IsRunning &&
                runner.SessionInfo.IsValid &&
                runner.IsServer &&
                runner.SessionInfo.PlayerCount >= GetMinimumPlayers(runner);
        }

        private static int GetMinimumPlayers(NetworkRunner runner)
        {
            int maximumPlayers = Mathf.Max(2, runner.SessionInfo.MaxPlayers);
            return maximumPlayers > 2 ? 3 : 2;
        }

        public void PrepareMultiplayerBattle(NetworkRunner runner)
        {
            BattleStarted = true;
            runner.SessionInfo.IsOpen = false;
            runner.SessionInfo.IsVisible = false;
        }

        public void LoadBattleScene(NetworkRunner runner)
        {
            LoadNetworkScene(runner, _playerSceneName, LoadSceneMode.Single);
        }

        public void LoadSoloMap(NetworkRunner runner)
        {
            BattleStarted = true;
            _playerSceneLoaded = true;
            LoadMapScene(runner);
        }

        public bool HandleSceneLoadDone(NetworkRunner runner)
        {
            MapSceneContext mapContext =
                Object.FindAnyObjectByType<MapSceneContext>(
                    FindObjectsInactive.Include);

            if (mapContext != null && mapContext.gameObject.scene.isLoaded)
            {
                SceneManager.SetActiveScene(mapContext.gameObject.scene);
                BattleStarted = true;
                return true;
            }

            if (!_playerSceneLoaded &&
                SceneManager.GetSceneByName(_playerSceneName).isLoaded)
            {
                BattleStarted = true;
                _playerSceneLoaded = true;

                if (runner.IsServer)
                    LoadMapScene(runner);

                return false;
            }

            return false;
        }

        private void LoadMapScene(NetworkRunner runner)
        {
            global::SceneLoadRequest.RequestOverlayShaderWarmup(_mapSceneName);
            LoadNetworkScene(runner, _mapSceneName, LoadSceneMode.Additive);
        }

        private static void LoadNetworkScene(
            NetworkRunner runner,
            string sceneName,
            LoadSceneMode mode)
        {
            if (runner == null || string.IsNullOrWhiteSpace(sceneName))
                return;

            NetworkSceneManagerDefault sceneManager =
                runner.GetComponent<NetworkSceneManagerDefault>();

            runner.LoadScene(
                sceneManager.GetSceneRef(sceneName),
                mode);
        }

        public void SpawnPlayerIfBattleStarted(
            NetworkRunner runner,
            PlayerSpawner spawner,
            PlayerRef player)
        {
            if (!BattleStarted || !runner.IsServer)
                return;

            MultiplayerMatchManager matchManager =
                Object.FindAnyObjectByType<MultiplayerMatchManager>();
            if (matchManager != null &&
                matchManager.TryGetPlayerLoadout(player, out var loadout))
            {
                spawner.Spawn(runner, player, loadout);
            }
        }

        public void SpawnActivePlayers(
            NetworkRunner runner,
            PlayerSpawner spawner)
        {
            if (runner == null || !runner.IsServer)
                return;

            MultiplayerMatchManager matchManager =
                Object.FindAnyObjectByType<MultiplayerMatchManager>();
            if (matchManager == null)
                return;

            foreach (PlayerRef player in runner.ActivePlayers)
            {
                if (matchManager.TryGetPlayerLoadout(
                        player,
                        out var loadout))
                {
                    spawner.Spawn(runner, player, loadout);
                }
            }
        }

        public void Reset()
        {
            BattleStarted = false;
            _playerSceneLoaded = false;
            _mapSceneName = _defaultMapSceneName;
        }
    }
}
