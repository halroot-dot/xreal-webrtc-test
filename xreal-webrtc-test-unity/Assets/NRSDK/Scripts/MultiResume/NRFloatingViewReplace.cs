/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/         
* 
*****************************************************************************/

namespace NRKernal
{
    using System;
    using UnityEngine;

    public class NRFloatingViewReplace : MonoBehaviour
    {
        public virtual IFloatingViewProxy CreateFloatingViewProxy()
        {
            return new NRDefaultFloatingViewProxy();
        }
    }
}