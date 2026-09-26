using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using NihongoLife.Core;

#if ENABLE_VIVOX
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
#endif

namespace NihongoLife.Audio
{
    public class VivoxVoiceManager : MonoBehaviour
    {
        public static VivoxVoiceManager Instance { get; private set; }
        
#pragma warning disable 0067
        public event Action<string, string> OnTextMessageReceived; // senderName, text
#pragma warning restore 0067

#if ENABLE_VIVOX
        private bool _isInitialized = false;
        private string _currentChannelName = string.Empty;

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            await InitializeVivoxAsync();
        }

        private async Task InitializeVivoxAsync()
        {
            try
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(Application.cloudProjectId))
                {
                    Debug.LogWarning("[VivoxVoiceManager] Unity Project is không được liên kết với UGS (Cloud Project ID trống). Bỏ qua khởi tạo Vivox để tránh lỗi 401 Unauthorized.");
                    return;
                }
#endif
                // 1. Initialize Unity Services
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                // 2. Authenticate anonymously (required for Vivox)
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[VivoxVoiceManager] Signed in anonymously as: {AuthenticationService.Instance.PlayerId}");
                }

                // 3. Initialize Vivox Service
                await VivoxService.Instance.InitializeAsync();
                VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
                _isInitialized = true;
                Debug.Log("[VivoxVoiceManager] Vivox Initialized successfully!");

                // 4. Listen for Scene changes to join channels automatically
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                // Join channel for the first loaded scene
                JoinChannelForScene(SceneManager.GetActiveScene().name);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VivoxVoiceManager] Failed to initialize Vivox: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            }
            
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_isInitialized && !string.IsNullOrEmpty(_currentChannelName))
            {
                LeaveCurrentChannel();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            JoinChannelForScene(scene.name);
        }

        private void JoinChannelForScene(string sceneName)
        {
            if (!_isInitialized) return;

            // Prefix channel to avoid collisions across different environments
            string newChannel = $"NihongoLife_Scene_{sceneName}";

            if (_currentChannelName == newChannel) return;

            if (!string.IsNullOrEmpty(_currentChannelName))
            {
                LeaveCurrentChannel();
            }

            _currentChannelName = newChannel;
            JoinPositionalChannel(_currentChannelName);
        }

        private async void JoinPositionalChannel(string channelName)
        {
            try
            {
                // Join as a 3D Positional Channel
                Channel3DProperties properties = new Channel3DProperties(
                    audibleDistance: 32,
                    conversationalDistance: 1,
                    audioFadeIntensityByDistance: 1.0f,
                    audioFadeModel: AudioFadeModel.InverseByDistance
                );

                await VivoxService.Instance.JoinPositionalChannelAsync(
                    channelName,
                    ChatCapability.TextAndAudio,
                    properties
                );

                Debug.Log($"[VivoxVoiceManager] Successfully joined 3D Voice Channel: {channelName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VivoxVoiceManager] Failed to join channel {channelName}: {e.Message}");
                _currentChannelName = string.Empty;
            }
        }

        private async void LeaveCurrentChannel()
        {
            if (string.IsNullOrEmpty(_currentChannelName)) return;

            try
            {
                await VivoxService.Instance.LeaveChannelAsync(_currentChannelName);
                Debug.Log($"[VivoxVoiceManager] Left Voice Channel: {_currentChannelName}");
                _currentChannelName = string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VivoxVoiceManager] Failed to leave channel {_currentChannelName}: {e.Message}");
            }
        }

        /// <summary>
        /// Update the 3D position of the local player so Vivox can calculate spatial audio (hearing voices from the correct direction).
        /// This should be called in Update() by your PlayerController.
        /// </summary>
        public void Update3DPosition(Transform playerTransform, Transform cameraTransform)
        {
            if (!_isInitialized || string.IsNullOrEmpty(_currentChannelName)) return;

            // Vivox requires forward and up vectors to calculate orientation
            VivoxService.Instance.Set3DPosition(
                playerTransform.gameObject,
                _currentChannelName,
                playerTransform.position,
                cameraTransform.forward,
                cameraTransform.up
            );
        }

        public async void SendTextMessage(string text)
        {
            if (!_isInitialized || string.IsNullOrEmpty(_currentChannelName)) return;
            try
            {
                await VivoxService.Instance.SendChannelTextMessageAsync(_currentChannelName, text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VivoxVoiceManager] Failed to send text: {e.Message}");
            }
        }

        private void OnChannelMessageReceived(VivoxMessage message)
        {
            if (message.FromSelf) return; // Don't echo our own messages (handled locally by UI)
            OnTextMessageReceived?.Invoke(message.SenderDisplayName, message.MessageText);
        }
#else
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public void Update3DPosition(Transform playerTransform, Transform cameraTransform) {}
        
        public void SendTextMessage(string text) {}
#endif
    }
}
