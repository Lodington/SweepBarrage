using RoR2;
namespace AutoShot
{
    class BulletHell : AutoShot
    {
        public override float FieldOfView => float.MaxValue;
        public override float MAXDistance => float.MaxValue;
        public override DamageType DamageType => DamageType.Stun1s | DamageType.IgniteOnHit;
        public override int MinimumFireCount => 25;
        public override float Damage => 1.3f;
    }
}

