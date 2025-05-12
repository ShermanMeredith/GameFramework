using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace PlayTable
{
    public class PTFlatGroupContent : MonoBehaviour
    {
        private PTFlatGroups groups;

        private void Awake()
        {
            groups = GetComponentInParent<PTFlatGroups>();
        }

        private void OnTransformChildrenChanged()
        {
            if (groups)
            {
                groups.Calibrate(true);
            }
        }
    }
}