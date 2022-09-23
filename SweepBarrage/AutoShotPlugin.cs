using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using UnityEngine;
namespace AutoShot
{
	[BepInDependency("com.ThinkInvisible.ClassicItems", BepInDependency.DependencyFlags.SoftDependency)]
	[R2APISubmoduleDependency(nameof(ContentAddition))]
	[BepInPlugin(GUID, MODNAME, VERSION)]
    public sealed class AutoShotPlugin : BaseUnityPlugin
    {
        public const string
            MODNAME = "AutoShot",
            AUTHOR = "lodington",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.2.0";

		public static ConfigEntry<float> DamageCoefficient { get; set; }
		public static ConfigEntry<int> MinimumFireCount { get; set; }
		public static ConfigEntry<float> ProcCoefficient { get; set; }
		public static ConfigEntry<float> StunEnabled { get; set; }

		SkillDef _autoShot;
		SkillDef _autoShotScepter;
		static GameObject _commandoBody = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
		SkillLocator skillLocator = _commandoBody.GetComponent<SkillLocator>();

		public static AssetBundle _assets;

		private void Awake() //Called when loaded by BepInEx.
        {
			var path = System.IO.Path.GetDirectoryName(Info.Location);
			_assets = AssetBundle.LoadFromFile(System.IO.Path.Combine(path, "commandoskills"));

			DamageCoefficient = Config.Bind("Damage Coefficient", "Damage Coefficient", 0.9f, "This Will set Damage Coefficient of Sweeping Barrage Skill Default Value is 0.9f");
			MinimumFireCount = Config.Bind("minimum Fire Count", "minimum Fire Count", 10, "The minimum amount of times Sweeping barrage will shoot at targets less than X amount of enemys in range.");
			ProcCoefficient = Config.Bind("Proc CoEfficient", "Proc CoEfficient", 1.0f, "This Will set proc Coefficient of Sweeping Barrage Skill Default Value is 1.0f");

			SetupAutoShot();
			if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.ThinkInvisible.ClassicItems"))
			{
				ScepterSkillSetup();
				ScepterSetup();
			}
		}
	
		private void SetupAutoShot()
        {
			_autoShot = ScriptableObject.CreateInstance<SkillDef>();
			_autoShot.activationState = new SerializableEntityStateType(typeof(AutoShot));
			_autoShot.skillNameToken = "Auto Shot";
			_autoShot.skillName = "C_AUTO_SHOT";
			_autoShot.skillDescriptionToken = "Fires a minimum of "+ MinimumFireCount.Value + " <style=cIsUtility>auto-targeting</style> shots. Fire more shots depending on how many enemies are in your sights.";
			_autoShot.activationStateMachineName = "Weapon";
			_autoShot.baseMaxStock = 1;
			_autoShot.baseRechargeInterval = 12f;
			_autoShot.beginSkillCooldownOnSkillEnd = false;
			_autoShot.canceledFromSprinting = false;
			_autoShot.fullRestockOnAssign = true;
			_autoShot.interruptPriority = InterruptPriority.Skill;
			_autoShot.isCombatSkill = true;
			_autoShot.mustKeyPress = false;
			_autoShot.rechargeStock = 1;
			_autoShot.requiredStock = 1;
			_autoShot.cancelSprintingOnActivation = true; 
			_autoShot.stockToConsume = 1;
			_autoShot.icon = _assets.LoadAsset<Sprite>("SWEEPING_BARRAGE");

			ContentAddition.AddSkillDef(_autoShot);
			
			SkillFamily skillFamily = skillLocator.special.skillFamily;
			Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);

			skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
			{
				skillDef = _autoShot,
				unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>(),
				viewableNode = new ViewablesCatalog.Node(_autoShot.skillNameToken, false, null)
			};
        }

		private void ScepterSkillSetup()
        {
	        _autoShotScepter = ScriptableObject.CreateInstance<SkillDef>();
			_autoShotScepter.activationState = new SerializableEntityStateType(typeof(BulletHell));
			_autoShotScepter.skillNameToken = "Bullet Hell";
			_autoShotScepter.skillName = "C_AUTO_SHOT_SCEPTER";
			_autoShotScepter.skillDescriptionToken = "Fires a minimum of 25 <style=cIsUtility>auto-targeting</style> shots. Fires at everything on your screen.";
			_autoShotScepter.activationStateMachineName = "Weapon";
			_autoShotScepter.baseMaxStock = 1;
			_autoShotScepter.baseRechargeInterval = 6f;
			_autoShotScepter.beginSkillCooldownOnSkillEnd = false;
			_autoShotScepter.canceledFromSprinting = false;
			_autoShotScepter.fullRestockOnAssign = true;
			_autoShotScepter.interruptPriority = InterruptPriority.Skill;
			_autoShotScepter.isCombatSkill = true;
			_autoShotScepter.mustKeyPress = false;
			_autoShot.cancelSprintingOnActivation = false; 
			_autoShotScepter.rechargeStock = 1;
			_autoShotScepter.requiredStock = 1;
			//AUTO_SHOT_SCEPTER.shootDelay = 1f;
			_autoShotScepter.stockToConsume = 1;
			_autoShotScepter.icon = _assets.LoadAsset<Sprite>("SWEEPING_BARRAGE_SCEPTER");

			ContentAddition.AddSkillDef(_autoShotScepter);
			//LoadoutAPI.AddSkillDef(_autoShotScepter);
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private void ScepterSetup() =>
			ThinkInvisible.ClassicItems.Scepter.instance.RegisterScepterSkill(_autoShotScepter, "CommandoBody",
				SkillSlot.Special, _autoShot);
    }
}
