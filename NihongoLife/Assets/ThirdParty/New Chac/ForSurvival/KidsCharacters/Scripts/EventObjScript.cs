using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class EventObjScript : MonoBehaviour
{
  private Animator _Animator;
  private GameObject _Effect;

  void Start()
  {
    _Animator = this.GetComponent<Animator>();
    _Effect = Resources.Load<GameObject>("Prefabs/Effect/EffectCube");
  }
  private void InsertKey ()
  {
    _Effect = Instantiate<GameObject>(_Effect,this.gameObject.transform);
  }
}
}
