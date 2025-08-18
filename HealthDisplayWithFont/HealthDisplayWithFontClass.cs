using HarmonyLib;
using HealthDisplayWithFont;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Test with clones and park
// TODO: make remote text scale based on distance from camera
// TODO: change color based on hp

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
        private static void AddHealthbarText(Transform UIBAR, PlayerController controller)
        {
            if (UIBAR == null) return;
            GameObject healthtext = new("HealthText");
            healthtext.transform.SetParent(UIBAR, false);

            TextMeshPro textRef = healthtext.AddComponent<TextMeshPro>();
            if (controller.controllerType == ControllerType.Local)
            {
                healthtext.transform.localPosition = new Vector3(-1.01f, 0.01f, 0f);
                healthtext.transform.localRotation = Quaternion.Euler(63f, 270f, 0f);
                healthtext.transform.localScale = new Vector3(0.015f, 0.007f, 0.015f);
            }
            else
            {
                if (controller.transform.Find("NameTag")?.gameObject?.activeSelf == true)
                    healthtext.SetActive(false);
                healthtext.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                healthtext.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                healthtext.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
            }

            textRef.text = controller.assignedPlayer.Data.HealthPoints.ToString();
            textRef.alignment = TextAlignmentOptions.Center;

            if (GetFont != null)
                textRef.font = GetFont();
        }

        private static Transform GetHealthbarText(PlayerController controller) => GetHealthbarText(controller?.transform?.Find("UI"), controller);
        private static Transform GetHealthbarText(Transform UI, PlayerController controller) => GetHealthbar(UI, controller?.controllerType)?.Find("HealthText");

        private static Transform GetHealthbar(Transform UI, ControllerType? controllerType)
        {
            if (UI == null) return null;
            if (controllerType == null) return null;
            if (controllerType == ControllerType.Local)
            {
                Transform healthbar = UI.GetChild(0)?.GetChild(1);
                if (healthbar?.name != "Local UI Bar")
                {
                    MelonLogger.Warning("Could not get Local Healthbar via GetChild");
                    healthbar = UI.Find("LocalUI/Local UI Bar");
                    if (healthbar == null)
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
                if (healthbar?.name != "RemoteUI")
                {
                    MelonLogger.Warning("Could not get RemoteUI via GetChild");
                    healthbar = UI.Find("RemoteUI");
                    if (healthbar == null)
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
            static void Postfix(PlayerHealth __instance, PlayerController controller) => AddHealthbarText(GetHealthbar(__instance.transform, controller.controllerType), controller);
        }

        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SetHealthBarPercentage))]
        class SetHealthBarPercentagePatch
        {
            static void Postfix(PlayerHealth __instance, float currentHealth)
            {
                TextMeshPro text = GetHealthbarText(__instance.transform, __instance.parentController)?.GetComponent<TextMeshPro>();
                if (text != null)
                    text.text = currentHealth.ToString();
            }
        }

        [HarmonyPatch(typeof(PlayerNameTag), nameof(PlayerNameTag.SetPlayerNameTagActive))]
        class SetPlayerNameTagActivePatch
        {
            static void Prefix(PlayerNameTag __instance, bool state)
            {
                PlayerController controller = __instance.parentController;
                if (controller?.controllerType != ControllerType.Remote || __instance.transform.parent != controller.transform)
                    return;

                GetHealthbarText(controller)?.gameObject?.SetActive(!state);
            }
        }

        [HarmonyPatch(typeof(PlayerNameTag), nameof(PlayerNameTag.FadePlayerNameTag))]
        class FadePlayerNameTagPatch
        {
            static void Prefix(PlayerNameTag __instance, bool on)
            {
                PlayerController controller = __instance.parentController;
                if (controller?.controllerType != ControllerType.Remote || __instance.transform.parent != controller.transform)
                    return;

                Transform healthbar = GetHealthbarText(controller);
                if (healthbar?.gameObject == null)
                    return;

                if (!on && !healthbar.gameObject.activeSelf)
                {
                    healthbar.gameObject.SetActive(true);
                    healthbar.GetComponent<TextMeshPro>().alpha = 0f;
                }

                MelonCoroutines.Start(FadeText(healthbar.GetComponent<TextMeshPro>(), on ? 0f : 1f, __instance.playerNameFadeOutDuration, on ? () => healthbar.gameObject.SetActive(false) : null));
            }
        }

        static private IEnumerator FadeText(TextMeshPro text, float endAlpha, float PlayerNameFadeOutDuration, Action onEnd = null)
        {
            if (text == null)
            {
                onEnd?.Invoke();
                yield break;
            }

            if (PlayerNameFadeOutDuration <= 0f)
            {
                text.alpha = endAlpha;
                onEnd?.Invoke();
                yield break;
            }

            float startAlpha = text.alpha;
            float elapsed = 0f;
            while (elapsed < PlayerNameFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                text.alpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsed / PlayerNameFadeOutDuration));
                yield return null;
            }

            text.alpha = endAlpha;
            onEnd?.Invoke();
        }
        #endregion

        #region Fontifier
        private static Func<TMP_FontAsset> GetFont;
        private static Func<string, TMP_FontAsset> FontFromName;

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            if (RegisteredMelons.FirstOrDefault(m => m.Info.Name == "Fontifier")?.GetType() is Type fontifierType && fontifierType != null) (GetFont, FontFromName) = ((Func<TMP_FontAsset>, Func<string, TMP_FontAsset>))fontifierType.GetMethod("RegisterMod", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { Info.Name, new EventHandler<EventArgs>(FontChanged) });
        }

        private static void FontChanged(object sender, EventArgs args)
        {
            TMP_FontAsset font = FontFromName(((dynamic)args).Value);
            foreach (Player player in PlayerManager.instance.AllPlayers)
                if (GetHealthbarText(player.Controller)?.GetComponent<TextMeshPro>() is TextMeshPro healthbar && healthbar != null)
                    healthbar.font = font;
        }
        #endregion
    }
}
