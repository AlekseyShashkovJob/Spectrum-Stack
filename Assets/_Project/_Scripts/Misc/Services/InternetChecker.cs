using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Misc.Services
{
    public class InternetChecker
    {
        private static readonly string[] TestUrls = new[]
        {
            "https://clients3.google.com/generate_204",
            "https://www.gstatic.com/generate_204",
            "https://www.apple.com/library/test/success.html"
        };

        public IEnumerator CheckAvailability(Action<bool> callback)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                Debug.LogWarning("Unity сообщает: нет соединения, выполняем HTTP проверку.");

            foreach (var url in TestUrls)
            {
                using var request = UnityWebRequest.Head(url);
                request.timeout = 8;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Интернет подтверждён через: {url}");
                    callback?.Invoke(true);
                    yield break;
                }

                Debug.LogWarning($"Ошибка проверки {url}: {request.error}");
            }

            Debug.LogError("Интернет не доступен после проверки всех URL.");
            callback?.Invoke(false);
        }
    }
}