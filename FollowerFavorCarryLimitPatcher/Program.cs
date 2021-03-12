using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FollowerFavorCarryLimitPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddRunnabilityCheck(CanRunPatch)
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

        private static void CanRunPatch(IRunnabilityState state)
        {
            switch (state.Settings.GameRelease.ToCategory())
            {
                case GameCategory.Skyrim:
                    if (File.Exists(Path.Combine(state.Settings.DataFolderPath, "Scripts\\ANDR_FollowerFavorCarryLimitScript.pex")) == false)
                    {
                        throw new Exception("Cannot find Scripts\\ANDR_FollowerFavorCarryLimitScript.pex. Make sure you have Andrealphus' Gameplay Tweaks - ANDR Tweaks 01 - Follower Favor Carry Limit installed.");
                    }

                    ModKey.TryFromNameAndExtension("Follower Favor Carry Limit.esp", out var origModKey);
                    if (state.LoadOrder.Any(lol => lol.ModKey == origModKey))
                    {
                        throw new Exception("Follower Favor Carry Limit.esp must be removed from your load order before running this patcher.");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            ScriptEntry FFCLS = new ScriptEntry
            {
                Name = "ANDR_FollowerFavorCarryLimitScript",
                Flags = ScriptEntry.Flag.Local
            };

            ScriptObjectProperty prop = new ScriptObjectProperty
            {
                Name = "VendorItemArrow",
                Object = Skyrim.Keyword.VendorItemArrow,
                Flags = ScriptProperty.Flag.Edited
            };

            FFCLS.Properties.Add(prop);

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList) || npc.EditorID == null || npc.Factions == null) continue;

                foreach (var faction in npc.Factions)
                {
                    if (!faction.Faction.TryResolve(state.LinkCache, out var fac) || fac.EditorID != "PotentialFollowerFaction") continue;

                    (state.PatchMod.Npcs.GetOrAddAsOverride(npc).VirtualMachineAdapter ??= new VirtualMachineAdapter()).Scripts.Add(FFCLS);
                }

            }
        }
    }
}
