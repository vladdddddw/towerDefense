using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vanguard.TD.Core;

namespace Vanguard.TD.UI
{
    /// <summary>
    /// End-game banner. Shown by <see cref="GameDirector"/> on win/loss.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VictoryPanel : MonoBehaviour
    {
        public static VictoryPanel Live { get; private set; }

        public GameObject       panel;
        public TextMeshProUGUI  headline;
        public TextMeshProUGUI  subtext;
        public Button           replayButton;

        void Awake()
        {
            if (Live != null && Live != this) { Destroy(gameObject); return; }
            Live = this;
            if (panel) panel.SetActive(false);
        }

        void Start()
        {
            if (replayButton != null)
                replayButton.onClick.AddListener(() => GameDirector.Live?.Restart());
        }

        void OnDestroy()
        {
            if (Live == this) Live = null;
        }

        public void Reveal(bool victory)
        {
            if (panel) panel.SetActive(true);
            if (headline)
                headline.text = victory ? "ПЕРЕМОГА!" : "ВИ ПРОГРАЛИ!";
            if (subtext)
                subtext.text = victory
                    ? "Усі 10 хвиль відбито!"
                    : "База була зруйнована...";
        }

        public void Hide()
        {
            if (panel) panel.SetActive(false);
        }
    }
}
