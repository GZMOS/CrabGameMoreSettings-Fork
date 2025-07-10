using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using SteamworksNative;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;

namespace MoreSettings
{
    [BepInPlugin($"lammas123.{MyPluginInfo.PLUGIN_NAME}", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class MoreSettings : BasePlugin
    {
        internal static MoreSettings Instance;

        internal MoreSettingsSave save;

        internal GeneralUiSettingsSlider guiTransparency;
        internal GeneralUiSettingsKeyInput alternativeJump;
        internal GeneralUiSettingsCheckbox holdJump;
        internal GeneralUiSettingsCheckbox holdSprint;
        internal GeneralUiSettingsKeyInput alternativeCrouch;
        internal GeneralUiSettingsCheckbox holdInteract;
        internal GeneralUiSettingsCheckbox holdAttack;

        internal Dictionary<ulong, PlayerListPlayer> playerListPlayers = [];
        internal Dictionary<ulong, RawImage> playerListMicImages = [];
        internal Dictionary<ulong, OnlinePlayerDissonanceJawMovement> playerJawMovements = [];
        
        public static readonly Dictionary<string, RawImage> _uiComponents = new();

        public override void Load()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Instance = this;

            ClassInjector.RegisterTypeInIl2Cpp<ModListViewSteamProfileButton>();

            Harmony.CreateAndPatchAll(typeof(Patches));
            Log.LogInfo($"Loaded [{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}]");
        }

        internal T CreateSetting<T>(GeneralUiSettingsOnClickHandler baseSetting, int siblingIndex, string settingName) where T : GeneralUiSettingsOnClickHandler
        {
            GameObject secondJumpGameObject = UnityEngine.Object.Instantiate(baseSetting.gameObject, baseSetting.transform.parent);
            secondJumpGameObject.transform.SetSiblingIndex(siblingIndex);
            secondJumpGameObject.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = settingName;
            T setting = secondJumpGameObject.GetComponent<T>();
            setting.m_OnClick = new();
            return setting;
        }

        internal bool PlayerIsTalking(ulong clientId)
            => playerJawMovements.ContainsKey(clientId) && playerJawMovements[clientId].field_Private_Boolean_0 && !playerJawMovements[clientId].pm.dead;
    }

    [Serializable]
    public class MoreSettingsSave
    {
        public int guiTransparency = 5;
        public int alternativeJump = (int)KeyCode.Space;
        public bool holdJump = false;
        public bool holdSprint = true;
        public int alternativeCrouch = (int)KeyCode.C;
        public bool holdInteract = false;
        public bool holdAttack = false;
    }

    public class ModListViewSteamProfileButton : MonoBehaviour
    {
        public void Clicked()
            => SteamFriends.ActivateGameOverlayToUser("steamid", new(PlayerListManagePlayer.Instance.field_Private_UInt64_0));
    }
}