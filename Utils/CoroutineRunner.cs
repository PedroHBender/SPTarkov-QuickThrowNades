using System;
using System.Collections;
using UnityEngine;

namespace QuickThrow.Utils
{
    public class CoroutineRunner : MonoBehaviour
    {
        // Shared update hook for utility code that needs Unity's frame loop.
        public static event Action? OnUpdate;
        private static CoroutineRunner? _instance;

        public static Coroutine Run(IEnumerator routine)
        {
            // BepInEx plugins are not scene objects by default, so create a hidden
            // Unity object the first time something needs to run a coroutine.
            if (_instance == null)
            {
                var go = new GameObject("QuickThrowCoroutineRunner");
                GameObject.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CoroutineRunner>();
            }
            return _instance.StartCoroutine(routine);
        }  
        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}
