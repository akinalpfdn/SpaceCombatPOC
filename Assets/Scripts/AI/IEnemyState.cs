namespace SpaceCombat.AI
{
    /// <summary>
    /// Interface for enemy AI states.
    /// Each state encapsulates behavior for one phase of enemy AI.
    /// </summary>
    public interface IEnemyState
    {
        /// <summary>
        /// Called when entering this state.
        /// </summary>
        void Enter(EnemyContext context);

        /// <summary>
        /// Called every frame while in this state.
        /// Returns the next state type, or null to stay in current state.
        /// </summary>
        IEnemyState Execute(EnemyContext context);

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        void Exit(EnemyContext context);

        /// <summary>
        /// State name for debugging.
        /// </summary>
        string StateName { get; }
    }
}
