using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRKernal.NRExamples
{
    public class Rotate : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            transform.localRotation *= Quaternion.Euler(0, 0, -1);
        }
    }
}