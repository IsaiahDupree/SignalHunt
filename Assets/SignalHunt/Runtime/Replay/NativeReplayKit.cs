using System.Runtime.InteropServices;
using UnityEngine;

namespace SignalHunt.Replay
{
    public static class NativeReplayKit
    {
        public static bool IsAvailable
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return SignalHuntReplay_IsAvailable();
#else
                return false;
#endif
            }
        }

        public static void Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
            SignalHuntReplay_Start();
#else
            Debug.Log("ReplayKit export is available in an iOS device build. Playing cinematic preview only.");
#endif
        }

        public static void StopAndPresent()
        {
#if UNITY_IOS && !UNITY_EDITOR
            SignalHuntReplay_StopAndPresent();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool SignalHuntReplay_IsAvailable();

        [DllImport("__Internal")]
        private static extern void SignalHuntReplay_Start();

        [DllImport("__Internal")]
        private static extern void SignalHuntReplay_StopAndPresent();
#endif
    }
}
