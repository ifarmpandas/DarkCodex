﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using DarkCodex.Components;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.RuleSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.ResourceLinks;
using Kingmaker.Blueprints.Facts;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;
using Kingmaker.Blueprints.Items.Weapons;
using Newtonsoft.Json;
using System.IO;
using Kingmaker;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using System.Reflection;
using Kingmaker.UnitLogic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.ActivatableAbilities;
using UnityEngine;

namespace DarkCodex
{
    public class Kineticist
    {
        private static List<BlueprintAbilityReference> _allAbilities;
        public static List<BlueprintAbilityReference> Blasts
        {
            get
            {
                if (_allAbilities != null)
                    return _allAbilities;
                _allAbilities = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f690edc756b748e43bba232e0eabd004").GetComponent<AddKineticistBurnModifier>().m_AppliableTo.ToList();
                return _allAbilities;
            }
        }

        public static void createKineticistBackground()
        {
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391");
            //var dragon_class = Helper.ToRef<BlueprintCharacterClassReference>("01a754e7c1b7c5946ba895a5ff0faffc");

            var feature = Helper.CreateBlueprintFeature(
                "BackgroundElementalist",
                "Elemental Plane Outsider",
                "Elemental Plane Outsider count as 1 Kineticist level higher for determining prerequisites for wild talents.",
                null,
                null,
                0
                ).SetComponents(Helper.CreateClassLevelsForPrerequisites(kineticist_class, 1));

            Helper.AppendAndReplace(ref ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("fa621a249cc836f4382ca413b976e65e").m_AllFeatures, feature.ToRef());
        }

        public static void createExtraWildTalentFeat()
        {
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391");
            var infusion_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");
            var wildtalent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0cd6d7d5448b7a420f51f8459");

            var extra_wild_talent_selection = Helper.CreateBlueprintFeatureSelection(
                "ExtraWildTalentFeat",
                "Extra Wild Talent",
                "You gain a wild talent for which you meet the prerequisites. You can select an infusion or a non-infusion wild talent, but not a blast or defense wild talent.\nSpecial: You can take this feat multiple times. Each time, you must choose a different wild talent.",
                null,
                ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("42f96fc8d6c80784194262e51b0a1d25").Icon, //ExtraArcanePool.Icon
                FeatureGroup.Feat
                ).SetComponents(
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 1, true)
                );
            extra_wild_talent_selection.Ranks = 10;

            extra_wild_talent_selection.m_AllFeatures = Helper.Append(infusion_selection.m_AllFeatures,     //InfusionSelection
                                                                    wildtalent_selection.m_AllFeatures);  //+WildTalentSelection

            Helper.AddFeats(extra_wild_talent_selection);
        }

        public static List<string> _blade_weapons = new List<string>() {
            "43ff67143efb86d4f894b10577329050",
            "6f121ff0644a2804d8239d4dfe0ace11",
            "92f9a719ffd652947ab37363266cc0a6",
            "5b0f10876af4fe54e989cc4d93bd0545",
            "7b413fc4f99050349ab5488f83fe25df",
            "df849df04cd828b4489f7827dbbf1dcd",
            "a72c3375b022c124986365d23596bd21",
            "31862bcb47f539649ae59d7e18f8ed11",
            "3ca6bbdb3c1dea541891f0568f52db05",
            "a1eee0a2735401546ba2b442e1a9d25d",
            "f58bc29b252308242a81b3f84a1d176a",
            "e72caa96c32ca3f4d8b736b97b067f58",
            "64885226d77f2bd408dde84fb8ccacc2",
            "878f68ff160c8fa42b05ade8b2d12ea5",
            "4934f54691fa90941b04341d457f4f96",
            "2e72609caf23e4843b246bec80550f06",
            "a8cd6e691ad7ee44dbdd4a255bf304d8",
            "6a1bc011f6bbc7745876ce2692ecdfb5",
        };
        public static void createKineticWhip()
        {
            var enablebuff = Helper.ToRef<BlueprintBuffReference>("426a9c079ee7ac34aa8e0054f2218074");
            var blade = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponType>("a15b2fb1d5dc4f247882a7148d50afb0");
            var blade_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("9ff81732daddb174aa8138ad1297c787");

            // make new weapon type so we can distinguish blade and whip (for AoO) and increase range 
            var weapon_energy = blade.Clone();

            try
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.All,
                });

                using (StreamWriter sw = new StreamWriter(Path.Combine(Main.ModPath, "clone.json")))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, weapon_energy);
                }

                Helper.Print("Exported clone data.");
            }
            catch (Exception e)
            {
                Helper.PrintException(e);
            }

            return;

            weapon_energy.m_AttackRange = 10.Feet();
            var weapon_physical = weapon_energy.Clone();
            weapon_physical.m_AttackType = AttackType.Melee;

            var whip = Helper.CreateBlueprintFeature(
                "KineticWhipInfusion",
                "Kinetic Whip",
                "Element: universal\nType: form infusion\nLevel: 3\nBurn: 2\nAssociated Blasts: any\nSaving Throw: none\nYou form a long tendril of energy or elemental matter. This functions as kinetic blade but counts as a reach weapon appropriate for your size. Unlike most reach weapons, the kinetic whip can also attack nearby creatures. The kinetic whip disappears at the beginning of your next turn, but in the intervening time, it threatens all squares within its reach, allowing you to make attacks of opportunity that deal the whip’s usual damage.",
                group: FeatureGroup.KineticBlastInfusion
                );

            // clone all weapon blasts
            foreach (var guid in _blade_weapons)
            {
                var weapon_base = ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>(guid); // AirKineticBladeWeapon
                if (weapon_base == null) continue;

                var weapon_clone = weapon_base.Clone();
                weapon_clone.name += "Whip";
                weapon_clone.m_Type = weapon_clone.Type.AttackType == AttackType.Melee ? weapon_physical.ToRef() : weapon_energy.ToRef();
                weapon_clone.AddAsset(GuidManager.i.Get(weapon_clone.name));

                var buff1 = Helper.CreateBlueprintBuff(
                    weapon_clone.name + "Buff",
                    "Kinetic Whip",
                    ""
                    ).SetComponents(
                    new AddKineticistBlade() { m_Blade = weapon_clone.ToRef() }
                    ); //AddKineticistBlade, see KineticBladeAirBlastBuff

                var ability1 = Helper.CreateBlueprintAbility(
                    weapon_clone.name + "Ability",
                    "Kinetic Whip",
                    "",
                    null,
                    icon: null,
                    AbilityType.Special,
                    UnitCommand.CommandType.Free,
                    AbilityRange.Personal
                    ).SetComponents(
                    Helper.CreateAbilityEffectRunAction(0,
                        Helper.CreateContextActionApplyBuff(buff1, 1),
                        Helper.CreateContextActionApplyBuff(enablebuff, 1)
                        ),
                    new AbilityKineticBlade(),
                    new AbilityKineticist() { BlastBurnCost = 0, InfusionBurnCost = 2 } // todo fix composite
                    ).ToRef2();

                whip.AddComponents(
                    Helper.CreateAddFeatureIfHasFact(ability1, ability1)
                    );
            }
            // see MetakinesisQuickenBuff; must add new ability to metakinesis lists
        }

        public static void createMobileGatheringFeat()
        {
            // --- base game stuff ---
            var buff1 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("e6b8b31e1f8c524458dc62e8a763cfb1");   //GatherPowerBuffI
            var buff2 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("3a2bfdc8bf74c5c4aafb97591f6e4282");   //GatherPowerBuffII
            var buff3 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("82eb0c274eddd8849bb89a8e6dbc65f8");   //GatherPowerBuffIII
            var gather_original_ab = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("6dcbffb8012ba2a4cb4ac374a33e2d9a");    //GatherPower
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391");

            // rename buffs, so it's easier to tell them apart
            buff1.m_Icon = gather_original_ab.Icon;
            buff1.m_DisplayName = Helper.CreateString(buff1.m_DisplayName + " Lv1");
            buff2.m_Icon = gather_original_ab.Icon;
            buff2.m_DisplayName = Helper.CreateString(buff2.m_DisplayName + " Lv2");
            buff3.m_Icon = gather_original_ab.Icon;
            buff3.m_DisplayName = Helper.CreateString(buff3.m_DisplayName + " Lv3");

            // new buff that halves movement speed, disallows normal gathering
            var mobile_debuff = Helper.CreateBlueprintBuff(
                "MobileGatheringDebuff",
                "Mobile Gathering Debuff",
                "Your movement speed is halved after gathering power.",
                null,
                Helper.CreateSprite("GatherMobileHigh.png"),
                null
                ).SetComponents(
                new TurnBasedBuffMovementSpeed(multiplier: 0.5f));

            var apply_debuff = Helper.CreateContextActionApplyBuff(mobile_debuff, 1);
            var can_gather = Helper.CreateAbilityRequirementHasBuffTimed(CompareType.LessOrEqual, 1.Rounds().Seconds, buff1, buff2, buff3);

            // cannot use usual gathering after used mobile gathering
            gather_original_ab.AddComponents(Helper.CreateAbilityRequirementHasBuffs(true, mobile_debuff));

            // ability as free action that applies buff and 1 level of gatherpower
            // - increases gather power by 1 level, similiar to GatherPower:6dcbffb8012ba2a4cb4ac374a33e2d9a
            // - applies debuff
            // - get same restriction as usual gathering
            var three2three = Helper.CreateConditional(Helper.CreateContextConditionHasBuff(buff3), Helper.CreateContextActionApplyBuff(buff3, 2));
            var two2three = Helper.CreateConditional(Helper.CreateContextConditionHasBuff(buff2).ObjToArray(), new GameAction[] { Helper.CreateContextActionRemoveBuff(buff2), Helper.CreateContextActionApplyBuff(buff3, 2) });
            var one2two = Helper.CreateConditional(Helper.CreateContextConditionHasBuff(buff1).ObjToArray(), new GameAction[] { Helper.CreateContextActionRemoveBuff(buff1), Helper.CreateContextActionApplyBuff(buff2, 2) });
            var zero2one = Helper.CreateConditional(Helper.MakeConditionHasNoBuff(buff1, buff2, buff3), new GameAction[] { Helper.CreateContextActionApplyBuff(buff1, 2) });
            var regain_halfmove = new ContextActionUndoAction(command: UnitCommand.CommandType.Move);
            var mobile_gathering_short_ab = Helper.CreateBlueprintAbility(
                "MobileGatheringShort",
                "Mobile Gathering (Move Action)",
                "You may move up to half your normal speed while gathering power.",
                null,
                Helper.CreateSprite("GatherMobileLow.png"),
                AbilityType.Special,
                UnitCommand.CommandType.Move,
                AbilityRange.Personal,
                null,
                null
                ).SetComponents(
                can_gather,
                Helper.CreateAbilityEffectRunAction(0, regain_halfmove, apply_debuff, three2three, two2three, one2two, zero2one));
            mobile_gathering_short_ab.CanTargetSelf = true;
            mobile_gathering_short_ab.Animation = CastAnimationStyle.Self;//UnitAnimationActionCastSpell.CastAnimationStyle.Kineticist;
            mobile_gathering_short_ab.HasFastAnimation = true;

            // same as above but standard action and 2 levels of gatherpower
            var one2three = Helper.CreateConditional(Helper.CreateContextConditionHasBuff(buff1).ObjToArray(), new GameAction[] { Helper.CreateContextActionRemoveBuff(buff1), Helper.CreateContextActionApplyBuff(buff3, 2) });
            var zero2two = Helper.CreateConditional(Helper.MakeConditionHasNoBuff(buff1, buff2, buff3), new GameAction[] { Helper.CreateContextActionApplyBuff(buff2, 2) });
            var hasMoveAction = Helper.CreateAbilityRequirementActionAvailable(false, ActionType.Move, 6f);
            var lose_halfmove = new ContextActionUndoAction(command: UnitCommand.CommandType.Move, amount: -1.5f);
            var mobile_gathering_long_ab = Helper.CreateBlueprintAbility(
                "MobileGatheringLong",
                "Mobile Gathering (Full Round)",
                "You may move up to half your normal speed while gathering power.",
                null,
                Helper.CreateSprite("GatherMobileMedium.png"),
                AbilityType.Special,
                UnitCommand.CommandType.Standard,
                AbilityRange.Personal,
                null,
                null
                ).SetComponents(
                can_gather,
                hasMoveAction,
                Helper.CreateAbilityEffectRunAction(0, lose_halfmove, apply_debuff, three2three, two2three, one2three, zero2two),
                new RestrictionCanGatherPowerAbility());
            mobile_gathering_long_ab.CanTargetSelf = true;
            mobile_gathering_long_ab.Animation = CastAnimationStyle.Self;
            mobile_gathering_long_ab.HasFastAnimation = true;

            var mobile_gathering_feat = Helper.CreateBlueprintFeature(
                "MobileGatheringFeat",
                "Mobile Gathering",
                "While gathering power, you can move up to half your normal speed. This movement provokes attacks of opportunity as normal.",
                null,
                mobile_debuff.Icon,
                FeatureGroup.Feat
                ).SetComponents(
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 7, true),
                Helper.CreateAddFacts(mobile_gathering_short_ab.ToRef2(), mobile_gathering_long_ab.ToRef2()),
                new RestrictionCanGatherPowerAbility());
            mobile_gathering_feat.Ranks = 1;
            Helper.AddFeats(mobile_gathering_feat);

            // make original gather ability visible for manual gathering and allow to extend buff3
            Helper.AppendAndReplace(ref gather_original_ab.GetComponent<AbilityEffectRunAction>().Actions.Actions, three2three);

        }

        public static void createImpaleInfusion()
        {
            var infusion_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391");
            var weapon = Helper.ToRef<BlueprintItemWeaponReference>("65951e1195848844b8ab8f46d942f6e8");
            var icon = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("2aad85320d0751340a0786de073ee3d5").Icon; //TorrentInfusionFeature

            var earth_base = Helper.ToRef<BlueprintAbilityReference>("e53f34fb268a7964caf1566afb82dadd");   //EarthBlastBase
            var earth_blast = Helper.ToRef<BlueprintFeatureReference>("7f5f82c1108b961459c9884a0fa0f5c4");    //EarthBlastFeature

            var metal_base = Helper.ToRef<BlueprintAbilityReference>("6276881783962284ea93298c1fe54c48");   //MetalBlastBase
            var metal_blast = Helper.ToRef<BlueprintFeatureReference>("ad20bc4e586278c4996d4a81b2448998");    //MetalBlastFeature

            var ice_base = Helper.ToRef<BlueprintAbilityReference>("403bcf42f08ca70498432cf62abee434");   //IceBlastBase
            var ice_blast = Helper.ToRef<BlueprintFeatureReference>("a8cc34ca1a5e55a4e8aa5394efe2678e");    //IceBlastFeature


            // impale feat
            BlueprintFeature impale_feat = Helper.CreateBlueprintFeature(
                "InfusionImpaleFeature",
                "Impale",
                "Element: earth\nType: form infusion\nLevel: 3\nBurn: 2\nAssociated Blasts: earth, metal, ice\n"
                + "You extend a long, sharp spike of elemental matter along a line, impaling multiple foes. Make a single attack roll against each creature or object in a 30-foot line.",
                null,
                icon,
                FeatureGroup.KineticBlastInfusion
                ).SetComponents(
                Helper.CreatePrerequisiteFeaturesFromList(true, earth_blast, metal_blast, ice_blast),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 6)
                );

            // earth ability
            var step1 = step1_run_damage(p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing, isAOE: true, half: false);
            var earth_impale_ab = Helper.CreateBlueprintAbility(
                "ImpaleEarthBlastAbility",
                impale_feat.m_DisplayName,
                impale_feat.m_Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null
                ).SetComponents(
                step1,
                step2_rank_dice(twice: false),
                step3_rank_bonus(half_bonus: false),
                step4_dc(),
                step5_burn(step1, infusion: 2, blast: 0),
                step6_feat(impale_feat),
                step7_projectile(Resource.Projectile.Kinetic_EarthBlastLine00, true, AbilityProjectileType.Line, 30, 5),
                step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetPoint(CastAnimationStyle.Kineticist);
            var attack = Helper.CreateConditional(new ContextConditionAttackRoll(weapon));
            attack.IfTrue = step1.Actions;
            step1.Actions = Helper.CreateActionList(attack);

            // metal ability
            step1 = step1_run_damage(p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing, isAOE: true, half: false);
            var metal_impale_ab = Helper.CreateBlueprintAbility(
                "ImpaleMetalBlastAbility",
                impale_feat.m_DisplayName,
                impale_feat.m_Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null
                ).SetComponents(
                step1,
                step2_rank_dice(twice: true),
                step3_rank_bonus(half_bonus: false),
                step4_dc(),
                step5_burn(step1, infusion: 2, blast: 2),
                step6_feat(impale_feat),
                step7_projectile(Resource.Projectile.Kinetic_MetalBlastLine00, true, AbilityProjectileType.Line, 30, 5),
                step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetPoint(CastAnimationStyle.Kineticist);
            attack = Helper.CreateConditional(new ContextConditionAttackRoll(weapon));
            attack.IfTrue = step1.Actions;
            step1.Actions = Helper.CreateActionList(attack);

            // ice ability
            step1 = step1_run_damage(p: PhysicalDamageForm.Piercing, e: DamageEnergyType.Cold, isAOE: true, half: false);
            var ice_impale_ab = Helper.CreateBlueprintAbility(
                "ImpaleIceBlastAbility",
                impale_feat.m_DisplayName,
                impale_feat.m_Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null
                ).SetComponents(
                step1,
                step2_rank_dice(twice: true),
                step3_rank_bonus(half_bonus: false),
                step4_dc(),
                step5_burn(step1, infusion: 2, blast: 2),
                step6_feat(impale_feat),
                step7_projectile(Resource.Projectile.Kinetic_IceBlastLine00, true, AbilityProjectileType.Line, 30, 5),
                step8_spell_description(SpellDescriptor.Cold),
                step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetPoint(CastAnimationStyle.Kineticist);
            attack = Helper.CreateConditional(new ContextConditionAttackRoll(weapon));
            attack.IfTrue = step1.Actions;
            step1.Actions = Helper.CreateActionList(attack);

            // add to feats and append variants
            Helper.AppendAndReplace(ref infusion_selection.m_AllFeatures, impale_feat.ToRef());
            Helper.AddToAbilityVariants(earth_base, earth_impale_ab);
            Helper.AddToAbilityVariants(metal_base, metal_impale_ab);
            Helper.AddToAbilityVariants(ice_base, ice_impale_ab);
        }

        public static void patchGatherPower()
        {
            var gather_original_ab = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("6dcbffb8012ba2a4cb4ac374a33e2d9a");    //GatherPower
            gather_original_ab.Hidden = false;
            gather_original_ab.Animation = CastAnimationStyle.SelfTouch;
            gather_original_ab.AddComponents(new RestrictionCanGatherPowerAbility());
        }

        /// <summary>QoL Soul Power.</summary>
        public static void patchDarkElementalist()
        {
            var soulability = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("31a1e5b27cdb78f4094630610519981c");    //DarkElementalistSoulPowerAbility
            soulability.ActionType = UnitCommand.CommandType.Free;
            soulability.m_IsFullRoundAction = false;
            soulability.HasFastAnimation = true;
            var targets = soulability.GetComponent<AbilityTargetsAround>();
            targets.m_Condition.Conditions = Array.Empty<Condition>();
            soulability.AddComponents(new AbilityRequirementOnlyCombat { Not = true });
        }

        public static void fixWallInfusion()
        {
            int counter = 0;
            foreach (var bp in ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.Values)
            {
                if (bp.Blueprint is BlueprintAbilityAreaEffect)
                {
                    var run = (bp.Blueprint as BlueprintAbilityAreaEffect).GetComponent<AbilityAreaEffectRunAction>();
                    if (run == null || !bp.Blueprint.name.StartsWith("Wall"))
                        continue;

                    run.Round = run.UnitEnter;
                    counter++;
                }
            }
            Helper.Print("Patched Wall Infusions: " + counter);
        }

        public static void createSelectiveMetakinesis()
        {
            var empower1 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f5f3aa17dd579ff49879923fb7bc2adb"); //MetakinesisEmpowerBuff
            //var empower2 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f8d0f7099e73c95499830ec0a93e2eeb"); //MetakinesisEmpowerCheaperBuff
            var kineticist = ResourcesLibrary.TryGetBlueprint<BlueprintProgression>("b79e92dd495edd64e90fb483c504b8df"); //KineticistProgression

             Sprite icon = null; // TODO icon!
            string displayname = "Metakinesis — Selective";
            string description = "At 7th level, by accepting 1 point of burn, a kineticist can adjust her kinetic blast as if using Selective Spell.";

            BlueprintActivatableAbility ab1 = Helper.CreateBlueprintActivatableAbility(
                "MetakinesisSelectiveAbility",
                displayname,
                description,
                out BlueprintBuff buff1,
                icon: icon
                );
            buff1.ComponentsArray = empower1.ComponentsArray;

            var feature1 = Helper.CreateBlueprintFeature(
                "MetakinesisSelectiveFeature",
                displayname,
                description,
                icon: icon
                ).SetComponents(
                Helper.CreateAddFacts(ab1.ToRef())
                );

            kineticist.AddFeature(7, feature1, "70322f5a2a294e54a9552f77ee85b0a7");

            //foreach (var ab in Blasts)
            //{
            //    ab.Get().AvailableMetamagic |= Metamagic.Selective;
            //    var variants = ab.Get().GetComponent<AbilityVariants>();
            //    if (variants != null)
            //        foreach (var variant in variants.m_Variants)
            //            variant.Get().AvailableMetamagic |= Metamagic.Selective;
            //}
        }

        #region Helper

        /// <summary>
        /// 1) make BlueprintAbility
        /// 2) set m_Parent to XBlastBase
        /// 3) set SpellResistance
        /// 4) make components with helpers (step1 to 9)
        /// Logic for dealing damage. Will make a composite blast, if both p and e are set. How much damage is dealt is defined in step 2.
        /// </summary>
        public static AbilityEffectRunAction step1_run_damage(PhysicalDamageForm p = 0, DamageEnergyType e = (DamageEnergyType)255, SavingThrowType save = SavingThrowType.Unknown, bool isAOE = false, bool half = false)
        {
            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, AbilityRankType.DamageDice, AbilityRankType.DamageBonus);

            List<ContextAction> list = new List<ContextAction>(2);

            bool isComposite = e != 0 && e != (DamageEnergyType)255;

            if (p != 0)
                list.Add(Helper.CreateContextActionDealDamage(p, dice, isAOE, isAOE, false, half, isComposite, AbilitySharedValue.DurationSecond, writeShare: isComposite));
            if (e != (DamageEnergyType)255)
                list.Add(Helper.CreateContextActionDealDamage(e, dice, isAOE, isAOE, false, half, isComposite, AbilitySharedValue.DurationSecond, readShare: isComposite));

            var runaction = Helper.CreateAbilityEffectRunAction(save, list.ToArray());

            return runaction;
        }

        /// <summary>
        /// Defines damage dice. Set twice for composite blasts. You shouldn't need half at all.
        /// </summary>
        public static ContextRankConfig step2_rank_dice(bool twice = false, bool half = false)
        {
            var progression = ContextRankProgression.AsIs;
            if (half) progression = ContextRankProgression.Div2;
            if (twice) progression = ContextRankProgression.MultiplyByModifier;

            var rankdice = Helper.CreateContextRankConfig(
                type: AbilityRankType.DamageDice,
                progression: progression,
                stepLevel: twice ? 2 : 0,
                baseValueType: ContextRankBaseValueType.FeatureRank,
                feature: "93efbde2764b5504e98e6824cab3d27c".ToRef<BlueprintFeatureReference>()); //KineticBlastFeature
            return rankdice;
        }

        /// <summary>
        /// Defines bonus damage. Set half_bonus for energy blasts.
        /// </summary>
        public static ContextRankConfig step3_rank_bonus(bool half_bonus = false)
        {
            var rankdice = Helper.CreateContextRankConfig(
                progression: half_bonus ? ContextRankProgression.Div2 : ContextRankProgression.AsIs,
                type: AbilityRankType.DamageBonus,
                baseValueType: ContextRankBaseValueType.CustomProperty,
                stat: StatType.Constitution,
                customProperty: "f897845bbbc008d4f9c1c4a03e22357a".ToRef<BlueprintUnitPropertyReference>()); //KineticistMainStatProperty
            return rankdice;
        }

        /// <summary>
        /// Simply makes the DC dex based.
        /// </summary>
        public static ContextCalculateAbilityParamsBasedOnClass step4_dc()
        {
            var dc = new ContextCalculateAbilityParamsBasedOnClass();
            dc.StatType = StatType.Dexterity;
            dc.m_CharacterClass = "42a455d9ec1ad924d889272429eb8391".ToRef<BlueprintCharacterClassReference>(); //KineticistClass
            return dc;
        }

        /// <summary>
        /// Creates damage tooltip from the run-action. Defines burn cost. Blast cost is 0, except for composite blasts which is 2. Talent is not used.
        /// </summary>
        public static AbilityKineticist step5_burn(AbilityEffectRunAction run, int infusion = 0, int blast = 0, int talent = 0)
        {
            var list = new List<AbilityKineticist.DamageInfo>();
            for (int i = 0; i < run.Actions.Actions.Length; i++)
            {
                var action = run.Actions.Actions[i] as ContextActionDealDamage; // TODO: don't get run, but action[]
                if (action == null) continue;

                list.Add(new AbilityKineticist.DamageInfo() { Value = action.Value, Type = action.DamageType, Half = action.Half });
            }

            var comp = new AbilityKineticist();
            comp.InfusionBurnCost = infusion;
            comp.BlastBurnCost = blast;
            comp.WildTalentBurnCost = talent;
            comp.CachedDamageInfo = list;
            return comp;
        }

        /// <summary>
        /// Required feat for this ability to show up.
        /// </summary>
        public static AbilityShowIfCasterHasFact step6_feat(BlueprintFeature fact)
        {
            return Helper.CreateAbilityShowIfCasterHasFact(fact.ToRef2());
        }

        /// <summary>
        /// Defines projectile.
        /// </summary>
        public static AbilityDeliverProjectile step7_projectile(string projectile_guid, bool isPhysical, AbilityProjectileType type, float length, float width)
        {
            string weapon = isPhysical ? "65951e1195848844b8ab8f46d942f6e8" : "4d3265a5b9302ee4cab9c07adddb253f"; //KineticBlastPhysicalWeapon //KineticBlastEnergyWeapon
            //KineticBlastPhysicalBlade b05a206f6c1133a469b2f7e30dc970ef
            //KineticBlastEnergyBlade a15b2fb1d5dc4f247882a7148d50afb0

            var projectile = Helper.CreateAbilityDeliverProjectile(
                projectile_guid.ToRef<BlueprintProjectileReference>(),
                type,
                weapon.ToRef<BlueprintItemWeaponReference>(),
                length.Feet(),
                width.Feet());
            return projectile;
        }

        /// <summary>
        /// Element descriptor for energy blasts.
        /// </summary>
        public static SpellDescriptorComponent step8_spell_description(SpellDescriptor descriptor)
        {
            return new SpellDescriptorComponent
            {
                Descriptor = descriptor
            };
        }

        // <summary>
        // This is identical for all blasts or is missing completely. It seems to me as if it not used and a leftover.
        // </summary>
        //public static ContextCalculateSharedValue step9_shared_value()
        //{
        //    return Helper.CreateContextCalculateSharedValue();
        //}

        /// <summary>
        /// Defines sfx for casting.
        /// Use either use either OnPrecastStart or OnStart for time.
        /// </summary>
        public static AbilitySpawnFx step_sfx(AbilitySpawnFxTime time, string sfx_guid)
        {
            var sfx = new AbilitySpawnFx();
            sfx.Time = time;
            sfx.PrefabLink = new PrefabLink() { AssetId = sfx_guid };
            return sfx;
        }

        #endregion
    }

    #region Patches

    /// <summary>
    /// Normal: The level of gathering power is determined by the mode (none, low, medium, high) selected. If the mode is lower than the already accumulated gather level, then levels are lost.
    /// Patched: The level of gathering is true to the accumulated level or the selected mode, whatever is higher.
    /// </summary>
    [HarmonyPatch(typeof(KineticistController), nameof(KineticistController.TryApplyGatherPower))]
    public class Patch_TrueGatherPowerLevel
    {
        public static BlueprintBuff buff1 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("e6b8b31e1f8c524458dc62e8a763cfb1");
        public static BlueprintBuff buff2 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("3a2bfdc8bf74c5c4aafb97591f6e4282");
        public static BlueprintBuff buff3 = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("82eb0c274eddd8849bb89a8e6dbc65f8");

        public static bool Prefix(UnitPartKineticist kineticist, BlueprintAbility abilityBlueprint, ref KineticistAbilityBurnCost cost)
        {
            if (kineticist == null || abilityBlueprint.GetComponent<AbilityKineticist>() == null || kineticist.GatherPowerAbility == null)
                return false;

            int buffRank = kineticist.TargetGatherPowerRank; // get the target power rank

            // check if stronger buff exists and if so apply it instead
            if (buffRank < 1 && kineticist.Owner.Buffs.GetBuff(buff1) != null)
                buffRank = 1;
            else if (buffRank < 2 && kineticist.Owner.Buffs.GetBuff(buff2) != null)
                buffRank = 2;
            else if (buffRank < 3 && kineticist.Owner.Buffs.GetBuff(buff3) != null)
                buffRank = 3;

            int value = KineticistUtils.CalculateGatherPowerBonus(kineticist.GatherPowerBaseValue, buffRank); // add increase from Supercharge

            cost.IncreaseGatherPower(value); // apply value

            return false;
        }
    }

    [HarmonyPatch(typeof(AddKineticistBlade), nameof(AddKineticistBlade.OnActivate))]
    public class Patch_KineticistAllowOpportunityAttack
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            List<CodeInstruction> list = instr.ToList();
            MethodInfo original = AccessTools.Method(typeof(UnitState), nameof(UnitState.AddCondition));
            MethodInfo replacement = AccessTools.Method(typeof(Patch_KineticistAllowOpportunityAttack), nameof(NullReplacement));

            for (int i = 0; i < list.Count; i++)
            {
                var mi = list[i].operand as MethodInfo;
                if (mi != null && mi == original)
                {
                    Helper.PrintDebug("KineticistAoO at " + i);
                    list[i].operand = replacement;
                }
            }

            return list;
        }

        public static void NullReplacement(UnitState state, UnitCondition condition, Buff sourceBuff)
        {
        }
    }

    [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.IsThreatHand))]
    public class Patch_KineticistAllowOpportunityAttack2
    {
        private static BlueprintGuid KineticBlastPhysicalBlade = BlueprintGuid.Parse("b05a206f6c1133a469b2f7e30dc970ef");
        private static BlueprintGuid KineticBlastEnergyBlade = BlueprintGuid.Parse("a15b2fb1d5dc4f247882a7148d50afb0");

        public static bool Prefix(UnitEntityData unit, WeaponSlot hand, ref bool __result)
        {
            if (!hand.HasWeapon)
                __result = false;

            else if (!hand.Weapon.Blueprint.IsMelee && !unit.State.Features.SnapShot)
                __result = false;

            else if (hand.Weapon.Blueprint.IsUnarmed && !unit.Descriptor.State.Features.ImprovedUnarmedStrike)
                __result = false;

            else if (hand.Weapon.Blueprint.Type?.AssetGuid == KineticBlastPhysicalBlade
                  || hand.Weapon.Blueprint.Type?.AssetGuid == KineticBlastEnergyBlade)
                __result = false;

            else
                __result = true;

            return false;
        }
    }


    #endregion

}
