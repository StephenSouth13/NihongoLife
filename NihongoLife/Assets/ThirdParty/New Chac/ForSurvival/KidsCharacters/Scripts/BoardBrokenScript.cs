using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class BoardBrokenScript : MonoBehaviour
{
    private void BoardBroken ()
    {
        Destroy(this.gameObject);
    }
}
}