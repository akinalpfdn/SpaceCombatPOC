using UnityEngine;

namespace StarReapers.AI
{
    /// <summary>
    /// Handles all enemy movement operations.
    /// Extracted from Enemy.cs for Single Responsibility.
    /// Used by AI states via EnemyContext.
    /// </summary>
    public static class EnemyMovement
    {
        private const float LERP_SPEED = 5f;

        /// <summary>
        /// Move towards a target position on the XZ plane.
        /// </summary>
        public static void MoveTowards(EnemyContext ctx, Vector3 position)
        {
            Vector3 targetPos = new Vector3(position.x, 0, position.z);
            Vector3 direction = (targetPos - ctx.Transform.position).normalized;

            Vector3 targetVelocity = direction * ctx.MoveSpeed;
            ctx.Rigidbody.linearVelocity = Vector3.Lerp(
                ctx.Rigidbody.linearVelocity,
                targetVelocity,
                LERP_SPEED * Time.deltaTime
            );

            RotateTowards(ctx, position);
        }

        /// <summary>
        /// Move away from a target position on the XZ plane.
        /// </summary>
        public static void MoveAwayFrom(EnemyContext ctx, Vector3 position)
        {
            Vector3 targetPos = new Vector3(position.x, 0, position.z);
            Vector3 direction = (ctx.Transform.position - targetPos).normalized;

            Vector3 targetVelocity = direction * ctx.MoveSpeed;
            ctx.Rigidbody.linearVelocity = Vector3.Lerp(
                ctx.Rigidbody.linearVelocity,
                targetVelocity,
                LERP_SPEED * Time.deltaTime
            );
        }

        /// <summary>
        /// Strafe around target - orbiting behavior.
        /// </summary>
        public static void StrafeAround(EnemyContext ctx, Vector3 targetPosition, float desiredDistance)
        {
            Vector3 targetPos = new Vector3(targetPosition.x, 0, targetPosition.z);
            Vector3 toTarget = targetPos - ctx.Transform.position;
            float currentDistance = toTarget.magnitude;
            Vector3 toTargetDir = toTarget.normalized;

            RotateTowards(ctx, targetPosition);

            // Perpendicular direction for strafing on XZ plane
            Vector3 strafeDir = new Vector3(-toTargetDir.z, 0, toTargetDir.x) * ctx.StrafeDirection;

            // Randomly flip direction occasionally
            if (Random.value < 0.005f)
            {
                ctx.StrafeDirection *= -1;
            }

            Vector3 moveDirection;
            float speedModifier;

            if (currentDistance < desiredDistance * 0.8f)
            {
                // Too close - back away with slight strafe
                moveDirection = (-toTargetDir * 0.7f + strafeDir * 0.3f).normalized;
                speedModifier = 0.8f;
            }
            else if (currentDistance > desiredDistance * 1.2f)
            {
                // Too far - move closer with slight strafe
                moveDirection = (toTargetDir * 0.7f + strafeDir * 0.3f).normalized;
                speedModifier = 0.6f;
            }
            else
            {
                // At good distance - mostly strafe/orbit
                moveDirection = strafeDir;
                speedModifier = 0.5f;
            }

            float targetSpeed = ctx.MoveSpeed * speedModifier;
            Vector3 targetVelocity = moveDirection * targetSpeed;

            ctx.Rigidbody.linearVelocity = Vector3.Lerp(
                ctx.Rigidbody.linearVelocity,
                targetVelocity,
                LERP_SPEED * Time.deltaTime
            );
        }

        /// <summary>
        /// Rotate to face a position on the XZ plane.
        /// </summary>
        public static void RotateTowards(EnemyContext ctx, Vector3 position)
        {
            Vector3 direction = position - ctx.Transform.position;
            direction.y = 0;
            direction.Normalize();

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                ctx.Transform.rotation = Quaternion.Slerp(
                    ctx.Transform.rotation,
                    targetRotation,
                    ctx.RotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Check if target is within range.
        /// </summary>
        public static bool IsTargetInRange(EnemyContext ctx, float range)
        {
            if (ctx.Target == null) return false;
            return Vector3.Distance(ctx.Transform.position, ctx.Target.position) <= range;
        }

        /// <summary>
        /// Get distance to target.
        /// </summary>
        public static float DistanceToTarget(EnemyContext ctx)
        {
            if (ctx.Target == null) return float.MaxValue;
            return Vector3.Distance(ctx.Transform.position, ctx.Target.position);
        }

        /// <summary>
        /// Get a random patrol point around spawn position.
        /// </summary>
        public static Vector3 GetRandomPatrolPoint(EnemyContext ctx)
        {
            Vector2 randomOffset = Random.insideUnitCircle * ctx.PatrolRadius;
            return ctx.SpawnPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
        }
    }
}
