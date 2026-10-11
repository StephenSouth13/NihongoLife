using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class BoyNPCScript : MonoBehaviour
{
  private Animator _Animator;
  private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
  private static readonly int DamageTreatedState = Animator.StringToHash("Base Layer.damage_treated");
  private static readonly int TreatedTag = Animator.StringToHash("Treated");
  private static readonly int CaughtDownState = Animator.StringToHash("Base Layer.caught_down");
  private static readonly int CaughtStandUpState = Animator.StringToHash("Base Layer.caught_standup");
  private static readonly int CaughtTag = Animator.StringToHash("Caught");
  private static readonly int CowardState = Animator.StringToHash("Base Layer.coward");
  private static readonly int CabinetSurprisedState = Animator.StringToHash("Base Layer.cabinet_surprised");

  void Start()
  {
    _Animator = this.GetComponent<Animator>();
  }

  void Update()
  {
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash == TreatedTag
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DamageTreatedState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
    }
    else if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash == CaughtTag
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CaughtDownState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(CaughtStandUpState, 0.1f, 0, 0);
    }
    else if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash == CaughtTag
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CaughtStandUpState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(CowardState, 0.3f, 0, 0);
    }
    else if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CabinetSurprisedState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
    }
  }
}
}
