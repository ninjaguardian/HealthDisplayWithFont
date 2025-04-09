using Il2CppRUMBLE.Managers;
using Il2CppTMPro;
using HarmonyLib;
using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Players;
using Il2CppSystem.Collections.Generic;
using HealthDisplayWithFont;
using System.Collections;


//it only works affter TEMPORARY REMOVE LATER is triggered
//for some reason even though that is local... also check line 210!

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
        private static readonly Dictionary<Player, GameObject?> healthBars = new();
        private static GameObject? localHealthBar;
        private static string? localPlayerPlayfabMasterID = null;

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

            Calls.onLocalPlayerHealthChanged += OnLocalHealthChanged;
            Calls.onRemotePlayerHealthChanged += OnHealthChanged;
            Calls.onMapInitialized += MapInit;
        }

        private static void MapInit()
        {
            MelonCoroutines.Start(LocalReloadHealthBar());
        }

        private static void SetPlayfabMasterID()
        {
            if (localPlayerPlayfabMasterID == null)
            {
                localPlayerPlayfabMasterID = Calls.Players.GetLocalPlayer().Data.GeneralData.PlayFabMasterId;
                MelonLogger.Msg("Set playfab master ID: " + localPlayerPlayfabMasterID);
            }
        }

        private static IEnumerator LocalReloadHealthBar()
        {
            const byte maxTimeout = 10; //max 255 seconds
            byte timeout = maxTimeout;
            while (Calls.Players.GetLocalHealthbarGameObject() == null || Calls.Players.GetLocalHealthbarGameObject().transform.GetChild(1).GetChild(0) == null) 
            {
                yield return new WaitForSeconds(1);
                if (--timeout == 0) {
                    MelonLogger.Error("Timeout while waiting for local health bar game object. Waited " + maxTimeout + " seconds.");
                    yield break;
                }
            }

            MelonLogger.Warning(timeout);

            GameObject healthBar = new("TextMeshPro");
            healthBar.transform.parent = Calls.Players.GetLocalHealthbarGameObject().transform.GetChild(1).GetChild(0);

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

        //private static void RemoteReloadHealthBars(PlayerHealth instance, PlayerController controller)
        //{
        //    Player player = controller.assignedPlayer;

        //    if (healthBars.ContainsKey(player) && healthBars[player] != null)
        //    {
        //        Object.Destroy(healthBars[player]);
        //        healthBars.Remove(player);
        //    }
        //    else if (healthBars.ContainsKey(player)) 
        //    {
        //        healthBars.Remove(player);
        //    }
            
        //    GameObject healthBar = new() { name = "TextMeshPro" };
        //    healthBar.transform.parent = controller.gameObject.transform.GetChild(5).GetChild(0).GetChild(0);

        //    healthBar.AddComponent<TextMeshPro>();
        //    healthBar.transform.localPosition = new Vector3(-1.0564f, 0.0061f, 0.0907f);
        //    healthBar.transform.localRotation = Quaternion.Euler(62.9944f, 260f, 0f);
        //    healthBar.transform.localScale = new Vector3(0.015f, 0.007f, 0.015f);
        //    TextMeshPro textRef = healthBar.GetComponent<TextMeshPro>();
        //    textRef.text = player.Data.HealthPoints.ToString();

        //    if (fontAsset != null)
        //    {
        //        textRef.font = fontAsset;
        //    }

        //    healthBars.Add(player, healthBar);
        //}

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

        //[HarmonyPatch(typeof(PlayerData)), HarmonyPatch(nameof(PlayerData.SetHealthPoints))]
        ////[HarmonyPatch(typeof(PlayerData)), HarmonyPatch(nameof(PlayerData.HealthPoints), MethodType.Setter)]
        ////[HarmonyPatch(typeof(PlayerData)), HarmonyPatch(nameof(PlayerData.HealthPoints))]
        ////[HarmonyPatch(typeof(PlayerData)), HarmonyPatch("set_HealthPoints")]
        //class PlayerDataSetHeathPatch
        //{
        //    static void Postfix(PlayerData __instance, short hp)
        //    {
        //        foreach (Player player in Calls.Players.GetAllPlayers())
        //        {
        //            if (__instance.Player == player)
        //            {
        //                MelonLogger.Warning($"Name: {player.Data.GeneralData.PublicUsername}, hp: {hp}");
        //            }
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Initialize))]
        //class PlayerInitPatch
        //{
        //    static void Postfix(PlayerHealth __instance, PlayerController controller)
        //    {
        //        if (localPlayerPlayfabMasterID != null)
        //        {
        //            if (controller.assignedPlayer.Data.GeneralData.PlayFabMasterId != localPlayerPlayfabMasterID) {
        //                MelonLogger.Warning($"TEMPORARY REMOVE LATER: Initializing health bar for remote player: {controller.assignedPlayer.Data.GeneralData.PublicUsername}");
        //                RemoteReloadHealthBars(__instance, controller);
        //            }
        //            else if (localHealthBar != null)
        //            {
        //                MelonLogger.Warning("LOCAL PLAYER DETECTED MAKE SURE TO TEST THIS TMR");
        //                localHealthBar.GetComponent<TextMeshPro>().text = controller.assignedPlayer.Data.HealthPoints.ToString();
        //            }
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SetHealth))]
        //class PlayerHealthChangePatch
        //{
        //    static void Postfix(PlayerHealth __instance, short newHealth, short previousHealth, bool useEffects) //Possibly need `= true`
        //    {
        //        foreach (Player player in Calls.Players.GetAllPlayers()) {
        //            if (__instance.parentController.assignedPlayer == player) {
        //                MelonLogger.Warning($"Name: {player.Data.GeneralData.PublicUsername}, newhp: {newHealth}, prevhp {previousHealth}, effect {useEffects}");
        //            }
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Initialize))]
        //class PlayerInitPatch
        //{
        //    static void Postfix(PlayerController controller)
        //    {
        //        MelonCoroutines.Start(CheckPlayerHealth(controller));
        //    }
        //}

        //private static IEnumerator CheckPlayerHealth(PlayerController controller)
        //{
        //    yield return new WaitForFixedUpdate();
        //    if (controller.assignedPlayer.Data.GeneralData.PlayFabMasterId == localPlayerPlayfabMasterID)
        //    {
        //        MelonLogger.Warning($"TEMPORARY REMOVE LATER: Initializing health bar for local player: {controller.assignedPlayer.Data.GeneralData.PublicUsername}");
        //        LocalReloadHealthBar();
        //    }
        //    else
        //    {
        //        MelonLogger.Warning($"TEMPORARY REMOVE LATER: Initializing health bar for remote player: {controller.assignedPlayer.Data.GeneralData.PublicUsername}");
        //        RemoteReloadHealthBars(controller);
        //    }
        //    yield break;
        //}

        //[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.AddRemotePlayer))]
        //class AddPlayerPatch
        //{
        //    static void Postfix()
        //    {
        //        //if (GameObject.Find("/Health") != Calls.Players.GetLocalHealthbarGameObject()) {
        //            //MelonLogger.Warning("*shrug*");
        //        //}
        //        //MelonLogger.Warning("add");
        //        //foreach (Il2CppRUMBLE.Players.Player player in Calls.Players.GetAllPlayers())
        //        //{
        //        //    MelonLogger.Warning(player.Data.GeneralData.PublicUsername);
        //        //}
        //        //remoteReloadHealthBars();
        //    }
        //}

        //[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.RemovePlayer))]
        //class RemovePlayerPatch
        //{
        //    //static void Prefix()
        //    //{
        //    //    MelonLogger.Warning("remove pre");
        //    //    foreach (Il2CppRUMBLE.Players.Player player in Calls.Players.GetAllPlayers())
        //    //    {
        //    //        MelonLogger.Warning(player.Data.GeneralData.PublicUsername);
        //    //    }
        //    //}
        //    //static void Postfix()
        //    //{
        //    //    MelonLogger.Warning("remove post");
        //    //    foreach (Il2CppRUMBLE.Players.Player player in Calls.Players.GetAllPlayers())
        //    //    {
        //    //        MelonLogger.Warning(player.Data.GeneralData.PublicUsername);
        //    //    }
        //    //}
        //    static void Postfix()
        //    {
        //        //remoteReloadHealthBars();
        //    }
        //}
        private static void OnLocalHealthChanged()
        {
            if (localHealthBar != null)
            {
                localHealthBar.GetComponent<TextMeshPro>().text = Calls.Players.GetLocalPlayer().Data.HealthPoints.ToString();
        }
            else
            {
                MelonLogger.Error("Local health bar detected as null");
            }
}

        private static void OnHealthChanged()
        {
            //foreach (KeyValuePair<Il2CppRUMBLE.Players.Player, GameObject> entry in healthBars)
            //{
            //    entry.Value.GetComponent<TextMeshPro>().text = entry.Key.Data.HealthPoints.ToString();
            //}
        }
    }
}
