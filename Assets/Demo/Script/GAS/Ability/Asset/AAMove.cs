﻿using System;
using UnityEngine;

namespace GAS.Runtime
{
    [CreateAssetMenu(fileName = "Move", menuName = "GAS/Ability/Move")]
    public class AAMove: AbilityAsset
    {
        public override Type AbilityType()
        {
            return typeof(Move);
        }
    }
}