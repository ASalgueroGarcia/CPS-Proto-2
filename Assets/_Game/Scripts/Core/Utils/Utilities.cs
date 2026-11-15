using UnityEngine;

namespace Core.Utils
{
    public static class Utilities
    {
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.TryGetComponent<T>(out var component) ? component : obj.AddComponent<T>();
    }
}
