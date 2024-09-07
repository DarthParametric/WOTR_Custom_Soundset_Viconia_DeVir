﻿using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Localization;
using Kingmaker.Sound;
using Kingmaker.Visual.Sound;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;
using static UnityModManagerNet.UnityModManager;

namespace PC_Female_BG_Viconia;

public static class Main {
    internal static Harmony HarmonyInstance;
    internal static ModEntry.ModLogger log;
	internal static string CDText;
	internal static string ChText;
	internal static float MoveCooldownSlider;
	internal static int MoveChanceSlider;
	internal static Settings settings;
	internal static ModEntry modEntry;

    public static bool Load(ModEntry modEntry) {
        Main.modEntry = modEntry;
		log = modEntry.Logger;
        modEntry.OnGUI = OnGUI;
		modEntry.OnSaveGUI = OnSaveGUI;
        HarmonyInstance = new Harmony(modEntry.Info.Id);
		settings = Settings.Load<Settings>(modEntry);
		CDText = settings.MoveCooldown.ToString();
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }

	static void OnSaveGUI(ModEntry modEntry) {
		settings.Save(modEntry);
	}

	public static void OnGUI(ModEntry modEntry) {
			GUILayout.Label("<b>Adjust Movement Bark Values</b>", GUILayout.ExpandWidth(false));

			GUILayout.BeginHorizontal();
			GUILayout.Label("Move bark cooldown (secs):", GUILayout.ExpandWidth(false));
			GUILayout.Space(10f);
			MoveCooldownSlider = GUILayout.HorizontalSlider(settings.MoveCooldown, 0f, 20f, GUILayout.Width(140f));
			GUILayout.Space(10f);
			CDText = GUILayout.TextField(MoveCooldownSlider.ToString("0.0"), GUILayout.Width(50f));
			GUILayout.Space(10f);
			GUILayout.Label("(Default: 10.0)", GUILayout.ExpandWidth(false));
			if (float.TryParse(CDText, out float MoveCDNew))
			{
				if (MoveCDNew > 20f) { MoveCDNew = 20f; }
				settings.MoveCooldown = MoveCDNew;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Move bark proc chance (%):", GUILayout.ExpandWidth(false));
			GUILayout.Space(10f);
			MoveChanceSlider = (int)Math.Round(GUILayout.HorizontalSlider(settings.MoveChance * 100, 0, 100, GUILayout.Width(140f)));
			GUILayout.Space(10f);
			ChText = GUILayout.TextField(MoveChanceSlider.ToString(), 3, GUILayout.Width(50f));
			GUILayout.Space(10f);
			GUILayout.Label("(Default: 10%)", GUILayout.ExpandWidth(false));
			if (float.TryParse(ChText, out float MoveChNew))
			{
				MoveChNew /= 100;
				if (MoveChNew > 1f) { MoveChNew = 1f; }
				settings.MoveChance = MoveChNew;
			}
			GUILayout.EndHorizontal();

			if (GUILayout.Button("Apply Changes", GUILayout.ExpandWidth(false)))
			{
				if (!float.IsNaN(MoveCDNew) && !float.IsNaN(MoveChNew))
				{
					settings.MoveCooldown = MoveCDNew;
					settings.MoveChance = MoveChNew;
					log.Log($"Modifying movement bark settings. Cooldown: {MoveCDNew:0.0}s, Chance: {MoveChNew * 100}%");
					OnSaveGUI(modEntry);
					HarmonyInstance.UnpatchAll(modEntry.Info.Id);
					HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
				}
			}

			GUILayout.Label("<i>N.B.: May require loading a save or a game restart to take effect</i>", GUILayout.ExpandWidth(false));
	}

    [HarmonyPatch]
    public static class Soundbanks {
        public static readonly HashSet<uint> LoadedBankIds = [];

        [HarmonyPatch(typeof(AkAudioService), nameof(AkAudioService.Initialize))]
        [HarmonyPostfix]
        public static void LoadSoundbanks() {
            var banksPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try {
                log.Log($"Adding soundbank base path {banksPath}");
                AkSoundEngine.AddBasePath(banksPath);

                foreach (var f in Directory.EnumerateFiles(banksPath, "*.bnk")) {
                    var bankName = Path.GetFileName(f);
                    var akResult = AkSoundEngine.LoadBank(bankName, out var bankId);

                    if (bankName == "Init.bnk")
                        throw new InvalidOperationException("Do not include Init.bnk");

                    if (akResult == AKRESULT.AK_BankAlreadyLoaded)
                        continue;

                    log.Log($"Loading soundbank {f}");

                    if (akResult == AKRESULT.AK_Success) {
                        LoadedBankIds.Add(bankId);
                    } else {
                        log.Error($"Loading soundbank {f} failed with result {akResult}");
                    }
                }
            } catch (Exception e) {
                log.LogException(e);
                UnloadSoundbanks();
            }
        }

        public static void UnloadSoundbanks() {
            foreach (var bankId in LoadedBankIds) {
                try {
                    AkSoundEngine.UnloadBank(bankId, IntPtr.Zero);
                    LoadedBankIds.Remove(bankId);
                } catch (Exception e) {
                    log.LogException(e);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        [HarmonyPostfix]
        static void AddAsksListBlueprint()
        {
            LocalizationManager.CurrentPack.PutString("PC_Female_BG_Viconia", "Viconia DeVir");

            var blueprint = new BlueprintUnitAsksList
            {
				// Every mod requires its own unique GUID. Autogenerated on template creation.
				AssetGuid = new(System.Guid.Parse("e11fff48793b4400940132622f863fd1")),
                name = "PC_Female_BG_Viconia_Barks",
                DisplayName = new() { m_Key = "PC_Female_BG_Viconia" }
            };

            blueprint.ComponentsArray =
            [
                new UnitAsksComponent()
            {
                OwnerBlueprint = blueprint,

                // Since the blueprint is added manually by the mod, remove the usual reference
                // to the bank name to prevent a Wwise "already loaded" error.
                SoundBanks = [],
                PreviewSound = "PC_Female_BG_Viconia_Test",
                Aggro = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CombatStart_01",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CombatStart_02",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CombatStart_03",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = true,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                Pain = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Pain",
                            RandomWeight = 0.0f,
                            ExcludeTime = 0,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 2.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                Fatigue = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Fatigue",
                            RandomWeight = 0.0f,
                            ExcludeTime = 0,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 60.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                Death = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Death",
                            RandomWeight = 0.0f,
                            ExcludeTime = 0,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = true,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                Unconscious = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Unconscious",
                            RandomWeight = 0.0f,
                            ExcludeTime = 0,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = true,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                LowHealth = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_LowHealth_01",
                            RandomWeight = 0.0f,
                            ExcludeTime = 1,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_LowHealth_02",
                            RandomWeight = 0.0f,
                            ExcludeTime = 1,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 10.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                CriticalHit = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CharCrit_01",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CharCrit_02",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CharCrit_03",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 0.7f,
                    ShowOnScreen = false
                },
                Order = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_AttackOrder_01",
                            RandomWeight = 0.0f,
                            ExcludeTime = 3,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_AttackOrder_02",
                            RandomWeight = 0.0f,
                            ExcludeTime = 3,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_AttackOrder_03",
                            RandomWeight = 0.0f,
                            ExcludeTime = 3,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_AttackOrder_04",
                            RandomWeight = 0.0f,
                            ExcludeTime = 3,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                OrderMove = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_01",
                            RandomWeight = 0.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_02",
                            RandomWeight = 0.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_03",
                            RandomWeight = 0.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_04",
                            RandomWeight = 0.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_05",
                            RandomWeight = 0.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_06",
                            RandomWeight = 0.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Move_07",
                            RandomWeight = 0.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
					Cooldown = settings.MoveCooldown, // Default Cooldown value is 10s. Make it user-adjustable from 0-20s instead of fixed.
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
					Chance = settings.MoveChance, // Default Chance value is 10%. Make it user-adjustable from 0-100% instead of fixed.
                    ShowOnScreen = false
                },
                Selected = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Select_01",
                            RandomWeight = 1.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Select_02",
                            RandomWeight = 1.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Select_03",
                            RandomWeight = 1.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Select_04",
                            RandomWeight = 1.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Select_05",
                            RandomWeight = 1.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Select_06",
                            RandomWeight = 1.0f,
                            ExcludeTime = 4,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_SelectJoke",
                            RandomWeight = 0.1f,
                            ExcludeTime = 30,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                RefuseEquip = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CantEquip_01",
                            RandomWeight = 1.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CantEquip_02",
                            RandomWeight = 1.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = true,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                RefuseCast = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CantCast",
                            RandomWeight = 0.0f,
                            ExcludeTime = 1,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = true,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                CheckSuccess = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CheckSuccess_01",
                            RandomWeight = 1.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CheckSuccess_02",
                            RandomWeight = 1.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                CheckFail = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CheckFail_01",
                            RandomWeight = 1.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_CheckFail_02",
                            RandomWeight = 1.0f,
                            ExcludeTime = 2,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                RefuseUnequip = new()
                {
                    Entries = [],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                Discovery = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Discovery_01",
                            RandomWeight = 0.0f,
                            ExcludeTime = 1,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        },
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_Discovery_02",
                            RandomWeight = 0.0f,
                            ExcludeTime = 1,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                Stealth = new()
                {
                    Entries =
                    [
                        new()
                        {
                            Text = null,
                            AkEvent = "PC_Female_BG_Viconia_StealthMode",
                            RandomWeight = 1.0f,
                            ExcludeTime = 1,
                            m_RequiredFlags = [],
                            m_ExcludedFlags = [],
                            m_RequiredEtudes = null,
                            m_ExcludedEtudes = null
                        }
                    ],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                StormRain = new()
                {
                    Entries = [],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                StormSnow = new()
                {
                    Entries = [],
                    Cooldown = 0.0f,
                    InterruptOthers = false,
                    DelayMin = 0.0f,
                    DelayMax = 0.0f,
                    Chance = 1.0f,
                    ShowOnScreen = false
                },
                AnimationBarks =
                [
                    new()
                    {
                        Entries =
                        [
                            new()
                            {
                                Text = null,
                                AkEvent = "PC_Female_BG_Viconia_AttackShort",
                                RandomWeight = 0.0f,
                                ExcludeTime = 0,
                                m_RequiredFlags = [],
                                m_ExcludedFlags = [],
                                m_RequiredEtudes = null,
                                m_ExcludedEtudes = null
                            }
                        ],
                        Cooldown = 0.0f,
                        InterruptOthers = false,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 0.7f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.AttackShort
                    },
                    new()
                    {
                        Entries =
                        [
                            new()
                            {
                                Text = null,
                                AkEvent = "PC_Female_BG_Viconia_CoupDeGrace",
                                RandomWeight = 0.0f,
                                ExcludeTime = 0,
                                m_RequiredFlags = [],
                                m_ExcludedFlags = [],
                                m_RequiredEtudes = null,
                                m_ExcludedEtudes = null
                            }
                        ],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.CoupDeGrace
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.Cast
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.CastDirect
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.CastLong
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.CastShort
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.CastTouch
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.CastYourself
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.Omnicast
                    },
                    new()
                    {
                        Entries = [],
                        Cooldown = 0.0f,
                        InterruptOthers = true,
                        DelayMin = 0.0f,
                        DelayMax = 0.0f,
                        Chance = 1.0f,
                        ShowOnScreen = false,
                        AnimationEvent = MappedAnimationEventType.Precast
                    },
                ],
            },
            ];

                ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(blueprint.AssetGuid, blueprint);

                BlueprintRoot.Instance.CharGen.m_FemaleVoices = BlueprintRoot.Instance.CharGen.m_FemaleVoices
                    .Append(blueprint.ToReference<BlueprintUnitAsksListReference>())
                    .ToArray();
        }
    }
}