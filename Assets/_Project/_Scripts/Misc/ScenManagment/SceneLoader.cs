using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Misc.SceneManagment
{
    public class SceneLoader : MonoBehaviour
    {
        private readonly float _fadeDuration = 0.5f;

        [SerializeField] private GameObject _loading;

        private CanvasGroup _canvasGroup;

        private bool isLoading = false;

        private void Awake()
        {
            _canvasGroup = _loading.GetComponent<CanvasGroup>();
        }

        public void ChangeScene(string sceneName)
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneWithFade(sceneName));
            }
        }

        private IEnumerator LoadSceneWithFade(string sceneName)
        {
            isLoading = true;
            _loading.SetActive(true);
            yield return StartCoroutine(FadeIn());

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;

            while (asyncOperation.progress < 0.9f)
                yield return null;

            yield return new WaitForSeconds(0.5f);

            asyncOperation.allowSceneActivation = true;

            while (!asyncOperation.isDone)
                yield return null;

            yield return StartCoroutine(FadeOut());

            isLoading = false;
        }

        private IEnumerator FadeIn()
        {
            float timer = 0.0f;
            while (timer <= _fadeDuration)
            {
                _canvasGroup.alpha = Mathf.Lerp(0.0f, 1.0f, timer / _fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            _canvasGroup.alpha = 1.0f;
        }

        private IEnumerator FadeOut()
        {
            float timer = 0.0f;
            while (timer <= _fadeDuration)
            {
                _canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, timer / _fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            _canvasGroup.alpha = 0.0f;
        }
    }
}