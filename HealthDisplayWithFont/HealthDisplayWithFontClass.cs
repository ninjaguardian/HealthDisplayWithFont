using HarmonyLib;
using HealthDisplayWithFont;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// FIXME: Test with clones and park
// TODO: change color based on hp
// FIXME: Shader may not work with culling
// TODO: Fine tune size
// TODO: Damage effect
// TODO: ModUI for enabling or disabling healthbars
// TODO: Replaymod sync with healthbar setting and fix overlap

#region Assemblies
[assembly: MelonInfo(typeof(HealthDisplayWithFontClass), HealthDisplayWithFontModInfo.ModName, HealthDisplayWithFontModInfo.ModVersion, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont")]
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
        public const string ModVersion = "0.4.2";
        /// <summary>
        /// MelonLoader Version.
        /// </summary>
        public const string MLVersion = "0.7.2";
        /// <summary>
        /// The name of the healthbar object.
        /// </summary>
        public const string HealthbarTextName = "HealthText";
    }
    #endregion

    /// <summary>
    /// Adds text above all player healthbars with their health.
    /// </summary>
    public class HealthDisplayWithFontClass : MelonMod
    {
        #region Healthbar stuff
        private static readonly Dictionary<TMP_FontAsset, TMP_FontAsset> FontToShaderFont = new();

        private static void AddHealthbarText(Transform? uiBar, PlayerController controller)
        {
            if (uiBar == null) return;
            if (_healthbarMaterial == null)
            {
                MelonLogger.Error("Something really bad happened! (Context: HealthbarMaterial)", new Exception("Mod detected impossible state"));
                return;
            }
            if (uiBar.Find(HealthDisplayWithFontModInfo.HealthbarTextName) is { } otherHealthbar)
            {
                if (ControllerIsReplay(controller))
                    otherHealthbar.gameObject.SetActive(true);
                return;
            }
            GameObject healthText = new(HealthDisplayWithFontModInfo.HealthbarTextName);
            healthText.transform.SetParent(uiBar, false);

            TextMeshPro textRef = healthText.AddComponent<TextMeshPro>();
            if (controller.controllerType == ControllerType.Local)
            {
                healthText.transform.localPosition = new Vector3(-1.01f, 0.01f, 0f);
                healthText.transform.localRotation = Quaternion.Euler(63f, 270f, 0f);
                healthText.transform.localScale = new Vector3(0.015f, 0.007f, 0.015f);
                textRef.fontSize = 36;
            }
            else
            {
                if (controller.transform.Find("NameTag")?.gameObject.activeSelf == true)
                    healthText.SetActive(false);
                healthText.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                healthText.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                healthText.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
                healthText.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
                textRef.fontSize = 30;
            }

            textRef.SetText("{0}", controller.assignedPlayer.Data.HealthPoints);
            textRef.alignment = TextAlignmentOptions.Center;

            if (_getFont == null) return;

            if (controller.controllerType == ControllerType.Local)
            {
                textRef.font = _getFont(true);
                return;
            }

            TMP_FontAsset font = _getFont(true);
            if (FontToShaderFont.TryGetValue(font, out TMP_FontAsset? cacheFont))
            {
                textRef.font = cacheFont;
                return;
            }

            TMP_FontAsset newFont = _getFont(false);
            textRef.font = newFont;
            _healthbarMaterial.mainTexture = newFont.atlasTexture;
            textRef.fontMaterial = _healthbarMaterial;
            newFont.material = _healthbarMaterial;
            FontToShaderFont[font] = newFont;
        }

        private static Transform? GetHealthbarText(PlayerController? controller) => GetHealthbarText(controller?.transform.Find("UI"), controller);
        private static Transform? GetHealthbarText(Transform? ui, PlayerController? controller) => GetHealthbar(ui, controller?.controllerType)?.Find(HealthDisplayWithFontModInfo.HealthbarTextName);

        private static Transform? GetHealthbar(Transform? ui, ControllerType? controllerType)
        {
            if (ui == null) return null;
            switch (controllerType)
            {
                case null:
                    return null;
                case ControllerType.Local:
                {
                    Transform? healthbar = ui.GetChild(0)?.GetChild(1);
                    if (healthbar?.name == "Local UI Bar") return healthbar;
                    MelonLogger.Warning("Could not get Local Healthbar via GetChild");
                    healthbar = ui.Find("LocalUI/Local UI Bar");
                    if (healthbar != null) return healthbar;
                    MelonLogger.Error("Could not get Local Healthbar via Find");
                    return null;
                }
                case ControllerType.Remote:
                {
                    Transform? healthbar = ui.GetChild(1);
                    if (healthbar?.name == "RemoteUI") return healthbar;
                    MelonLogger.Warning("Could not get RemoteUI via GetChild");
                    healthbar = ui.Find("RemoteUI");
                    if (healthbar != null) return healthbar;
                    MelonLogger.Error("Could not get RemoteUI via Find");
                    return null;
                }
                default:
                    MelonLogger.Warning($"Unknown controller type: {controllerType}");
                    return null;
            }
        }

        private static bool ControllerIsReplay(PlayerController controller) => controller.transform.parent?.name == "Replay Players";
        #endregion

        #region Patches
        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Initialize))]
        private static class PlayerHealthInitPatch
        {
            private static void Postfix(PlayerHealth __instance, PlayerController controller) =>
                AddHealthbarText(GetHealthbar(__instance.transform, controller.controllerType), controller);
        }

        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SetHealthBarPercentage))]
        private static class SetHealthBarPercentagePatch
        {
            private static void Postfix(PlayerHealth __instance, float currentHealth) =>
                GetHealthbarText(__instance.transform, __instance.parentController)
                    ?.GetComponent<TextMeshPro>()
                    ?.SetText("{0}", currentHealth);
        }

        [HarmonyPatch(typeof(PlayerNameTag), nameof(PlayerNameTag.SetPlayerNameTagActive))]
        private static class SetPlayerNameTagActivePatch
        {
            private static void Prefix(PlayerNameTag __instance, bool state)
            {
                if (FadeActive.Contains(__instance)) return;

                PlayerController? controller = __instance.parentController;
                if (controller?.controllerType != ControllerType.Remote || __instance.transform.parent != controller.transform || ControllerIsReplay(controller))
                    return;

                GetHealthbarText(controller)?.gameObject.SetActive(!state);
            }
        }

        private static readonly HashSet<PlayerNameTag> FadeActive = new();

        [HarmonyPatch(typeof(PlayerNameTag), nameof(PlayerNameTag.FadePlayerNameTag))]
        private static class FadePlayerNameTagPatch
        {
            private static void Prefix(PlayerNameTag __instance, bool on)
            {
                PlayerController? controller = __instance.parentController;
                if (controller?.controllerType != ControllerType.Remote || __instance.transform.parent != controller.transform || ControllerIsReplay(controller))
                    return;

                Transform? healthbar = GetHealthbarText(controller);
                if (healthbar?.gameObject == null)
                    return;

                if (!on && !healthbar.gameObject.activeSelf)
                {
                    healthbar.gameObject.SetActive(true);
                    healthbar.GetComponent<TextMeshPro>().alpha = 0f;
                }

                if (!FadeActive.Add(__instance)) return;

                MelonCoroutines.Start(FadeText(healthbar.GetComponent<TextMeshPro>(), on ? 0f : 1f, __instance.playerNameFadeOutDuration, on ? () => { healthbar?.gameObject.SetActive(false); FadeActive.Remove(__instance); } : () => FadeActive.Remove(__instance)));
            }
        }

        private static IEnumerator FadeText(TextMeshPro text, float endAlpha, float playerNameFadeOutDuration, Action? onEnd = null)
        {
            if (text == null)
            {
                onEnd?.Invoke();
                yield break;
            }

            float startAlpha = text.alpha;
            if (playerNameFadeOutDuration <= 0f || Mathf.Approximately(startAlpha, endAlpha))
            {
                text.alpha = endAlpha;
                onEnd?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < playerNameFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                text.alpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsed / playerNameFadeOutDuration));
                yield return null;
            }

            text.alpha = endAlpha;
            onEnd?.Invoke();
        }
        #endregion

        #region Fontifier & Shader
        private static Func<bool, TMP_FontAsset>? _getFont;
        private static Func<string, bool, TMP_FontAsset>? _fontFromName;
        private static Material? _healthbarMaterial;

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            _healthbarMaterial = RumbleModdingAPI.RMAPI.AssetBundles.LoadAssetFromStream<Material>(this, $"{HealthDisplayWithFontModInfo.ModName}.healthbartextshader", "healthbartext");
            _healthbarMaterial.hideFlags = HideFlags.HideAndDontSave;
            _healthbarMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            if (FindMelon("Fontifier", "ninjaguardian")?.GetType() is not { } fontifierType) return;
            MethodInfo? method = fontifierType.GetMethod("RegisterModCopy", BindingFlags.Public | BindingFlags.Static);
            object[] param = { Info.Name, new EventHandler<EventArgs>(FontChanged) };
            object? res = method?.Invoke(null, param);
            if (res == null)
            {
                MelonLogger.Error("Something really bad happened! (Context: Fontifier installed, cannot invoke)", new Exception("Mod detected impossible state"));
                return;
            }
            (_getFont, _fontFromName) = ((Func<bool, TMP_FontAsset>, Func<string, bool, TMP_FontAsset>))res;
        }

        private static void FontChanged(object? sender, EventArgs args)
        {
            if (_fontFromName == null || _healthbarMaterial == null)
            {
                MelonLogger.Error($"Something really bad happened! (Context: FontFromName {_fontFromName != null}, HealthbarMaterial {_healthbarMaterial != null})", new Exception("Mod detected impossible state"));
                return;
            }
            TMP_FontAsset font = _fontFromName(((dynamic)args).Value, true);
            TMP_FontAsset remoteFont;
            if (FontToShaderFont.TryGetValue(font, out TMP_FontAsset? cacheFont))
                remoteFont = cacheFont;
            else
            {
                remoteFont = _fontFromName(((dynamic)args).Value, false);
                _healthbarMaterial.mainTexture = remoteFont.atlasTexture;
                remoteFont.material = _healthbarMaterial;
                FontToShaderFont[font] = remoteFont;
            }

            foreach (Player player in PlayerManager.instance.AllPlayers)
            {
                if (GetHealthbarText(player.Controller)?.GetComponent<TextMeshPro>() is not { } healthbar) continue;

                if (player.Controller.controllerType == ControllerType.Local)
                {
                    healthbar.font = font;
                    continue;
                }

                healthbar.font = remoteFont;
                healthbar.fontMaterial = _healthbarMaterial;
            }
        }
        #endregion
    }
}
