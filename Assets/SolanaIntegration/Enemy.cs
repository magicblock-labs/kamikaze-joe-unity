using System;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Chainstrike
{
    [Serializable]
    public class Enemy: MonoBehaviour
    {
        [SerializeField]
        public int energy;
    }
}