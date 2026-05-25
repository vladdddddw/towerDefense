using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Vanguard.TD.UI
{
    /// <summary>
    /// Controller for the title screen. Two actions: start match, show rules.
    /// </summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        public Button startButton;
        public Button rulesButton;
        public Button rulesCloseButton;
        public GameObject rulesPanel;

        void Start()
        {
            if (startButton)       startButton.onClick      .AddListener(LaunchMatch);
            if (rulesButton)       rulesButton.onClick      .AddListener(ShowRules);
            if (rulesCloseButton)  rulesCloseButton.onClick .AddListener(HideRules);
            if (rulesPanel)        rulesPanel.SetActive(false);
        }

        void LaunchMatch() => SceneManager.LoadScene("Match");

        // Note: the editor SceneBuilder names scenes "Title" and "Match".
        void ShowRules()   { if (rulesPanel) rulesPanel.SetActive(true);  }
        void HideRules()   { if (rulesPanel) rulesPanel.SetActive(false); }
    }
}
