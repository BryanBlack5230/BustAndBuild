using UnityEngine;
using BB.Resources;

namespace BB.Combat
{
    [RequireComponent(typeof(Health))]
    public class Grabbable : MonoBehaviour
    {
       private bool grabbed = false;
        public void Grab()
        {
            grabbed = true;
        }
    }
}


