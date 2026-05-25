using UnityEngine;
using UnityEngine.UI;
using Vanguard.TD.Core;

namespace Vanguard.TD.UI
{
    /// <summary>
    /// Self-registering button that triggers wave launch. Visible only during Loadout.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class WaveLaunchButton : MonoBehaviour
    {
        Button _btn;

        void Start()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(Trigger);

            if (GameFlow.Live != null)
            {
                GameFlow.Live.PhaseChanged += OnPhase;
                OnPhase(GameFlow.Live.Phase);
            }
        }

        void Trigger()
        {
            GameDirector.Live?.IgniteWave();
        }

        void OnPhase(MatchPhase p)
        {
            gameObject.SetActive(p == MatchPhase.Loadout);
        }
    }
}
