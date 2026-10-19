using System;
using System.Collections;
using bison.core.Common;
using bison.core.Transition;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace bison.core.Boot
{
    public sealed class SplashController : MonoBehaviour
    {
        private const float PreloadReadyProgress = 0.9f;
        private const float CoverDuration = 0.5f;
        private const float RevealDuration = 0.6f;
        private const float SkipFadeDuration = 0.1f;

        [SerializeField, FormerlySerializedAs("_config")] private SplashConfig config;
        [SerializeField, FormerlySerializedAs("_logoImage")] private Image logoImage;

        [Header("Systems")]
        [SerializeField, FormerlySerializedAs("_waitForReady")] private bool should_WaitForReady;
        [SerializeField, FormerlySerializedAs("_onBootStart")] private UnityEvent onBootStart;

        private CanvasGroup canvasGroup;
        private RectTransform logoRect;
        private Vector2 basePosition;

        private bool is_Ready;
        private bool should_Skip;
        private bool can_SkipNow;

        public void MarkReady() => is_Ready = true;

        public void Skip()
        {
            if (can_SkipNow) should_Skip = true;
        }

        // Lifecycle

        private void Awake()
        {
            logoRect = logoImage.rectTransform;
            basePosition = logoRect.anchoredPosition;

            canvasGroup = logoImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = logoImage.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void Start() => StartCoroutine(RunSequence());

        // Sequence

        private IEnumerator RunSequence()
        {
            if (config == null || string.IsNullOrEmpty(config.NextSceneName))
            {
                Debug.LogError(LogMessages.SplashConfigInvalid);
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(config.NextSceneName))
            {
                Debug.LogError(LogMessages.SceneNotInBuild(config.NextSceneName));
                yield break;
            }

            onBootStart.Invoke();

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(config.NextSceneName);
            loadOperation.allowSceneActivation = false;

            foreach (SplashLogo logo in config.Logos)
            {
                if (logo == null || logo.Sprite == null) continue;
                yield return PlayLogo(logo);
            }

            while (loadOperation.progress < PreloadReadyProgress || (should_WaitForReady && !is_Ready))
                yield return null;

            yield return SceneTransition.Cover(TransitionStyle.Circle, CoverDuration);

            loadOperation.allowSceneActivation = true;
            SceneTransition.RevealAfter(loadOperation, RevealDuration);
        }

        private IEnumerator PlayLogo(SplashLogo logo)
        {
            logoImage.sprite = logo.Sprite;
            can_SkipNow = logo.CanSkip;
            should_Skip = false;

            canvasGroup.alpha = 0f;
            logoRect.localScale = Vector3.one;
            logoRect.anchoredPosition = basePosition;

            yield return RunTween(logo.FadeInDuration, progress => ApplyIntroAnimation(logo.Animation, progress));

            float holdTimer = 0f;

            while (!should_Skip && holdTimer < logo.HoldDuration)
            {
                holdTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            float fadeOutDuration = should_Skip ? SkipFadeDuration : logo.FadeOutDuration;
            yield return RunTween(fadeOutDuration, progress => canvasGroup.alpha = 1f - progress);

            should_Skip = false;
        }

        // Animation

        private static IEnumerator RunTween(float duration, Action<float> applyProgress)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                applyProgress(Mathf.Clamp01(elapsedTime / duration));
                yield return null;
            }

            applyProgress(1f);
        }

        private void ApplyIntroAnimation(LogoAnimation animation, float progress)
        {
            canvasGroup.alpha = progress;

            switch (animation)
            {
                case LogoAnimation.FadeZoom:
                    logoRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, EaseOutCubic(progress));
                    break;

                case LogoAnimation.SlideUp:
                    logoRect.anchoredPosition = basePosition + Vector2.down * Mathf.Lerp(150f, 0f, EaseOutCubic(progress));
                    break;

                case LogoAnimation.Pop:
                    logoRect.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, EaseOutBack(progress));
                    break;
            }
        }

        private static float EaseOutCubic(float progress)
        {
            float inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float progress)
        {
            const float Overshoot = 1.70158f;
            const float OvershootPlusOne = Overshoot + 1f;

            float shifted = progress - 1f;
            return 1f + OvershootPlusOne * shifted * shifted * shifted + Overshoot * shifted * shifted;
        }
    }
}
