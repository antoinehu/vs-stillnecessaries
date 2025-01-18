using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using System.Diagnostics;

namespace necessaries.src.HarmonyStuff
{
    [HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
    class PatchesGetHeldItemInfo
    {

        static public string IsRepaired(ItemStack stack)
        { 
            if (stack.Attributes.HasAttribute("maxRepair")) return " " + Lang.Get("repaired");
            else return "";
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // I guess what happens is something like
            // t = Transpiler(instructions)
            // foreach (instruction in t) {
            //    new_instruction = instruction
            // }
            PropertyInfo m_Attributes = AccessTools.Property(typeof(ItemStack), "Attributes");
            MethodInfo m_getInt32 = AccessTools.Method(typeof(ITreeAttribute), "GetInt", new Type[] { typeof(string), typeof(int) });
            MethodInfo m_LangGet = AccessTools.Method(typeof(Lang), "Get", new Type[] { typeof(string), typeof(object[]) });
            MethodInfo m_conCatTwo = AccessTools.Method(typeof(string), "Concat", new Type[] { typeof(string), typeof(string) });
            MethodInfo m_IsRepaired = AccessTools.Method(typeof(PatchesGetHeldItemInfo), "IsRepaired", new Type[] { typeof(ItemStack) });

            int refCount = 0;
            int patched = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                // local variable 3 = class Vintagestory.API.Common.IHeldBag bag
                // local variable 0 = Vintagestory.API.Common.ItemStack
                // local variable 2 = durability
                // in Vintage Story 1.20.0-rc.9
                if (instruction.opcode == OpCodes.Ldloc_3) refCount++;
                // argument 2 = [System.Runtime]System.Text.StringBuilder
                if (instruction.IsLdarg(2) && refCount == 1 && patched == 0)
                {
                    // These go before ldarg.2 that is after the first ldloc.3
                    // First call get_Attributes() to get ITreeAttributes and store it on top of the stack
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Callvirt, m_Attributes.GetGetMethod(false));
                    // Put "maxRepair" and durability on top of the stack
                    yield return new CodeInstruction(OpCodes.Ldstr, "maxRepair");
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    // Call ITreeAttributes.GetInt("maxRepair", defaultValue=durability)
                    yield return new CodeInstruction(OpCodes.Callvirt, m_getInt32);
                    // Store the resulting value for 'maxRepair' into durability
                    yield return new CodeInstruction(OpCodes.Stloc_2);
                    // Continue normal code
                    yield return instruction;
                    patched = 1;
                } 
                else if(patched == 1 && instruction.Calls(m_LangGet))
                {
                    yield return instruction;
                    //Add check for maxRapair Attribute
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, m_IsRepaired);
                    yield return new CodeInstruction(OpCodes.Call, m_conCatTwo);

                    patched = 2;
                }
                else
                    yield return instruction;

            }
        }


    }
}

