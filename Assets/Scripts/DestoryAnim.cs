using UnityEngine;

public class DestoryAnim : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
    {
        Destroy(animator.gameObject, stateInfo.length);
    }
}
