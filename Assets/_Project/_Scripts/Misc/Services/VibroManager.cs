using UnityEngine;

namespace Misc.Services
{
    public static class VibroManager
    {
        public static bool IsVibroOn => PlayerPrefs.GetInt(PlayerPrefsKeys.VibroOn, 0) == 1;

        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (IsVibroOn)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}