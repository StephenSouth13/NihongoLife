using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class BrokenStoneScript : MonoBehaviour
{
  private float timer_destroy = 0;
  
  void Update()
  {
    timer_destroy += 1 * Time.deltaTime;
    if(timer_destroy > 1.0f)
    {
      Destroy(this.gameObject);
    }
  }
}
}
