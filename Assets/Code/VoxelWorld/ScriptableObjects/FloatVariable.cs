using UnityEngine;
using System.Collections;
using System;

namespace VoxelWorld
{
    /// <summary>
    /// Example of a shared variable which can be referenced everywhere, useful for i.e. config values
    /// </summary>
    [CreateAssetMenu(fileName = "FloatVariable", menuName = "ScriptableObjects/FloatVariable")]
    public class FloatVariable : ScriptableObject, ISerializationCallbackReceiver
    {
        public float initialValue => _initialValue;
        [SerializeField]
        private float _initialValue;

        [NonSerialized]
        public float runtimeValue;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            runtimeValue = _initialValue;
        }
    }
}