using System;
using BigWalkArchipelago.Core.Net;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Debug: hosts Debug.AutoHostSave from the title menu as a player would.
    internal class AutoHost : MonoBehaviour
    {
        private const string Tag = "[" + nameof(AutoHost) + "]";
        private const float StepSeconds = 0.5f;
        private const float RetrySeconds = 3f;

        private float _stepAt;
        private float _confirmAt;
        private float _proceedAt;
        private bool _micDone;
        private bool _countChosen;
        private bool _hostAsked;
        private bool _slotPicked;
        private bool _done;
        private bool _missingLogged;

        public AutoHost(IntPtr ptr) : base(ptr)
        {
        }

        private void Update()
        {
            if (_done || Time.unscaledTime < _stepAt)
                return;
            _stepAt = Time.unscaledTime + StepSeconds;

            var save = ModConfig.AutoHostSave.Value?.Trim();
            if (string.IsNullOrEmpty(save))
            {
                _done = true;
                return;
            }

            try
            {
                if (WorldManager.isReadyForEffects)
                {
                    _done = true;
                    Plugin.Log.LogInfo($"{Tag} Hosting '{save}'.");
                    if (ModConfig.AutoHostGuest.Value && !LoopbackGuest.IsGuest)
                    {
                        Plugin.Log.LogInfo($"{Tag} Starting the loopback guest.");
                        LoopbackLauncher.LaunchGuest(withoutTheMod: false);
                    }
                    return;
                }

                var menus = MainMenuManager.instance;
                if (menus == null)
                    return;

                // The guest instance only passes the microphone check; it joins on its own.
                if (LoopbackGuest.IsGuest && _micDone)
                {
                    _done = true;
                    return;
                }

                // The microphone check at launch: continued as the player would.
                if (!_micDone && menus.micCheckMenu != null && menus.micCheckMenu.gameObject.activeInHierarchy)
                {
                    _micDone = true;
                    Plugin.Log.LogInfo($"{Tag} Microphone check: continue.");
                    menus.micCheckMenu.ActionContinue();
                    return;
                }

                if (LoopbackGuest.IsGuest)
                    return;

                if (!_hostAsked && menus.titleMenu != null && menus.titleMenu.gameObject.activeInHierarchy)
                {
                    _hostAsked = true;
                    Plugin.Log.LogInfo($"{Tag} Title menu: to the host menu.");
                    menus.titleMenu.GoToHostMenu();
                    return;
                }

                var select = menus.hostMenuSelect;
                if (!_slotPicked && select != null && select.gameObject.activeInHierarchy && select.cards != null && select.cards.Count > 0)
                {
                    foreach (var card in select.cards)
                    {
                        if (card == null || card.saveData == null || card.saveData.slotName != save)
                            continue;
                        _slotPicked = true;
                        Plugin.Log.LogInfo($"{Tag} The save '{save}': picked.");
                        card.PlaySlot();
                        return;
                    }
                    if (!_missingLogged)
                    {
                        _missingLogged = true;
                        Plugin.Log.LogWarning($"{Tag} No save called '{save}' on the host menu: create it once by hand.");
                    }
                    return;
                }

                var confirm = menus.hostMenuConfirm;
                if (confirm != null && confirm.gameObject.activeInHierarchy && Time.unscaledTime >= _confirmAt)
                {
                    _confirmAt = Time.unscaledTime + RetrySeconds;
                    Plugin.Log.LogInfo($"{Tag} Confirm screen: start.");
                    confirm.ActionStart();
                    return;
                }

                var count = menus.playerCountMenu;
                if (count != null && count.gameObject.activeInHierarchy && Time.unscaledTime >= _proceedAt)
                {
                    // Clicked as a player would: the menu's own setters did not pick a count.
                    if (!_countChosen && count.settingsRow != null)
                    {
                        _countChosen = true;
                        _proceedAt = Time.unscaledTime + 1f;
                        Plugin.Log.LogInfo($"{Tag} Player count: two.");
                        count.settingsRow.ActionSelect0();
                        return;
                    }
                    _proceedAt = Time.unscaledTime + RetrySeconds;
                    Plugin.Log.LogInfo($"{Tag} Player count: play.");
                    count.ActionProceed();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {ex.Message}");
            }
        }
    }
}
