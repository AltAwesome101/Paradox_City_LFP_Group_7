using System.Collections.Generic;
using UnityEngine;

namespace Forestlevel
{
    public static class GamePause
    {
        static readonly HashSet<object> _owners = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { _owners.Clear(); Time.timeScale = 1f; } // survives disabled domain reload

        public static void Request(object owner) { _owners.Add(owner); Apply(); }
        public static void Release(object owner) { _owners.Remove(owner); Apply(); }
        static void Apply() => Time.timeScale = _owners.Count > 0 ? 0f : 1f;
    }
}



