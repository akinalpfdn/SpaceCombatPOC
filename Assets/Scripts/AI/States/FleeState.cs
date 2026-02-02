namespace SpaceCombat.AI.States
{
    public class FleeState : IEnemyState
    {
        public string StateName => "Flee";

        public void Enter(EnemyContext ctx) { }

        public IEnemyState Execute(EnemyContext ctx)
        {
            if (ctx.Target == null)
            {
                return new PatrolState();
            }

            EnemyMovement.MoveAwayFrom(ctx, ctx.Target.position);

            float distance = EnemyMovement.DistanceToTarget(ctx);
            bool safeDistance = distance > ctx.DetectionRange * 2f;
            bool healthRecovered = ctx.HealthPercent > ctx.FleeHealthThreshold;

            if (safeDistance || healthRecovered)
            {
                return new PatrolState();
            }

            return null;
        }

        public void Exit(EnemyContext ctx) { }
    }
}
