using UnityEngine;

namespace Simulation01.Weather
{
    /// <summary>
    /// Used to destroy parent object when the state ends
    /// </summary>
    public class CloudFadeOutBehaviour : StateMachineBehaviour
    {
        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Destroy(animator.gameObject.transform.parent.gameObject);
        }
    }
}