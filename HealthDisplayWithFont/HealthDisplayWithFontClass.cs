using HarmonyLib;
using HealthDisplayWithFont;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Test with clones
// TODO: make remote text scale based on distance from camera
// TODO: dont block player nametag.

#region Assemblies
[assembly: MelonInfo(typeof(HealthDisplayWithFontClass), HealthDisplayWithFontModInfo.ModName, HealthDisplayWithFontModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(HealthDisplayWithFontModInfo.MLVersion, true)]
#endregion

namespace HealthDisplayWithFont
{
    #region HealthDisplayWithFontModInfo
    /// <summary>
    /// Contains mod info.
    /// </summary>
    public static class HealthDisplayWithFontModInfo
    {
        /// <summary>
        /// Mod name.
        /// </summary>
        public const string ModName = "HealthDisplayWithFont";
        /// <summary>
        /// Mod version.
        /// </summary>
        public const string ModVer = "0.3.1";
        /// <summary>
        /// Melonloader Version.
        /// </summary>
        public const string MLVersion = "0.7.0";
    }
    #endregion

    /// <summary>
    /// Adds text above all player healthbars with their health.
    /// </summary>
    public class HealthDisplayWithFontClass : MelonMod
    {
        #region Healthbar stuff
        private static void AddHealthbarText(Transform UIBAR, Player player, ControllerType controllerType)
        {
            if (UIBAR == null) return;
            GameObject healthBar = new("HealthText");
            healthBar.transform.SetParent(UIBAR, false);

            TextMeshPro textRef = healthBar.AddComponent<TextMeshPro>();
            if (controllerType == ControllerType.Local)
            {
                healthBar.transform.localPosition = new Vector3(-1.01f, 0.01f, 0f);
                healthBar.transform.localRotation = Quaternion.Euler(63f, 270f, 0f);
                healthBar.transform.localScale = new Vector3(0.015f, 0.007f, 0.015f);
            }
            else
            {
                healthBar.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                healthBar.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                healthBar.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
            }

            textRef.text = player.Data.HealthPoints.ToString();
            textRef.alignment = TextAlignmentOptions.Center;

            if (GetFont != null)
                textRef.font = GetFont();
        }

        private static Transform GetHealthbar(Transform UI, ControllerType? controllerType)
        {
            if (controllerType == null) return null;
            if (controllerType == ControllerType.Local)
            {
                Transform healthbar = UI.GetChild(0)?.GetChild(1);
                if (healthbar?.name != "Local UI Bar" || healthbar?.gameObject?.active != true)
                {
                    MelonLogger.Warning("Could not get Local Healthbar via GetChild");
                    healthbar = UI.Find("LocalUI/Local UI Bar");
                    if (healthbar == null || healthbar.gameObject?.active != true)
                    {
                        MelonLogger.Error("Could not get Local Healthbar via Find");
                        return null;
                    }
                }
                return healthbar;
            }
            else if (controllerType == ControllerType.Remote)
            {
                Transform healthbar = UI.GetChild(1);
                if (healthbar?.name != "RemoteUI" || healthbar?.gameObject?.active != true)
                {
                    MelonLogger.Warning("Could not get RemoteUI via GetChild");
                    healthbar = UI.Find("RemoteUI");
                    if (healthbar == null || healthbar.gameObject?.active != true)
                    {
                        MelonLogger.Error("Could not get RemoteUI via Find");
                        return null;
                    }
                }
                return healthbar;
            }
            else {
                MelonLogger.Warning($"Unknown controller type: {controllerType}");
                return null;
            }
        }
        #endregion

        #region Patches
        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Initialize))]
        class PlayerHealthInitPatch
        {
            static void Postfix(PlayerHealth __instance, PlayerController controller)
            {
                AddHealthbarText(GetHealthbar(__instance.transform, controller.controllerType), controller.assignedPlayer, controller.controllerType);
            }
        }

        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SetHealthBarPercentage))]
        class SetHealthBarPercentagePatch
        {
            static void Postfix(PlayerHealth __instance, float currentHealth)
            {
                TextMeshPro textMaybeNull = GetHealthbar(__instance.transform, __instance.parentController?.controllerType)?.Find("HealthText")?.GetComponent<TextMeshPro>();
                if (textMaybeNull != null) {
                    textMaybeNull.text = currentHealth.ToString();
                }
            }
        }
        #endregion

        #region Fontifier
        private static Func<TMP_FontAsset> GetFont;
        private static Func<string, TMP_FontAsset> FontFromName;

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            if (RegisteredMelons.FirstOrDefault(m => m.Info.Name == "Fontifier")?.GetType() is Type fontifierType && fontifierType != null) (GetFont, FontFromName) = ((Func<TMP_FontAsset>, Func<string, TMP_FontAsset>))fontifierType.GetMethod("RegisterMod", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { this.Info.Name, new EventHandler<EventArgs>(FontChanged) });
        }

        private static void FontChanged(object sender, EventArgs args)
        {
            TMP_FontAsset font = FontFromName(((dynamic)args).Value);
            foreach (Player player in PlayerManager.instance.AllPlayers)
            {
                Transform UI = player.Controller?.transform?.Find("UI");
                if (UI != null)
                {
                    GetHealthbar(UI, player.Controller.controllerType).Find("HealthText").GetComponent<TextMeshPro>().font = font;
                }
            }
        }
        #endregion
    }
}
