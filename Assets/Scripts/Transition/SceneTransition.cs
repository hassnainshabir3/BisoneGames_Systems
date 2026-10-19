using System.Collections;
using bison.core.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace bison.core.Transition
{
    public enum TransitionStyle { Circle = 0, Splash = 1, Ink = 2, Wave = 3, Slash = 4, Dots = 5 }

    public sealed class SceneTransition : MonoBehaviour
    {
        private const float MaximumSeed = 50f;

        private Material transitionMaterial;
        private RawImage fillImage;
        private bool is_Busy;

        public static SceneTransition Instance { get; private set; }
        public static bool IsBusy => Instance != null && Instance.is_Busy;

        // Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject(nameof(SceneTransition)).AddComponent<SceneTransition>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void BuildOverlay()
        {
            Material sourceMaterial = Resources.Load<Material>(ResourcePaths.TransitionMaterial);

            if (sourceMaterial == null)
            {
                Debug.LogError(LogMessages.MissingResource(ResourcePaths.TransitionMaterial));
                return;
            }

            transitionMaterial = new Material(sourceMaterial);

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            gameObject.AddComponent<GraphicRaycaster>();

            GameObject fillObject = new GameObject(nameof(RawImage), typeof(RectTransform));
            fillObject.transform.SetParent(transform, false);

            RectTransform fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            fillImage = fillObject.AddComponent<RawImage>();
            fillImage.material = transitionMaterial;
            fillImage.raycastTarget = true;
            fillImage.enabled = false;
        }

        // Public API

        public static void Load(string sceneName, TransitionStyle transitionStyle = TransitionStyle.Splash,
                                float coverDuration = 0.6f, float revealDuration = 0.6f)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(LogMessages.SceneNotInBuild(sceneName));
                return;
            }

            if (Instance == null || Instance.transitionMaterial == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            if (Instance.is_Busy) return;
            Instance.StartCoroutine(Instance.LoadRoutine(sceneName, transitionStyle, coverDuration, revealDuration));
        }

        public static IEnumerator Cover(TransitionStyle transitionStyle, float duration)
        {
            if (Instance == null || Instance.transitionMaterial == null) yield break;

            Instance.is_Busy = true;
            Instance.PrepareOverlay(transitionStyle);
            yield return Instance.AnimateProgress(duration, false);
        }

        public static void RevealAfter(AsyncOperation loadOperation, float duration)
        {
            if (Instance == null || Instance.transitionMaterial == null) return;
            Instance.StartCoroutine(Instance.RevealRoutine(loadOperation, duration));
        }

        // Routines

        private IEnumerator LoadRoutine(string sceneName, TransitionStyle transitionStyle,
                                        float coverDuration, float revealDuration)
        {
            is_Busy = true;
            PrepareOverlay(transitionStyle);
            yield return AnimateProgress(coverDuration, false);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
            yield return RevealRoutine(loadOperation, revealDuration);
        }

        private IEnumerator RevealRoutine(AsyncOperation loadOperation, float duration)
        {
            if (loadOperation != null)
                while (!loadOperation.isDone) yield return null;

            yield return null;
            yield return AnimateProgress(duration, true);

            fillImage.enabled = false;
            is_Busy = false;
        }

        private void PrepareOverlay(TransitionStyle transitionStyle)
        {
            transitionMaterial.SetFloat(ShaderProperties.Style, (float)transitionStyle);
            transitionMaterial.SetFloat(ShaderProperties.Seed, Random.Range(0f, MaximumSeed));
            transitionMaterial.SetFloat(ShaderProperties.Reveal, 0f);
            transitionMaterial.SetFloat(ShaderProperties.Progress, 0f);
            fillImage.enabled = true;
        }

        private IEnumerator AnimateProgress(float duration, bool is_Revealing)
        {
            transitionMaterial.SetFloat(ShaderProperties.Reveal, is_Revealing ? 1f : 0f);
            transitionMaterial.SetFloat(ShaderProperties.Progress, 0f);

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                transitionMaterial.SetFloat(ShaderProperties.Progress, Mathf.SmoothStep(0f, 1f, normalizedTime));
                yield return null;
            }

            transitionMaterial.SetFloat(ShaderProperties.Progress, 1f);
        }
    }
}
