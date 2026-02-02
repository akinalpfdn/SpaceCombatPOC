namespace SpaceCombat.AI.States
{
    public class ChaseState : IEnemyState
    {
        public string StateName => "Chase";

        public void Enter(EnemyContext ctx) { }

        public IEnemyState Execute(EnemyContext ctx)
        {
            if (ctx.Target == null)
            {
                return new PatrolState();
            }

            float distance = EnemyMovement.DistanceToTarget(ctx);

            if (distance > ctx.DetectionRange * 1.5f)
            {
                return new PatrolState();
            }

            if (distance <= ctx.AttackRange)
            {
                return new AttackState();
            }

            if (ShouldFlee(ctx))
            {
                return new FleeState();
            }

            EnemyMovement.MoveTowards(ctx, ctx.Target.position);
            EnemyMovement.RotateTowards(ctx, ctx.Target.position);

            return null;
        }

        public void Exit(EnemyContext ctx) { }

        private bool ShouldFlee(EnemyContext ctx)
        {
            return ctx.CanFlee && ctx.HealthPercent <= ctx.FleeHealthThreshold;
        }
    }
}
