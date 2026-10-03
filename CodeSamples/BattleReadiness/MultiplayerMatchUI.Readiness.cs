using TMPro;
using RIP.Player.Input;
using RIP.PlayerCamera;
using RIP.UI;
using UnityEngine;

namespace RIP.Multiplayer
{
    // Selected members; other lifecycle, UI and helper members are omitted.
    public sealed class MultiplayerMatchUI : MonoBehaviour
    {

        [SerializeField] private MultiplayerMatchManager _matchManager;

        [SerializeField] private TMP_Text _timerText;

        private TMP_Text _overtimeText;

        private BattleIntroSignalOverlay _battleIntroSignalOverlay;

        private MultiplayerCombatPresenceHUD _combatPresenceHud;

        private bool _battleIntroGateActive;

        private bool _battleIntroCameraInitialized;

        private void UpdateBattleIntroState()
        {
            bool gateActive =
                _matchManager.IsBattleIntroPreparing &&
                !_matchManager.Started;
            if (gateActive)
            {
                _battleIntroGateActive = true;
                FindAnyObjectByType<PlayerInputProvider>()
                    ?.SetGameplayInputEnabled(false);
                PlayerCameraRig.Instance?.SetRotationInputEnabled(false);
                PlayerStatusUI.ActiveLocal?.SetGameplayHudVisible(false);
                _combatPresenceHud?.SetGameplayHudVisible(false);
                _timerText.gameObject.SetActive(false);
                _overtimeText.gameObject.SetActive(false);

                if (_matchManager.IsBattleIntroVisualPreparing)
                {
                    if (!_battleIntroCameraInitialized &&
                        PlayerCameraRig.Instance != null &&
                        PlayerCameraRig.Instance.HasPlayerBinding)
                    {
                        PlayerCameraRig.Instance.SetBattleIntroProgress(0f);
                        _battleIntroCameraInitialized = true;
                        return;
                    }

                    if (_battleIntroCameraInitialized)
                        _matchManager.NotifyLocalBattleIntroVisualReady();

                    _battleIntroSignalOverlay?.Hide();
                    return;
                }

                if (_matchManager.IsBattleIntroActive)
                {
                    float introProgress = _matchManager.BattleIntroProgress;
                    PlayerCameraRig.Instance?.SetBattleIntroProgress(introProgress);
                    _battleIntroSignalOverlay?.SetProgress(introProgress);

                    if (global::SceneLoadRequest.IsOverlayOnly)
                        global::SceneLoadRequest.HideOverlay();
                }
                else
                    _battleIntroSignalOverlay?.Hide();

                return;
            }

            if (!_battleIntroGateActive)
                return;

            _battleIntroGateActive = false;
            _battleIntroCameraInitialized = false;
            _battleIntroSignalOverlay?.Hide();
            PlayerCameraRig.Instance?.EndBattleIntro();
            PlayerCameraRig.Instance?.SetRotationInputEnabled(true);
            FindAnyObjectByType<PlayerInputProvider>()
                ?.SetGameplayInputEnabled(true);
            PlayerStatusUI.ActiveLocal?.SetGameplayHudVisible(true);
            _combatPresenceHud?.SetGameplayHudVisible(true);
            _timerText.gameObject.SetActive(true);
        }

    }
}
