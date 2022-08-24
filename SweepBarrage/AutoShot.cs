using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoShot
{
	public class AutoShot : BaseState
	{
        public float baseDuration = 1.3f;
        private float _duration;
        private float _fireTimer;
        public float timeBetweenBullets;
        private float _firingDuration;
        public static float fieldOfView = 100;
        public float maxDistance = 150;
        
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private List<HurtBox> _targetHurtboxes = new List<HurtBox>();

        public static int minimumFireCount = AutoShotPlugin.minimumFireCount.Value;
        public int totalBulletsToFire;
        public int totalBulletsFired;
        private int _targetHurtboxIndex;

        public override void OnEnter()
        {
            base.OnEnter();
            _duration = baseDuration / attackSpeedStat;
            _firingDuration = baseDuration / attackSpeedStat;
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
                maxDistanceFilter = maxDistance,
                maxAngleFilter = fieldOfView * 0.5f
            };
            bullseyeSearch.RefreshCandidates();
            //var hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
            _targetHurtboxes = bullseyeSearch.GetResults().Where(Util.IsValid).Distinct(default(HurtBox.EntityEqualityComparer)).ToList();
            totalBulletsToFire = Mathf.Max(_targetHurtboxes.Count, minimumFireCount);
            timeBetweenBullets = _firingDuration / totalBulletsToFire;

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

                if (_targetHurtboxIndex >= _targetHurtboxes.Count) _targetHurtboxIndex = 0;
                HurtBox hurtBox = _targetHurtboxes[_targetHurtboxIndex];
                
                if (_targetHurtboxes.Count > 0)
                    if (hurtBox)
                    {
                        _targetHurtboxIndex++;
                        Vector3 direction = hurtBox.transform.position - aimRay.origin;
                        //inputBank.aimDirection = direction;
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
                                    damage = characterBody.damage * AutoShotPlugin.damageCoefficient.Value,
                                    force = 3,
                                    falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                                    tracerEffectPrefab = tracerEffectPrefab,
                                    hitEffectPrefab = hitEffectPrefab,
                                    isCrit = RollCrit(),
                                    HitEffectNormal = false,
                                    damageType = DamageType.Stun1s,
                                    stopperMask = LayerIndex.world.mask,
                                    smartCollision = true,
                                    maxDistance = maxDistance
                                }.Fire();
                    }

                //Debug.Log("Bullet " + totalBulletsFired);
                totalBulletsFired++;
            }
          
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            _fireTimer -= Time.fixedDeltaTime;
            if (_fireTimer <= 0f)
            {
                Fire();
                _fireTimer += timeBetweenBullets;
            }
            if (fixedAge >= _duration && isAuthority) outer.SetNextStateToMain();
        }
        public bool isListEmpty(List<HurtBox> list) => !list.Any();
        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}

