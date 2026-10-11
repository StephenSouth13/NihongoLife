using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class BoyNPC2Script : MonoBehaviour
{
    private Animator _Animator;
    private CharacterController _Ctrl;
    private GameObject _Enemy;
    private Animator _EnemyAnimator;
    // Hash
    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int CowardState = Animator.StringToHash("Base Layer.coward");
    private static readonly int CaughtDownState = Animator.StringToHash("Base Layer.caught_down");
    private static readonly int CaughtStandUpState = Animator.StringToHash("Base Layer.caught_standup");

    void Start()
    {
        _Animator = this.GetComponent<Animator>();
        _Ctrl = this.GetComponent<CharacterController>();
        if(GameObject.Find("enemy0_Humanoid") != null)
        {
            _Enemy = GameObject.Find("enemy0_Humanoid");
        }
        else if(GameObject.Find("enemy0") != null)
        {
            _Enemy = GameObject.Find("enemy0");
        }

        _EnemyAnimator = _Enemy.GetComponent<Animator>();
    }
    void Update()
    {
        Vector3 pos1 = this.transform.position;
        Vector3 pos2 = _Enemy.transform.position;
        float distance = (pos1.x - pos2.x)*(pos1.x - pos2.x)+(pos1.y - pos2.y)*(pos1.y - pos2.y)+(pos1.z - pos2.z)*(pos1.z - pos2.z);
        if(distance < 10)
        {
            if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == IdleState
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(CowardState, 0.1f, 0, 0);
            }
        }
        else
        {
            if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CowardState
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(IdleState, 0.1f, 0, 0);
            }
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f
            && !_Animator.IsInTransition(0)
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CaughtDownState)
        {
            _Animator.CrossFade(CaughtStandUpState, 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f
            && !_Animator.IsInTransition(0)
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CaughtStandUpState)
        {
            _Animator.CrossFade(CowardState, 0.1f, 0, 0);
        }
    }
}
}