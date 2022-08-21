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
        public float baseDuration = 1.3f;
        private float duration;
        private float fireTimer;
        public float timeBetweenBullets;
        private float firingDuration;
        public static float fieldOfView = float.MaxValue;
        public float maxDistance = float.MaxValue;

        public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");

        private List<HurtBox> targetHurtboxes = new List<HurtBox>();

        public static int minimumFireCount = 25;
        public int totalBulletsToFire;
        public int totalBulletsFired;
        private int targetHurtboxIndex;
        public float damage = 1.3f;


        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            this.firingDuration = this.baseDuration / this.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.characterBody.SetAimTimer(3f);
            base.PlayAnimation("Gesture, Additive", "FireSweepBarrage", "FireSweepBarrage.playbackRate", this.baseDuration * 1.1f);
            base.PlayAnimation("Gesture, Override", "FireSweepBarrage", "FireSweepBarrage.playbackRate", this.baseDuration * 1.1f);
            var bullseyeSearch = new RoR2.BullseyeSearch();
            bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(base.GetTeam());
            bullseyeSearch.filterByLoS = true;
            bullseyeSearch.searchOrigin = aimRay.origin;
            bullseyeSearch.searchDirection = aimRay.direction;
            bullseyeSearch.sortMode = RoR2.BullseyeSearch.SortMode.DistanceAndAngle;
            bullseyeSearch.maxDistanceFilter = maxDistance;
            bullseyeSearch.maxAngleFilter = AutoShot.fieldOfView * 0.5f;
            bullseyeSearch.RefreshCandidates();
            //var hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
            this.targetHurtboxes = bullseyeSearch.GetResults().Where(new Func<HurtBox, bool>(Util.IsValid)).Distinct(default(HurtBox.EntityEqualityComparer)).ToList<HurtBox>();
            this.totalBulletsToFire = Mathf.Max(this.targetHurtboxes.Count, minimumFireCount);
            this.timeBetweenBullets = (this.firingDuration / (float)this.totalBulletsToFire);

        }
        private void Fire()
        {
            if (isListEmpty(this.targetHurtboxes))
            {
                return;
            }
            if (this.totalBulletsFired < this.totalBulletsToFire)
            {
                var localUser = RoR2.LocalUserManager.GetFirstLocalUser();
                var controller = localUser.cachedMasterController;
                if (!controller)
                    return;
                var body = controller.master.GetBody();
                if (!body)
                    return;
                var inputBank = body.GetComponent<RoR2.InputBankTest>();
                var aimRay = new Ray(inputBank.aimOrigin, inputBank.aimDirection);

                if (this.targetHurtboxIndex >= this.targetHurtboxes.Count)
                {
                    this.targetHurtboxIndex = 0;
                }
                HurtBox hurtBox = this.targetHurtboxes[this.targetHurtboxIndex];
                if (this.targetHurtboxes.Count > 0)
                {
                    if (hurtBox)
                    {
                        this.targetHurtboxIndex++;
                        Vector3 direction = hurtBox.transform.position - aimRay.origin;
                        //inputBank.aimDirection = direction;
                        HealthComponent healthComponent = hurtBox.healthComponent;
                        if (healthComponent)
                        {
                            if (base.isAuthority)
                            {
                                new BulletAttack
                                {
                                    owner = base.gameObject,
                                    weapon = base.gameObject,
                                    origin = aimRay.origin,
                                    muzzleName = "MuzzleRight",
                                    aimVector = direction,
                                    minSpread = 0f,
                                    maxSpread = base.characterBody.spreadBloomAngle,
                                    bulletCount = 1U,
                                    procCoefficient = AutoShotPlugin.procCoefficient.Value,
                                    damage = base.characterBody.damage * damage,
                                    force = 3,
                                    falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                                    tracerEffectPrefab = this.tracerEffectPrefab,
                                    hitEffectPrefab = this.hitEffectPrefab,
                                    isCrit = base.RollCrit(),
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
                this.totalBulletsFired++;
            }

        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.fireTimer -= Time.fixedDeltaTime;
            if (this.fireTimer <= 0f)
            {
                this.Fire();
                this.fireTimer += this.timeBetweenBullets;
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
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

