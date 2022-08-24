using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using UnityEngine;
/*
using R2API;
using R2API.Utils;
*/
namespace AutoShot
{
	[BepInDependency("com.ThinkInvisible.ClassicItems", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInPlugin(GUID, MODNAME, VERSION)]
    public sealed class AutoShotPlugin : BaseUnityPlugin
    {
        public const string
            MODNAME = "AutoShot",
            AUTHOR = "lodington",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.2.0";

		public static ConfigEntry<float> damageCoefficient { get; set; }
		public static ConfigEntry<int> minimumFireCount { get; set; }
		public static ConfigEntry<float> procCoefficient { get; set; }
		public static ConfigEntry<float> stunEnabled { get; set; }

		SkillDef AUTO_SHOT;
		SkillDef AUTO_SHOT_SCEPTER;
		static GameObject CommandoBody = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
		public static AssetBundle assets;
		SkillLocator skillLocator = CommandoBody.GetComponent<SkillLocator>();


		public static AssetBundle _assets;

		private void Awake() //Called when loaded by BepInEx.
        {
			var path = System.IO.Path.GetDirectoryName(Info.Location);
			_assets = AssetBundle.LoadFromFile(System.IO.Path.Combine(path, "commandoskills"));

			damageCoefficient = Config.Bind("Damage Coefficient", "Damage Coefficient", 0.9f, "This Will set Damage Coefficient of Sweeping Barrage Skill Default Value is 0.9f");
			minimumFireCount = Config.Bind("minimum Fire Count", "minimum Fire Count", 10, "The minimum amount of times Sweeping barrage will shoot at targets less than X amount of enemys in range.");
			procCoefficient = Config.Bind("Proc CoEfficient", "Proc CoEfficient", 1.0f, "This Will set proc Coefficient of Sweeping Barrage Skill Default Value is 1.0f");

			setupAutoShot();
			if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.ThinkInvisible.ClassicItems"))
			{
				ScepterSkillSetup();
				ScepterSetup();
			}
		}
	
		private void setupAutoShot()
        {
			AUTO_SHOT = ScriptableObject.CreateInstance<SkillDef>();
			AUTO_SHOT.activationState = new SerializableEntityStateType(typeof(AutoShot));
			AUTO_SHOT.skillNameToken = "Auto Shot";
			AUTO_SHOT.skillName = "C_AUTO_SHOT";
			AUTO_SHOT.skillDescriptionToken = "Fires a minimum of "+ minimumFireCount.Value + " <style=cIsUtility>auto-targeting</style> shots. Fire more shots depending on how many enemies are in your sights.";
			AUTO_SHOT.activationStateMachineName = "Weapon";
			AUTO_SHOT.baseMaxStock = 1;
			AUTO_SHOT.baseRechargeInterval = 12f;
			AUTO_SHOT.beginSkillCooldownOnSkillEnd = false;
			AUTO_SHOT.canceledFromSprinting = false;
			AUTO_SHOT.fullRestockOnAssign = true;
			AUTO_SHOT.interruptPriority = InterruptPriority.Skill;
			AUTO_SHOT.isCombatSkill = true;
			AUTO_SHOT.mustKeyPress = false;
			AUTO_SHOT.rechargeStock = 1;
			AUTO_SHOT.requiredStock = 1;
			AUTO_SHOT.cancelSprintingOnActivation = true; 
			AUTO_SHOT.stockToConsume = 1;
			AUTO_SHOT.icon = _assets.LoadAsset<Sprite>("SWEEPING_BARRAGE");

			LoadoutAPI.AddSkillDef(AUTO_SHOT);
			SkillFamily skillFamily = skillLocator.special.skillFamily;
			Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);

			skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
			{
				skillDef = AUTO_SHOT,
				unlockableName = "",
				viewableNode = new ViewablesCatalog.Node(AUTO_SHOT.skillNameToken, false, null)
			};
        }

		private void ScepterSkillSetup()
        {
	        AUTO_SHOT_SCEPTER = ScriptableObject.CreateInstance<SkillDef>();
			AUTO_SHOT_SCEPTER.activationState = new SerializableEntityStateType(typeof(BulletHell));
			AUTO_SHOT_SCEPTER.skillNameToken = "Bullet Hell";
			AUTO_SHOT_SCEPTER.skillName = "C_AUTO_SHOT_SCEPTER";
			AUTO_SHOT_SCEPTER.skillDescriptionToken = "Fires a minimum of 25 <style=cIsUtility>auto-targeting</style> shots. Fires at everything on your screen.";
			AUTO_SHOT_SCEPTER.activationStateMachineName = "Weapon";
			AUTO_SHOT_SCEPTER.baseMaxStock = 1;
			AUTO_SHOT_SCEPTER.baseRechargeInterval = 6f;
			AUTO_SHOT_SCEPTER.beginSkillCooldownOnSkillEnd = false;
			AUTO_SHOT_SCEPTER.canceledFromSprinting = false;
			AUTO_SHOT_SCEPTER.fullRestockOnAssign = true;
			AUTO_SHOT_SCEPTER.interruptPriority = InterruptPriority.Skill;
			AUTO_SHOT_SCEPTER.isCombatSkill = true;
			AUTO_SHOT_SCEPTER.mustKeyPress = false;
			AUTO_SHOT.cancelSprintingOnActivation = false; 
			AUTO_SHOT_SCEPTER.rechargeStock = 1;
			AUTO_SHOT_SCEPTER.requiredStock = 1;
			//AUTO_SHOT_SCEPTER.shootDelay = 1f;
			AUTO_SHOT_SCEPTER.stockToConsume = 1;
			AUTO_SHOT_SCEPTER.icon = _assets.LoadAsset<Sprite>("SWEEPING_BARRAGE_SCEPTER");

			LoadoutAPI.AddSkillDef(AUTO_SHOT_SCEPTER);
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private void ScepterSetup()
		{
			ThinkInvisible.ClassicItems.Scepter.instance.RegisterScepterSkill(AUTO_SHOT_SCEPTER, "CommandoBody",
				SkillSlot.Special, AUTO_SHOT);
		}
		
	}
}
