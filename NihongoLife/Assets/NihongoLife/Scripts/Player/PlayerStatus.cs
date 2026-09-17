using System;
using UnityEngine;

namespace NihongoLife.Player
{
    public class PlayerStatus : MonoBehaviour
    {
        public static PlayerStatus Instance { get; private set; }

        public string PlayerName { get; private set; } = "Học viên Nihongo";
        public int Level { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int MaxExp { get; private set; } = 100;
        
        public int CurrentHealth { get; private set; } = 100;
        public int MaxHealth { get; private set; } = 100;
        
        public int CurrentStamina { get; private set; } = 50;
        public int MaxStamina { get; private set; } = 50;

        public event Action OnStatusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            PlayerName = PlayableCharacterCatalog.GetPlayerName();
        }

        public void SetPlayerName(string playerName)
        {
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? PlayableCharacterCatalog.DefaultPlayerName : playerName.Trim();
            PlayableCharacterCatalog.SavePlayerName(PlayerName);
            OnStatusChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddExp(int amount)
        {
            CurrentExp += amount;
            while (CurrentExp >= MaxExp)
            {
                CurrentExp -= MaxExp;
                Level++;
                MaxExp = Mathf.FloorToInt(MaxExp * 1.5f);
                MaxHealth += 10;
                CurrentHealth = MaxHealth;
            }
            OnStatusChanged?.Invoke();
        }

        public void ConsumeStamina(int amount)
        {
            CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
            OnStatusChanged?.Invoke();
        }
    }
}
