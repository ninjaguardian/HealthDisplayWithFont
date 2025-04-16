using Il2CppTMPro;
using HarmonyLib;
using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Players;
using HealthDisplayWithFont;
using System.Collections;


// Basicaly done. The only issue is that when loding into a scene, SetHealthBarPercentage is triggered.
// Find a way to check the current scene when the code runs. Go from there.

[assembly: MelonInfo(typeof(HealthDisplayWithFontClass), "HealthDisplayWithFont", "1.0.0", "ninjaguardian")]
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
        private static TMP_FontAsset? fontAsset;
        private static GameObject? localHealthBar;

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

                fontAsset = LoadFont(fontPaths);
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
            while (GameObject.Find("/Health") == null || GameObject.Find("/Health").transform.GetChild(1).GetChild(0) == null || !(localHealthBar == null)) 
            {
                yield return new WaitForSeconds(1);
                if (--timeout == 0) {
                    MelonLogger.Error($"Timeout while waiting for local health bar game object. Waited {maxTimeout} seconds.");
                    MelonLogger.Warning($"Is Health null: {GameObject.Find("/Health") == null} (expected: false)");
                    MelonLogger.Warning($"Is Healthbar null: {GameObject.Find("/Health").transform.GetChild(1).GetChild(0) == null} (expected: false)");
                    MelonLogger.Warning($"Is localHealthBar null: {localHealthBar == null} (expected: true)");
                    yield break;
                }
            }

            GameObject healthBar = new("TextMeshPro");
            healthBar.transform.SetParent(GameObject.Find("/Health").transform.GetChild(1).GetChild(0), false);

            TextMeshPro textRef = healthBar.AddComponent<TextMeshPro>();
            healthBar.transform.localPosition = new Vector3(-1.01f, 0.01f, 0.1f);
            healthBar.transform.localRotation = Quaternion.Euler(63f, 270f, 0f);
            healthBar.transform.localScale = new Vector3(0.015f, 0.007f, 0.015f);
            textRef.text = Calls.Players.GetLocalPlayer().Data.HealthPoints.ToString();

            if (fontAsset != null)
            {
                textRef.font = fontAsset;
            }

            localHealthBar = healthBar;
        }

        private static TMP_FontAsset? LoadFont(string[] fontNames)
        {
            Il2CppAssetBundle bundle = Il2CppAssetBundleManager.LoadFromFile(@"UserData\HealthDisplayWithFont\fontbundle");
            foreach (string fontName in fontNames)
            {
                try
                {
                    Font? loadedFont = Object.Instantiate(bundle.LoadAsset<Font>(fontName));
                    if (loadedFont != null)
                    {
                        TMP_FontAsset myFont = TMP_FontAsset.CreateFontAsset(loadedFont);
                        myFont.hideFlags = HideFlags.HideAndDontSave;
                        return myFont;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        //[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Initialize))]
        //class PlayerHealthInitPatch
        //{
        //    static void Postfix(ref PlayerHealth __instance, ref PlayerController controller)
        //    {
        //        MelonLogger.Warning(controller.assignedPlayer.Data.GeneralData.PublicUsername);

        //        if (controller.controllerType == ControllerType.Local)
        //        { // Removes clones
        //            bool foundYet = false;
        //            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
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
        //            Il2CppSystem.Reflection.MethodInfo? method = Il2CppType.Of<HealthDisplayWithFontClass>().GetMethod(
        //                nameof(OnLocalHealthChanged),
        //                Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Static
        //            );

        //            if (method == null)
        //            {
        //                MelonLogger.Error(nameof(OnLocalHealthChanged) + " method not found.");
        //                return;
        //            }

        //            Il2CppSystem.Delegate del = Il2CppSystem.Delegate.CreateDelegate(
        //                Il2CppType.Of<UnityAction<short>>(),
        //                method
        //            );

        //            if (del == null)
        //            {
        //                MelonLogger.Error("Failed to create delegate for " + nameof(OnLocalHealthChanged));
        //                return;
        //            }

        //            __instance.onDamageTaken.AddListener((UnityAction<short>)del);
        //        }
        //        else
        //        {
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SetHealthBarPercentage))]
        class HealthBarPercentagePatch
        {
            static void Postfix(ref PlayerHealth __instance, float currentHealth, float previousHealth, bool useEffects)
            {
                if (__instance.parentController.controllerType == ControllerType.Local)
                {
                    OnLocalHealthChanged();
                }
            }
        }

        private static void OnLocalHealthChanged()//short healthGained)
        {
            if (localHealthBar != null)
            {
                localHealthBar.GetComponent<TextMeshPro>().text = Calls.Players.GetLocalPlayer().Data.HealthPoints.ToString();
            }
            else
            {
                MelonLogger.Error("Local health bar detected as null");
                MapInit();
            }
        }
    }
}
