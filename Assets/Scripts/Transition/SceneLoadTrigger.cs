using UnityEngine;
using UnityEngine.Serialization;

namespace bison.core.Transition
{
    public sealed class SceneLoadTrigger : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("_sceneName")] private string sceneName = "Game";
        [SerializeField, FormerlySerializedAs("_style")] private TransitionStyle transitionStyle = TransitionStyle.Splash;
        [SerializeField, Min(0f), FormerlySerializedAs("_coverTime")] private float coverDuration = 0.6f;
        [SerializeField, Min(0f), FormerlySerializedAs("_revealTime")] private float revealDuration = 0.6f;

        public void Load() => SceneTransition.Load(sceneName, transitionStyle, coverDuration, revealDuration);
    }
}
