using HarmonyLib;
using System.Reflection;

static class PowerDistributionPatch
{

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    // A21: Exact copy of A20 patch version
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerBatteryBank))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerBatteryBank_HandlePowerReceived
    {
        static MethodInfo IsPoweredChanged = AccessTools.Method(typeof(PowerItem), "IsPoweredChanged");
        static bool Prefix(PowerItem __instance, ref ushort power, ref bool ___isPowered)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            if (newPowered != __instance.IsPowered)
            {
                ___isPowered = newPowered;
                IsPoweredChanged.Invoke(__instance, new object[] { newPowered });
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }

            power -= num;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    // A21: Exact copy of A20 patch version
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerTrigger_HandlePowerReceived
    {
        static MethodInfo CheckForActiveChange = AccessTools.Method(typeof(PowerTrigger), "CheckForActiveChange");
        static MethodInfo HandleParentTriggering = AccessTools.Method(typeof(PowerTrigger), "HandleParentTriggering");
        static bool Prefix(PowerTrigger __instance, ref ushort power, ref bool ___isPowered)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            ___isPowered = (int)num == (int)__instance.RequiredPower;
            power -= num;
            if (power <= (ushort)0)
                return false;
            CheckForActiveChange.Invoke(__instance, null);
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                if (__instance.Children[index] is PowerTrigger child)
                    HandleParentTriggering.Invoke(__instance, new object[] { child });
            }
            return false;
        }
    }


    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    // A21: Exact copy of A20 patch version
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerTimerRelay))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerTimerRelay_HandlePowerReceived
    {
        static MethodInfo CheckForActiveChange = AccessTools.Method(typeof(PowerTimerRelay), "CheckForActiveChange");
        static bool Prefix(PowerTimerRelay __instance,
            ref ushort power, ref bool ___isPowered,
            ref bool ___isActive, ref bool ___isTriggered)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            ___isPowered = newPowered;
            power -= num;
            CheckForActiveChange.Invoke(__instance, null);
            // Fix weird vanilla implementation "bug"
            ___isActive = ___isTriggered;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    // A21: Exact copy of A20 patch version
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerConsumerToggle_HandlePowerReceived
    {
        static MethodInfo IsPoweredChanged = AccessTools.Method(typeof(PowerConsumer), "IsPoweredChanged");
        static bool Prefix(PowerConsumerToggle __instance,
            ref ushort power, ref bool ___isPowered)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            if (__instance.IsToggled == false) newPowered = false;
            if (newPowered != __instance.IsPowered)
            {
                ___isPowered = newPowered;
                IsPoweredChanged.Invoke(__instance, new object[] { newPowered });
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }

            power -= __instance.IsToggled ? num : (ushort)0;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Doesn't obey `PowerChildren` flag!?
    // A21: Exact copy of A20 patch version
    [HarmonyPatch(typeof(PowerTimerRelay))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTimerRelay_HandlePowerUpdate
    {
        static bool Prefix(PowerTimerRelay __instance,
            bool parentIsOn, ref bool ___hasChangesLocal)
        {

            if (__instance.TileEntity != null)
            {
                ((TileEntityPoweredTrigger)__instance.TileEntity).Activate(
                    __instance.IsPowered & parentIsOn, __instance.IsTriggered);
                __instance.TileEntity.SetModified();
            }

            ___hasChangesLocal = true;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Doesn't obey `PowerChildren` flag!?
    // A21: Exact copy of A20 patch version
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTrigger_HandlePowerUpdate
    {
        static MethodInfo HandleSingleUseDisable = AccessTools.Method(typeof(PowerTrigger), "HandleSingleUseDisable");
        static bool Prefix(PowerTrigger __instance,
            bool parentIsOn, ref bool ___hasChangesLocal)
        {

            if (__instance.TileEntity != null)
            {
                ((TileEntityPoweredTrigger)__instance.TileEntity).Activate(
                    __instance.IsPowered & parentIsOn, __instance.IsTriggered);
                __instance.TileEntity.SetModified();
            }

            ___hasChangesLocal = true;
            HandleSingleUseDisable.Invoke(__instance, null);

            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Obeys to `PowerChildren` flag!!
    // A21: Exact copy of A20 patch version
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumerToggle_HandlePowerUpdate
    {
        static bool Prefix(PowerConsumerToggle __instance,
            bool isOn, ref bool ___lastActivate)
        {
            bool activated = __instance.IsPowered & isOn && __instance.IsToggled;
            if (__instance.TileEntity != null)
            {
                __instance.TileEntity.Activate(activated);
                if (activated && ___lastActivate != activated)
                    __instance.TileEntity.ActivateOnce();
            }
            ___lastActivate = activated;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Obeys to `PowerChildren` flag!!
    // A21: Exact copy of A20 patch version
    [HarmonyPatch(typeof(PowerConsumer))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumer_HandlePowerUpdate
    {
        static bool Prefix(PowerConsumerToggle __instance,
            bool isOn, ref bool ___lastActivate)
        {
            bool activated = __instance.IsPowered & isOn;
            if (__instance.TileEntity != null)
            {
                __instance.TileEntity.Activate(activated);
                if (activated && ___lastActivate != activated)
                    __instance.TileEntity.ActivateOnce();
                __instance.TileEntity.SetModified();
            }
            ___lastActivate = activated;
            return false;
        }
    }

    // Only consumers obey this flag!?
    // A21: Exact copy of A20 patch version
    [HarmonyPatch(typeof(PowerItem))]
    [HarmonyPatch("PowerChildren")]
    public class PowerItem_PowerChildren
    {
        static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }

    // Don't forcefully remove children if one trigger goes inactive
    // Some child might be another trigger, forming a trigger group,
    // thus if one sub-triggers is active, children stay connected.
    // A21: Exact copy of A20 patch version
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandleDisconnectChildren")]
    public class PowerTrigger_HandleDisconnectChildrenOverhaul
    {
        static bool Prefix(PowerTrigger __instance)
        {

            if (__instance.IsActive) return false;

            // Make sure no parent is triggered
            if (__instance.Parent != null)
            {
                if (__instance.Parent is PowerTrigger upstream)
                {
                    if (upstream.IsActive)
                    {
                        return false;
                    }
                }
            }
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                // If child is another trigger, only disconnect if not active
                if (__instance.Children[index] is PowerTrigger trigger)
                {
                    trigger.SetTriggeredByParent(false);
                    if (trigger.IsActive)
                    {
                        continue;
                    }
                    trigger.HandleDisconnectChildren();
                }
                else if (!(__instance.Children[index] is PowerSource))
                {
                    __instance.Children[index].HandleDisconnect();
                }
            }
            // Disable power for this instance?
            // Get better results without this?
            // __instance.HandlePowerUpdate(false);
            // Fully replace implementation
            return false;
        }
    }
}

