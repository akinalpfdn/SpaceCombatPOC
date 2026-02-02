using UnityEngine;

namespace SpaceCombat.AI.States
{
    public class PatrolState : IEnemyState
    {
        public string StateName => "Patrol";

        public void Enter(EnemyContext ctx)
        {
            ctx.PatrolPoint = EnemyMovement.GetRandomPatrolPoint(ctx);
        }

        public IEnemyState Execute(EnemyContext ctx)
        {
            if (EnemyMovement.IsTargetInRange(ctx, ctx.DetectionRange))
            {
                return new ChaseState();
            }

            EnemyMovement.MoveTowards(ctx, ctx.PatrolPoint);

            if (Vector3.Distance(ctx.Transform.position, ctx.PatrolPoint) < 1f)
            {
                return new IdleState();
            }

            return null;
        }

        public void Exit(EnemyContext ctx) { }
    }
}
