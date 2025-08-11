using HarmonyLib;
using HealthDisplayWithFont;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Test with clones
// TODO: make gh action to make sure version nums are correct

[assembly: MelonInfo(typeof(HealthDisplayWithFontClass), "HealthDisplayWithFont", "0.1.3", "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(0, 7, 0, true)]

namespace HealthDisplayWithFont
{
    /// <summary>
    /// Adds text above all player healthbars with their health.
    /// </summary>
    public class HealthDisplayWithFontClass : MelonMod
    {
        private static TMP_FontAsset fontAsset;

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            try
            {
                string[] fontPaths = new string[]
                {
                    "font.ttf",
                    "font.otf"
                };

                foreach (string fontName in fontPaths)
                {
                    try
                    {
                        var loadedFont = TMP_FontAsset.CreateFontAsset(
                            $@"UserData\HealthDisplayWithFont\{fontName}",
                            0,
                            90,
                            5,
                            GlyphRenderMode.SDFAA,
                            1024,
                            1024
                        );
                        loadedFont.hideFlags = HideFlags.HideAndDontSave;
                        fontAsset = loadedFont;
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"Error loading font bundle. Using default font. Error: {ex.Message}");
            }
        }

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

            if (fontAsset != null)
            {
                textRef.font = fontAsset;
            }
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
    }
}
