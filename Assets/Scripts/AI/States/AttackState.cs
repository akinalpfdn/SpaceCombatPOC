using UnityEngine;

namespace SpaceCombat.AI.States
{
    public class AttackState : IEnemyState
    {
        public string StateName => "Attack";

        public void Enter(EnemyContext ctx) { }

        public IEnemyState Execute(EnemyContext ctx)
        {
            if (ctx.Target == null)
            {
                return new PatrolState();
            }

            float distance = EnemyMovement.DistanceToTarget(ctx);

            if (distance > ctx.AttackRange * 1.3f)
            {
                return new ChaseState();
            }

            if (ctx.CanFlee && ctx.HealthPercent <= ctx.FleeHealthThreshold)
            {
                return new FleeState();
            }

            // Strafe around player at optimal combat distance
            float optimalDistance = ctx.AttackRange * 0.75f;
            EnemyMovement.StrafeAround(ctx, ctx.Target.position, optimalDistance);

            TryFire(ctx);

            return null;
        }

        public void Exit(EnemyContext ctx) { }

        private void TryFire(EnemyContext ctx)
        {
            if (ctx.WeaponController == null || ctx.Config == null) return;

            float weaponFireRate = ctx.Config.weapon?.fireRate ?? 0.2f;
            float modifiedFireRate = weaponFireRate * ctx.Config.fireRateMultiplier;

            if (Time.time >= ctx.LastFireTime + modifiedFireRate)
            {
                Vector3 aimDir = (ctx.Target.position - ctx.Transform.position).normalized;
                Vector2 aimDir2D = new Vector2(aimDir.x, aimDir.z);
                ctx.WeaponController.SetAimDirection(aimDir2D);

                if (ctx.WeaponController.TryFire())
                {
                    ctx.LastFireTime = Time.time;
                }
            }
        }
    }
}
