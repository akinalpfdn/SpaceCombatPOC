using UnityEngine;

namespace SpaceCombat.AI.States
{
    public class IdleState : IEnemyState
    {
        public string StateName => "Idle";

        public void Enter(EnemyContext ctx)
        {
            ctx.StateTimer = ctx.IdleTime;
        }

        public IEnemyState Execute(EnemyContext ctx)
        {
            ctx.StateTimer -= Time.deltaTime;

            if (EnemyMovement.IsTargetInRange(ctx, ctx.DetectionRange))
            {
                return new ChaseState();
            }

            if (ctx.StateTimer <= 0)
            {
                return new PatrolState();
            }

            return null;
        }

        public void Exit(EnemyContext ctx) { }
    }
}
