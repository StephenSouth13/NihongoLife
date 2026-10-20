using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife
{
    public class Test1_Mainmenu : MonoBehaviour
    {
        public static Test1_Mainmenu Instance { get; private set; }

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject newGamePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadGamePanel;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            mainMenuPanel.SetActive(true);
            newGamePanel.SetActive(false);
            settingsPanel.SetActive(false);
            loadGamePanel.SetActive(false);
        }

        void Update()
        {
            OnEnable();
        }

        private void OnEnable()
        {
            newGameButton.onClick.AddListener(OnClickNewGameButton);
            loadGameButton.onClick.AddListener(OnClickLoadGameButton);
            settingsButton.onClick.AddListener(OnClickSettingsButton);
        }

        #region Button Click Handlers
        private void OnClickNewGameButton()
        {
            mainMenuPanel.SetActive(false);
            newGamePanel.SetActive(true);
        }

        private void OnClickSettingsButton()
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void OnClickLoadGameButton()
        {
            mainMenuPanel.SetActive(false);
            loadGamePanel.SetActive(true);
        }
        #endregion
    }
}
