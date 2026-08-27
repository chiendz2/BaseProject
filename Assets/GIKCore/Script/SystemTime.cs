using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GIKCore
{
    [DefaultExecutionOrder(-1)]
    public class SystemTime : MonoBehaviour
    {
        public static double Seconds;

        [DllImport("__Internal")]
        private static extern double GetSystemUptime();

        private void Awake()
        {
            UpdateCurrentTime();
        }

        private void Update()
        {
            Seconds += Time.unscaledDeltaTime;
        }

        private void OnApplicationPause(bool pause)
        {
            UpdateCurrentTime();
        }

        public void UpdateCurrentTime()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Seconds = GetSystemUptime();
#elif UNITY_ANDROID && !UNITY_EDITOR
            using (var systemClock = new AndroidJavaObject("android.os.SystemClock"))
            {
                long milliseconds = systemClock.CallStatic<long>("elapsedRealtime");
                Seconds = milliseconds * .001;
            }
#elif UNITY_STANDALONE || UNITY_EDITOR
            int ticks = Environment.TickCount;

            if (ticks < 0)
                ticks = Int32.MaxValue + Environment.TickCount;

            Seconds = ticks * .001;
#endif
        }
    }
}
