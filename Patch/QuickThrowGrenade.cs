using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InputSystem;
using HarmonyLib;
using QuickThrow.Utils;
using System.Linq;
using UnityEngine;

namespace QuickThrow.Patch
{
    [HarmonyPatch(typeof(Player))]
    internal static class QuickThrowGrenade
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetInHands", typeof(ThrowWeapItemClass), typeof(Callback<IHandsThrowController>))]
        public static bool SetInHandsPrefix(
            Player __instance,
            ThrowWeapItemClass throwWeap,
            Callback<IHandsThrowController> callback)
        {
            // This path is used by the grenade selector UI, for example when the
            // player holds G and chooses a grenade with the mouse wheel.
            if (!ShouldQuickThrow(__instance))
                return true;

            StartQuickThrow(__instance, throwWeap, "SetInHands grenade");
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetInHandsForQuickUse", typeof(Item), typeof(Callback<IOnHandsUseCallback>))]
        public static bool QuickUsePrefix(
            Player __instance,
            Item quickUseItem,
            Callback<IOnHandsUseCallback> callback)
        {
            // SPT 4.x also has a generic quick-use path. Most quick-use items should
            // remain vanilla, so only grenades are redirected here.
            if (quickUseItem is not ThrowWeapItemClass throwWeap)
                return true;

            if (!ShouldQuickThrow(__instance))
                return true;

            StartQuickThrow(__instance, throwWeap, "generic quick-use");
            return false;
        }

        private static bool ShouldQuickThrow(Player player)
        {
            // All entry points share this gate so the disable toggle, local-player
            // check, and cancel key behave consistently.
            if (Plugin.DisableFastGrenade.Value)
                return false;

            if (!player.IsYourPlayer)
                return false;

            return !Input.GetKey(Plugin.KeyboardBindingOrigine.Value.MainKey);
        }

        private static void StartQuickThrow(Player player, ThrowWeapItemClass throwWeap, string source)
        {
            Plugin.LogSource?.LogInfo($"QuickThrow starting from {source}: {throwWeap.TemplateId}");

            // This is the SPT 4.x fast grenade controller. It skips the normal
            // grenade-in-hands controller and immediately starts the throw animation.
            player.SetInHandsForQuickUse(
                throwWeap,
                new Callback<GInterface206>(quickUseResult =>
                {
                    QuickThrowLogger.Log($"[QuickThrowGrenade] Quick throw started from {source}: {quickUseResult}");
                }));
        }

        [HarmonyPatch]
        public static class FastGrenadeCommand
        {
            // Class1725 is Tarkov's obfuscated player input translator in SPT 4.x.
            // The direct G press is handled there before it reaches the UI path.
            private static readonly System.Type PlayerInputTranslatorType = AccessTools.TypeByName("Class1725");

            private static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(PlayerInputTranslatorType, "TranslateCommand");
            }

            [HarmonyPrefix]
            public static bool Prefix(object __instance, ECommand command, ref InputNode.ETranslateResult __result)
            {
                // PressThrowGrenade is the normal G press. ThrowGrenade is included
                // for compatibility with alternate input states/bind mappings.
                if (command != ECommand.ThrowGrenade && command != ECommand.PressThrowGrenade)
                    return true;

                // Pull the private Player_0 field from Class1725. Reflection keeps
                // this patch localized to the obfuscated input translator.
                var player = AccessTools.Field(PlayerInputTranslatorType, "Player_0")?.GetValue(__instance) as Player;
                if (player == null)
                    return true;

                if (!ShouldQuickThrow(player))
                    return true;

                if (!player.StateIsSuitableForHandInput || player.IsInBufferZone)
                    return true;

                // Match vanilla safety checks so quick throw does not interrupt
                // blocked hand operations such as heavy item use or transitions.
                if (player.HandsController.InCanNotBeInterruptedOperation())
                    return true;

                // If the quick grenade controller is already active, consume the
                // input and avoid starting a second throw on top of the first one.
                if (player.HandsController is GInterface206)
                {
                    __result = InputNode.ETranslateResult.Ignore;
                    return false;
                }

                var grenade = GetPreferredGrenade(player);
                if (grenade == null)
                    return true;

                StartQuickThrow(player, grenade, $"command {command}");
                __result = InputNode.ETranslateResult.Ignore;
                return false;
            }

            private static ThrowWeapItemClass? GetPreferredGrenade(Player player)
            {
                var inventoryController = player.InventoryController;
                var equipment = inventoryController.Inventory.Equipment;

                // Tarkov already computes the top-priority grenade for the HUD.
                // If that is empty, fall back to the same helper used by the UI list.
                return equipment.TopPriorityGrenade
                    ?? GClass3373.GetThrowablePriorityGrenadesList(inventoryController).FirstOrDefault();
            }
        }

        [HarmonyPatch(typeof(Player.BaseGrenadeHandsController), "vmethod_1")]
        public static class ForceLowThrow
        {
            [HarmonyPrefix]
            public static void Prefix(
                ref float timeSinceSafetyLevelRemoved,
                ref bool low,
                Player.BaseGrenadeHandsController __instance)
            {
                // This patch runs at the final throw calculation. When the low-throw
                // key is held, force the quick grenade controller's low flag.
                if (Plugin.DisableFastGrenade.Value)
                    return;

                var playerField = AccessTools.Field(typeof(Player.BaseGrenadeHandsController), "_player");
                if (playerField?.GetValue(__instance) is not Player player)
                    return;

                if (__instance is not Player.QuickGrenadeThrowHandsController || !player.IsYourPlayer)
                    return;

                if (!Input.GetKey(Plugin.KeyboardBindingShort.Value.MainKey))
                    return;

                low = true;
                QuickThrowLogger.Log("[QuickThrowGrenade] Forced low throw");
            }
        }
    }
}
