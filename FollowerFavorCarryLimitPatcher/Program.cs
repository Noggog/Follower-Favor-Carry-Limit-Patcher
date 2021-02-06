using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using System.IO;

namespace FollowerFavorCarryLimitPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run(args, new RunPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "YourPatcher.esp",
                        TargetRelease = GameRelease.SkyrimSE,
                    }
                });
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            FormKey.TryFactory("0917E7:Skyrim.esm", out var scriptObjectFormKey);

            if (scriptObjectFormKey == null)
            {
                throw new Exception("Cannot find VendorItemArrow [KYWD:000917E7]. Something is wrong with your configuration.");
            }

            if (File.Exists(Path.Combine(state.Settings.DataFolderPath, "Scripts\\ANDR_FollowerFavorCarryLimitScript.pex")) == false)
            {
                throw new Exception("Cannot find Scripts\\ANDR_FollowerFavorCarryLimitScript.pex. Make sure you have Andrealphus' Gameplay Tweaks - ANDR Tweaks 01 - Follower Favor Carry Limit installed.");
            }

            ModKey.TryFromNameAndExtension("Follower Favor Carry Limit.esp", out var origModKey);
            if (state.LoadOrder.Keys.Contains(origModKey))
            {
                throw new Exception("Follower Favor Carry Limit.esp must be removed from your load order before running this patcher.");
            }

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList) || npc.EditorID == null || npc.Factions == null) continue;
                
                foreach (var faction in npc.Factions)
                {
                    if (faction.Faction.TryResolve(state.LinkCache, out var fac) && fac.EditorID == "PotentialFollowerFaction")
                    {
                        var npcCopy = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                        ScriptEntry FFCLS = new ScriptEntry();
                        FFCLS.Name = "ANDR_FollowerFavorCarryLimitScript";
                        FFCLS.Flags |= ScriptEntry.Flag.Local;

                        ScriptObjectProperty prop = new ScriptObjectProperty();
                        prop.Name = "VendorItemArrow";
                        prop.Flags |= ScriptProperty.Flag.Edited;
                        prop.Object = scriptObjectFormKey;

                        FFCLS.Properties.Add(prop);
                        if (npcCopy != null)
                        {
                            if(npcCopy.VirtualMachineAdapter == null)
                            {
                                npcCopy.VirtualMachineAdapter = new VirtualMachineAdapter();
                            }
                            npcCopy.VirtualMachineAdapter.Scripts.Add(FFCLS);
                        }
                    }
                }
                
            }
        }
    }
}
