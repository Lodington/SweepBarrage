using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityStates.Commando.CommandoWeapon;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoShot
{
	public class AutoShot : BaseState
	{
        public float baseDuration = 1.3f;
        public float duration;
        public float fireTimer;
        public float timeBetweenBullets;
        public float firingDuration;
        public virtual float FieldOfView => 100;
        public virtual float MAXDistance => 150;
        public virtual DamageType DamageType => DamageType.Stun1s;
        
        public static GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private List<HurtBox> _targetHurtboxes = new List<HurtBox>();

        public virtual int MinimumFireCount => AutoShotPlugin.minimumFireCount.Value;
        public virtual float Damage => AutoShotPlugin.damageCoefficient.Value;
        public virtual float RecoilAmplitude => 2f;
        public int totalBulletsToFire;
        public int totalBulletsFired;
        private int _targetHurtboxIndex;
        private bool _hasSuccessfullyFoundTarget;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            firingDuration = baseDuration / attackSpeedStat;
            Ray aimRay = GetAimRay();
            characterBody.SetAimTimer(3f);
            PlayAnimation("Gesture, Additive", "FireSweepBarrage", "FireSweepBarrage.playbackRate", baseDuration * 1.1f);
            PlayAnimation("Gesture, Override", "FireSweepBarrage", "FireSweepBarrage.playbackRate", baseDuration * 1.1f);
            var bullseyeSearch = new BullseyeSearch
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam()),
                filterByLoS = true,
                searchOrigin = aimRay.origin,
                searchDirection = aimRay.direction,
                sortMode = BullseyeSearch.SortMode.DistanceAndAngle,
                maxDistanceFilter = MAXDistance,
                maxAngleFilter = FieldOfView * 0.5f
            };
            bullseyeSearch.RefreshCandidates();
            //var hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
            _targetHurtboxes = bullseyeSearch.GetResults().Where(Util.IsValid).Distinct(default(HurtBox.EntityEqualityComparer)).ToList();
            totalBulletsToFire = Mathf.Max(_targetHurtboxes.Count, MinimumFireCount);
            timeBetweenBullets = firingDuration / totalBulletsToFire;
        }
        private void Fire()
        {
            if (isListEmpty(_targetHurtboxes)) return;
            if (totalBulletsFired < totalBulletsToFire)
            {
                var localUser = LocalUserManager.GetFirstLocalUser();
                var controller = localUser.cachedMasterController;
                if (!controller) return;
                var body = controller.master.GetBody();
                if (!body) return;
                var inputBankTest = body.GetComponent<InputBankTest>();
                var aimRay = new Ray(inputBankTest.aimOrigin, inputBankTest.aimDirection);

                AddRecoil(-0.8f * RecoilAmplitude, -1f * RecoilAmplitude, -0.1f * RecoilAmplitude, 0.15f * RecoilAmplitude);
                
                if (_targetHurtboxIndex >= _targetHurtboxes.Count) _targetHurtboxIndex = 0;
                HurtBox hurtBox = _targetHurtboxes[_targetHurtboxIndex];
                
                if (_targetHurtboxes.Count > 0)
                    if (hurtBox)
                    {
                        _hasSuccessfullyFoundTarget = true;
                        _targetHurtboxIndex++;
                        Vector3 direction = hurtBox.transform.position - aimRay.origin;
                        inputBank.aimDirection = direction;
                        HealthComponent boxHealthComponent = hurtBox.healthComponent;
                        if (boxHealthComponent)
                            if (isAuthority)
                                new BulletAttack
                                {
                                    owner = gameObject,
                                    weapon = gameObject,
                                    origin = aimRay.origin,
                                    muzzleName = "MuzzleRight",
                                    aimVector = direction,
                                    minSpread = 0f,
                                    maxSpread = characterBody.spreadBloomAngle,
                                    bulletCount = 1U,
                                    procCoefficient = AutoShotPlugin.procCoefficient.Value,
                                    damage = characterBody.damage * Damage,
                                    force = 3,
                                    falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                                    tracerEffectPrefab = tracerEffectPrefab,
                                    hitEffectPrefab = hitEffectPrefab,
                                    isCrit = RollCrit(),
                                    HitEffectNormal = false,
                                    damageType = DamageType,
                                    stopperMask = LayerIndex.world.mask,
                                    smartCollision = true,
                                    maxDistance = MAXDistance
                                }.Fire();
                        Util.PlaySound(FireBarrage.fireBarrageSoundString, gameObject);
                        characterBody.AddSpreadBloom(FireBarrage.spreadBloomValue);
                    }
                //Debug.Log("Bullet " + totalBulletsFired);
                totalBulletsFired++;
            }
          
        }
        public override void OnExit()
        {
            base.OnExit();
            if (!_hasSuccessfullyFoundTarget && NetworkServer.active) skillLocator.special.AddOneStock();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            fireTimer -= Time.fixedDeltaTime;
            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer += timeBetweenBullets;
            }
            if (fixedAge >= duration && isAuthority) outer.SetNextStateToMain();
        }
        public bool isListEmpty(List<HurtBox> list) => !list.Any();
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}

