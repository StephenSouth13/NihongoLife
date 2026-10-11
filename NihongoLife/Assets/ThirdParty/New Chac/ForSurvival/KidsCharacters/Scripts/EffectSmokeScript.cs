using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class EffectSmokeScript : MonoBehaviour
{
  private float timer_destroy = 0;
  void Update()
  {
    timer_destroy += 1 * Time.deltaTime;
    if(timer_destroy > 3)
    {
      Destroy(this.gameObject);
    }
  }
}
}
