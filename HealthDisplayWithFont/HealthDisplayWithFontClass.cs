using HarmonyLib;
using HealthDisplayWithFont;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using RumbleModdingAPI;
using System.Collections;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Test with clones
// The issue only occurs after the playerhealth is initlizied

[assembly: MelonInfo(typeof(HealthDisplayWithFontClass), "HealthDisplayWithFont", "0.1.1", "ninjaguardian")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

namespace HealthDisplayWithFont
{
    /// <summary>
    /// Adds text above all player healthbars with their health.
    /// </summary>
    public class HealthDisplayWithFontClass : MelonMod
    {
        private static TMP_FontAsset fontAsset;
        private static TextMeshPro localHealthBarText;

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

            Calls.onMapInitialized += MapInit;
        }

        private static void MapInit()
        {
            MelonCoroutines.Start(LocalReloadHealthBar());
        }

        private static IEnumerator LocalReloadHealthBar()
        {
            const byte maxTimeout = 10; //max 255 seconds
            byte timeout = maxTimeout;
            while (Calls.Players.GetPlayerController().transform.Find("UI/LocalUI/Local UI Bar") == null || localHealthBarText != null) 
            {
                yield return new WaitForSeconds(1);
                if (--timeout == 0) {
                    MelonLogger.Error($"Timeout while waiting for local health bar game object. Waited {maxTimeout} seconds.");
                    MelonLogger.Warning($"Is PlayerController null: {Calls.Players.GetPlayerController() == null} (expected: false)");
                    MelonLogger.Warning($"Is UI Bar null: {Calls.Players.GetPlayerController().transform.Find("UI/LocalUI/Local UI Bar") == null} (expected: false)");
                    MelonLogger.Warning($"Is Local UI Bar Text null: {localHealthBarText == null} (expected: true)");
                    yield break;
                }
            }

            GameObject healthBar = new("TextMeshPro");
            healthBar.transform.SetParent(Calls.Players.GetPlayerController().transform.Find("UI/LocalUI/Local UI Bar"), false);

            TextMeshPro textRef = healthBar.AddComponent<TextMeshPro>();
            healthBar.transform.localPosition = new Vector3(-1.01f, 0.01f, 0.1f);
            healthBar.transform.localRotation = Quaternion.Euler(63f, 270f, 0f);
            healthBar.transform.localScale = new Vector3(0.015f, 0.007f, 0.015f);
            textRef.text = Calls.Players.GetLocalPlayer().Data.HealthPoints.ToString();

            if (fontAsset != null)
            {
                textRef.font = fontAsset;
            }

            localHealthBarText = textRef;
        }

        //[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Initialize))]
        //class PlayerHealthInitPatch
        //{
        //    static void Postfix(ref PlayerHealth __instance, ref PlayerController controller)
        //    {
        //        MelonLogger.Warning(controller.assignedPlayer.Data.GeneralData.PublicUsername);

        //        if (controller.controllerType == ControllerType.Local)
        //        {
        //            bool foundYet = false;
        //            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>()) // Removes clones
        //            {
        //                if (obj.name == "Player Controller(Clone)")
        //                {
        //                    if (foundYet)
        //                        return;
        //                    foundYet = true;
        //                }
        //            }
        //        }

        //        MelonLogger.Warning("Pass " + (controller.controllerType == ControllerType.Local));


        //        if (controller.controllerType == ControllerType.Local)
        //        {
        //            MelonLogger.Warning("BLAH");
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SetHealthBarPercentage))]
        class HealthBarPercentagePatch
        {
            static void Postfix(ref PlayerHealth __instance, float currentHealth)
            {
                if (__instance?.parentController?.controllerType == ControllerType.Local)
                {
                    OnLocalHealthChanged(currentHealth);
                }
            }
        }

        private static void OnLocalHealthChanged(float newHealth)
        {
            if (localHealthBarText != null)
            {
                localHealthBarText.text = newHealth.ToString();
            }
            else
            {
                MelonLogger.Error("Local UI Bar Text detected as null. Attempting to fix.");
                MapInit();
            }
        }
    }
}
