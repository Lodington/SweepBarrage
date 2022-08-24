using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;

using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AutoShot
{
    class BulletHell : BaseState
    {
        private float baseDuration = 1.3f;
        private float _duration;
        private float _fireTimer;
        private float _timeBetweenBullets;
        private float _firingDuration;
        private static float _fieldOfView = float.MaxValue;
        private float maxDistance = float.MaxValue;
        
        private readonly GameObject _hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        private readonly GameObject _tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private List<HurtBox> _targetHurtboxes = new List<HurtBox>();

        private static int minimumFireCount = 25;
        private int _totalBulletsToFire;
        private int _totalBulletsFired;
        private int _targetHurtboxIndex;
        private float damage = 1.3f;


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
                maxAngleFilter = AutoShot.fieldOfView * 0.5f
            };
            bullseyeSearch.RefreshCandidates();
            //var hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
            _targetHurtboxes = bullseyeSearch.GetResults().Where(Util.IsValid).Distinct(default(HurtBox.EntityEqualityComparer)).ToList();
            _totalBulletsToFire = Mathf.Max(_targetHurtboxes.Count, minimumFireCount);
            _timeBetweenBullets = _firingDuration / _totalBulletsToFire;

        }
        private void Fire()
        {
            if (isListEmpty(_targetHurtboxes))
            {
                return;
            }
            if (_totalBulletsFired < _totalBulletsToFire)
            {
                var localUser = LocalUserManager.GetFirstLocalUser();
                var controller = localUser.cachedMasterController;
                if (!controller) return;
                var body = controller.master.GetBody();
                if (!body) return;
                var bank = body.GetComponent<InputBankTest>();
                var aimRay = new Ray(bank.aimOrigin, bank.aimDirection);

                if (_targetHurtboxIndex >= _targetHurtboxes.Count) _targetHurtboxIndex = 0;
                
                HurtBox hurtBox = _targetHurtboxes[_targetHurtboxIndex];
                if (_targetHurtboxes.Count > 0)
                {
                    if (hurtBox)
                    {
                        _targetHurtboxIndex++;
                        Vector3 direction = hurtBox.transform.position - aimRay.origin;
                        //inputBank.aimDirection = direction;
                        HealthComponent hurtBoxHealthComponent = hurtBox.healthComponent;
                        if (hurtBoxHealthComponent)
                        {
                            if (isAuthority)
                            {
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
                                    damage = characterBody.damage * damage,
                                    force = 3,
                                    falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                                    tracerEffectPrefab = _tracerEffectPrefab,
                                    hitEffectPrefab = _hitEffectPrefab,
                                    isCrit = RollCrit(),
                                    HitEffectNormal = false,
                                    damageType = DamageType.IgniteOnHit | DamageType.Stun1s,
                                    stopperMask = LayerIndex.world.mask,
                                    smartCollision = true,
                                    maxDistance = maxDistance
                                }.Fire();
                            }
                        }
                    }
                }
                //Debug.Log("Bullet " + totalBulletsFired);
                _totalBulletsFired++;
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
                _fireTimer += _timeBetweenBullets;
            }
            if (fixedAge >= _duration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }
        public bool isListEmpty(List<HurtBox> list)
        {
            return !list.Any();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}

