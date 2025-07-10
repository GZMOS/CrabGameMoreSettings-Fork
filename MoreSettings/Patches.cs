using HarmonyLib;
using System.IO;
using System.Xml.Serialization;
using TMPro;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static MoreSettings.MoreSettings;

namespace MoreSettings
{
    internal static class Patches
    {
        //   Anti Bepinex detection (Thanks o7Moon: https://github.com/o7Moon/CrabGame.AntiAntiBepinex)
        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0))] // Ensures effectSeed is never set to 4200069 (if it is, modding has been detected)
        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.Method_Private_Void_0))] // Ensures connectedToSteam stays false (true means modding has been detected)
        // [HarmonyPatch(typeof(SnowSpeedModdingDetector), nameof(SnowSpeedModdingDetector.Method_Private_Void_0))] // Would ensure snowSpeed is never set to Vector3.zero (though it is immediately set back to Vector3.one due to an accident on Dani's part lol)
        [HarmonyPrefix]
        internal static bool PreBepinexDetection()
            => false;


        // Toggle sprint, alt crouch key, alt jump key, and hold to jump
        internal static bool sprintToggled = true;
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.SetInput))]
        [HarmonyPrefix]
        internal static void PrePlayerMovementSetInput(ref bool param_2, ref bool param_3, ref bool param_4)
        {
            if (!Instance.save.holdSprint)
            {
                if (Input.GetKeyDown((KeyCode)PlayerKeybinds.sprint))
                    sprintToggled = !sprintToggled;
                param_4 = sprintToggled;
            }

            if (PersistentPlayerData.frozen || PersistentPlayerData.hnsFrozen)
                return;
            
            if (CurrentSettings.holdCrouch)
                param_2 = param_2 || PlayerInput.CheckInput(Instance.save.alternativeCrouch);
            else if (PlayerInput.CheckInputDown(Instance.save.alternativeCrouch))
            {
                PlayerInput.Instance.crouching = !PlayerInput.Instance.crouching;
                param_2 = PlayerInput.Instance.crouching;
            }
            
            if (Instance.save.holdJump)
                param_3 = PlayerInput.CheckInput(PlayerKeybinds.jump) || PlayerInput.CheckInput(Instance.save.alternativeJump);
            else
                param_3 = param_3 || PlayerInput.CheckInputDown(Instance.save.alternativeJump);
        }

        // Hold to use items
        internal static bool checkingInteract = false;
        internal static bool checkingLeftClick = false;
        [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.NotFrozenInput))]
        [HarmonyPrefix]
        internal static void PrePlayerInputNotFrozenInput(PlayerInput __instance)
        {
            if (!__instance.playerMovement)
                return;

            checkingInteract = true;
            checkingLeftClick = true;
        }

        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), [typeof(KeyCode)])]
        [HarmonyPrefix]
        internal static bool PreInputGetKeyDown(ref bool __result, KeyCode key)
        {
            if (checkingInteract && key == (KeyCode)PlayerKeybinds.interact)
            {
                checkingInteract = false;
                if (Instance.save.holdInteract)
                {
                    __result = Input.GetKey((KeyCode)PlayerKeybinds.interact);
                    return false;
                }
                return true;
            }

            if (checkingLeftClick && key == (KeyCode)PlayerKeybinds.leftClick)
            {
                checkingLeftClick = false;
                if (Instance.save.holdAttack)
                {
                    __result = Input.GetKey((KeyCode)PlayerKeybinds.leftClick);
                    return false;
                }
                return true;
            }

            return true;
        }


        // Makes all items 'automatic' so that holding down PlayerKeybinds.leftClick works
        [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.Awake))]
        [HarmonyPostfix]
        internal static void PostItemManagerAwake()
        {
            foreach (ItemData itemData in ItemManager.idToItem.Values)
                if (itemData.gunComponent != null)
                    itemData.gunComponent.automatic = true;
        }
        
        // Add custom setting options to ui
        [HarmonyPatch(typeof(GeneralUiSettings), nameof(GeneralUiSettings.Start))]
        [HarmonyPostfix]
        internal static void PostGeneralUiSettingsStart(GeneralUiSettings __instance)
        {
            GeneralUiSettingsSlider guiTransparency = Instance.CreateSetting<GeneralUiSettingsSlider>(__instance.sens, 20, "GUI Transparency");
            guiTransparency.SetSettings(Instance.save.guiTransparency);
            guiTransparency.slider.minValue = 0;
            guiTransparency.slider.maxValue = 10;
            float x = guiTransparency.currentSetting / 10f;
            guiTransparency.value.text = x.ToString("0.##");
            Instance.guiTransparency = guiTransparency;  // Assign to Instance after initialization is complete
            
            Instance.alternativeJump = Instance.CreateSetting<GeneralUiSettingsKeyInput>(__instance.jump, 5, "Jump (Alternative)");
            Instance.alternativeJump.SetSetting(Instance.save.alternativeJump, "Jump (Alternative)");

            Instance.holdJump = Instance.CreateSetting<GeneralUiSettingsCheckbox>(__instance.holdCrouch, 6, "Hold to jump");
            Instance.holdJump.SetSetting(Instance.save.holdJump);

            Instance.holdSprint = Instance.CreateSetting<GeneralUiSettingsCheckbox>(__instance.holdCrouch, 9, "Hold to sprint");
            Instance.holdSprint.SetSetting(Instance.save.holdSprint);
            
            Instance.alternativeCrouch = Instance.CreateSetting<GeneralUiSettingsKeyInput>(__instance.crouch, 11, "Crouch / Slide (Alternative)");
            Instance.alternativeCrouch.SetSetting(Instance.save.alternativeCrouch, "Crouch / Slide (Alternative)");

            Instance.holdInteract = Instance.CreateSetting<GeneralUiSettingsCheckbox>(__instance.holdCrouch, 14, "Hold to interact");
            Instance.holdInteract.SetSetting(Instance.save.holdInteract);

            Instance.holdAttack = Instance.CreateSetting<GeneralUiSettingsCheckbox>(__instance.holdCrouch, 19, "Hold to attack");
            Instance.holdAttack.SetSetting(Instance.save.holdAttack);
            
            __instance.fov.slider.maxValue = 140;
            // __instance.fpsLimit.slider.maxValue = 10000;
        }
        
        // Listening for when settings are changed, and saves them
        [HarmonyPatch(typeof(GeneralUiSettingsKeyInput), nameof(GeneralUiSettingsKeyInput.SetKey))]
        [HarmonyPostfix]
        internal static void PostGeneralUiSettingsKeyInputSetKey(GeneralUiSettingsKeyInput __instance)
        {
            if (__instance == Instance.alternativeJump)
            {
                Instance.save.alternativeJump = __instance.currentKey;
                SaveManager.Instance.Save();
                return;
            }

            if (__instance == Instance.alternativeCrouch)
            {
                Instance.save.alternativeCrouch = __instance.currentKey;
                SaveManager.Instance.Save();
                return;
            }
        }
        
        [HarmonyPatch(typeof(GeneralUiSettingsSlider), nameof(GeneralUiSettingsSlider.UpdateSettings))]
        [HarmonyPostfix]
        internal static void PostGeneralUiSettingsSliderSetSettings(GeneralUiSettingsSlider __instance)
        {
            if (__instance == Instance.guiTransparency)
            {
                Instance.save.guiTransparency = __instance.currentSetting;
                SaveManager.Instance.Save();

                float alpha = __instance.currentSetting / 10f;
                __instance.value.text = alpha.ToString("0.##");
                UpdateGuiTransparencyUI(alpha);

            }
        }
        
        [HarmonyPatch(typeof(GeneralUiSettingsCheckbox), nameof(GeneralUiSettingsCheckbox.ToggleSetting))]
        [HarmonyPostfix]
        internal static void PostGeneralUiSettingsCheckboxToggleSetting(GeneralUiSettingsCheckbox __instance)
        {
            if (__instance == Instance.holdJump)
            {
                Instance.save.holdJump = __instance.currentSetting == 1;
                SaveManager.Instance.Save();
                return;
            }
            
            if (__instance == Instance.holdSprint)
            {
                Instance.save.holdSprint = __instance.currentSetting == 1;
                SaveManager.Instance.Save();
                return;
            }

            if (__instance == Instance.holdInteract)
            {
                Instance.save.holdInteract = __instance.currentSetting == 1;
                SaveManager.Instance.Save();
                return;
            }

            if (__instance == Instance.holdAttack)
            {
                Instance.save.holdAttack = __instance.currentSetting == 1;
                SaveManager.Instance.Save();
                return;
            }
        }

        // Manage the save for MoreSettings
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Load))]
        [HarmonyPrefix]
        internal static void PreSaveManagerLoad()
        {
            if (PlayerPrefs.HasKey("moreSettingsSave"))
            {
                XmlSerializer xmlSerializer = new(typeof(MoreSettingsSave));
                StringReader stringReader = new(PlayerPrefs.GetString("moreSettingsSave"));
                Instance.save = (MoreSettingsSave)xmlSerializer.Deserialize(stringReader);
            }

            Instance.save ??= new();
        }
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.NewSave))]
        [HarmonyPrefix]
        internal static void PreSaveManagerNewSave()
            => Instance.save = new();
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save))]
        [HarmonyPrefix]
        internal static void PreSaveManagerSave()
        {
            XmlSerializer xmlSerializer = new(typeof(MoreSettingsSave));
            StringWriter stringWriter = new();
            xmlSerializer.Serialize(stringWriter, Instance.save);
            PlayerPrefs.SetString("moreSettingsSave", stringWriter.ToString());
        }


        // Who's talking? Player list
        [HarmonyPatch(typeof(PlayerList), nameof(PlayerList.Awake))]
        [HarmonyPostfix]
        internal static void PostPlayerListAwake()
        {
            Instance.playerListPlayers.Clear();
            Instance.playerListMicImages.Clear();
            Instance.playerJawMovements.Clear();
        }

        [HarmonyPatch(typeof(PlayerListPlayer), nameof(PlayerListPlayer.SetPlayer))]
        [HarmonyPostfix]
        internal static void PostPlayerListPlayerSetPlayer(PlayerListPlayer __instance, Player param_1)
        {
            // Check if the mic hasn't already been created
            if (!Instance.playerListPlayers.ContainsKey(param_1.steamProfile.m_SteamID) || Instance.playerListPlayers[param_1.steamProfile.m_SteamID] != __instance || Instance.playerListMicImages[param_1.steamProfile.m_SteamID] == null)
            {
                Instance.playerListPlayers[param_1.steamProfile.m_SteamID] = __instance;

                // Make room for the mic in the ui
                __instance.ping.GetComponent<RectTransform>().sizeDelta -= new Vector2(35/*width (25) + spacing (10)*/, 0);

                // Create the mic icon
                GameObject micObject = Object.Instantiate(__instance.icon.gameObject, __instance.transform);
                micObject.name = "Talking";
                micObject.transform.SetSiblingIndex(4);

                Instance.playerListMicImages[param_1.steamProfile.m_SteamID] = micObject.GetComponent<RawImage>();
                Instance.playerListMicImages[param_1.steamProfile.m_SteamID].texture = GameUiStatus.Instance.hpCircle.transform.parent.GetChild(2).GetChild(0).GetComponent<RawImage>().texture;
            }

            // Show the mic image if the player is talking
            Instance.playerListMicImages[param_1.steamProfile.m_SteamID].enabled = Instance.PlayerIsTalking(param_1.steamProfile.m_SteamID);
        }

        [HarmonyPatch(typeof(OnlinePlayerDissonanceJawMovement), nameof(OnlinePlayerDissonanceJawMovement.SlowUpdate))]
        [HarmonyPostfix]
        internal static void PostOnlinePlayerDissonanceJawMovementSlowUpdate(OnlinePlayerDissonanceJawMovement __instance)
        {
            if (__instance.pm == null || __instance.pm.steamProfile.m_SteamID == default || !Instance.playerListMicImages.ContainsKey(__instance.pm.steamProfile.m_SteamID) || Instance.playerListMicImages[__instance.pm.steamProfile.m_SteamID] == null)
                return;

            Instance.playerJawMovements[__instance.pm.steamProfile.m_SteamID] = __instance;
            Instance.playerListMicImages[__instance.pm.steamProfile.m_SteamID].enabled = Instance.PlayerIsTalking(__instance.pm.steamProfile.m_SteamID);
        }


        // View Steam Profile in Player List
        [HarmonyPatch(typeof(PlayerListManagePlayer), nameof(PlayerListManagePlayer.Awake))]
        [HarmonyPostfix]
        internal static void PostPlayerListManagePlayerAwake(PlayerListManagePlayer __instance)
        {
            // Increase the height of the background to fit the extra button (button height(32) + spacing(5) = 37)
            Transform tr = __instance.transform;
            RectTransform rectTr = tr.GetChild(0).GetComponent<RectTransform>();
            rectTr.sizeDelta += new Vector2(0f, 37f);

            // Create viewBtn from muteBtn
            Transform muteBtn = tr.GetChild(0).GetChild(1).GetChild(3);
            GameObject viewBtn = Object.Instantiate(muteBtn.gameObject, muteBtn.parent);

            // Change viewBtn visuals
            viewBtn.GetComponent<Graphic>().color = new Color(0.25f, 0.25f, 0.75f);
            viewBtn.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = "Profile";

            // Set viewBtn's onClick event
            UnityEvent ev = viewBtn.GetComponent<Button>().onClick;
            ev.m_PersistentCalls.Clear();
            ev.AddListener(viewBtn.AddComponent<ModListViewSteamProfileButton>(), UnityEventBase.GetValidMethodInfo(Il2CppType.Of<ModListViewSteamProfileButton>(), "Clicked", new Il2CppReferenceArray<Il2CppSystem.Type>(0)));
        }
        
        // Ui color shit
        
        internal static void UpdateGuiTransparencyUI(float alpha)
        {
            void AddComponent(string path) =>
                _uiComponents[path] = GameObject.Find(path).GetComponent<RawImage>();

            AddComponent("GameUI/PlayerList/WindowUI");
            AddComponent("GameUI/Pause/Overlay/MapSelection/GameSettingsWindow");
            AddComponent("GameUI/Pause/Overlay/Menu/BtnsPanel/Invite");
            AddComponent("GameUI/Status/ReadyStatus/Tab1");
            // Removed GUI stuff because players couldn't read text or see items clearly
            // AddComponent("GameUI/Pause/Overlay/Inventory/GameSettingsWindow");
            // AddComponent("GameUI/Pause/Overlay/Settings");

            foreach (var component in _uiComponents.Values)
            {
                var color = component.color;
                color.a = alpha;
                component.color = color;
            }
        }

        [HarmonyPatch(typeof(GameUi), nameof(GameUi.Start))]
        [HarmonyPostfix]
        internal static void PostGameUiStart(GameUi __instance)
        {
            float alpha = Instance.save.guiTransparency/ 10f;
            UpdateGuiTransparencyUI(alpha);
        }
        

    }
}