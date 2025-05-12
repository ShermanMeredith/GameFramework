using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayTable
{
    public class PTSingletonMonobehaviour : MonoBehaviour
    {
        public static PTSingletonMonobehaviour singleton = null;

        protected virtual void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

    }
}
