// ============================================
// AUTO DESTROY ANIMATION
// Destroys GameObject when animation finishes
// Attach to animated VFX prefabs
// ============================================

using UnityEngine;

namespace SpaceCombat.VFX
{
    /// <summary>
    /// Automatically destroys this GameObject when the attached animation finishes.
    /// Use this for one-shot VFX like explosions.
    /// </summary>
    public class AutoDestroyAnimation : MonoBehaviour
    {
        private void Start()
        {
            // Destroy when animation completes
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                // Get animation length
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                float animationLength = stateInfo.length;

                Destroy(gameObject, animationLength);
            }
            else
            {
                // Fallback: destroy after default time if no animator
                Destroy(gameObject, 1f);
            }
        }
    }
}
