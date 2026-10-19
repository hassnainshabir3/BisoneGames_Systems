using System;
using System.Collections.Generic;
using bison.core.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace bison.core.Boot
{
    public enum LogoAnimation
    {
        Fade,
        FadeZoom,
        SlideUp,
        Pop,
    }

    [Serializable]
    public sealed class SplashLogo
    {
        [SerializeField, FormerlySerializedAs("logoSprite")] private Sprite sprite;
        [SerializeField, FormerlySerializedAs("animation")] private LogoAnimation logoAnimation = LogoAnimation.Fade;
        [SerializeField, FormerlySerializedAs("fadeIn")] private float fadeInDuration = 0.5f;
        [SerializeField, FormerlySerializedAs("hold")] private float holdDuration = 2.5f;
        [SerializeField, FormerlySerializedAs("fadeOut")] private float fadeOutDuration = 0.6f;
        [SerializeField] private bool can_Skip = true;

        public Sprite Sprite => sprite;
        public LogoAnimation Animation => logoAnimation;
        public float FadeInDuration => fadeInDuration;
        public float HoldDuration => holdDuration;
        public float FadeOutDuration => fadeOutDuration;
        public bool CanSkip => can_Skip;
    }

    [CreateAssetMenu(fileName = "SplashConfig", menuName = "Bison/Splash Config")]
    public sealed class SplashConfig : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("nextScene")] private string nextSceneName = SceneNames.MainMenu;
        [SerializeField, FormerlySerializedAs("logos")] private List<SplashLogo> logos = new List<SplashLogo>();

        public string NextSceneName => nextSceneName;
        public IReadOnlyList<SplashLogo> Logos => logos;
    }
}
