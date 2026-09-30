using NihongoLife.Core;
using TMPro;
using UnityEngine;

namespace NihongoLife.Interaction
{
    /// <summary>
    /// Bắt sự kiện bấm phím Emote và tạo ra icon bay lơ lửng trên đầu.
    /// </summary>
    public class PlayerEmoteController : MonoBehaviour
    {
        private GameInputService _input;
        private string[] _randomEmotes = { "♥", "♪", "!", "?", "★", "☻" };

        private void Start()
        {
            _input = GameInputService.GetOrCreate();
        }

        private void Update()
        {
            if (_input == null) return;

            // Khi bấm phím E (hoặc phím D-pad Trái)
            if (_input.WasPressed(GameInputId.Emote))
            {
                SpawnEmote();
            }
        }

        private void SpawnEmote()
        {
            // Lấy ngẫu nhiên 1 biểu tượng
            string text = _randomEmotes[Random.Range(0, _randomEmotes.Length)];

            // Tạo GameObject chứa TextMeshPro
            GameObject emoteGo = new GameObject("FloatingEmote");
            // Vị trí xuất hiện: Trên đầu nhân vật 2m
            emoteGo.transform.position = transform.position + Vector3.up * 2f;

            var tmp = emoteGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 8;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;
            
            // Xoay text hướng về camera
            tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);

            // Gắn kịch bản bay lên và mờ dần
            emoteGo.AddComponent<EmoteParticle>();
        }
    }

    /// <summary>
    /// Điều khiển hiệu ứng bay lên và mờ dần của icon. 
    /// Tồn tại tối đa 2.5 giây.
    /// </summary>
    public class EmoteParticle : MonoBehaviour
    {
        private TextMeshPro _tmp;
        private float _lifetime = 2.5f; // Thời gian tồn tại
        private float _age = 0f;
        private float _floatSpeed = 0.5f; // Tốc độ bay lên
        private Transform _mainCam;

        private void Start()
        {
            _tmp = GetComponent<TextMeshPro>();
            if (Camera.main != null) _mainCam = Camera.main.transform;
        }

        private void Update()
        {
            _age += Time.deltaTime;

            // Di chuyển từ từ lên trên
            transform.position += Vector3.up * (_floatSpeed * Time.deltaTime);

            // Xoay luôn luôn hướng về Camera (Billboard)
            if (_mainCam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _mainCam.position);
            }

            // Hiệu ứng mờ dần (Fade out) ở 1 giây cuối
            if (_age > (_lifetime - 1f))
            {
                float alpha = Mathf.Clamp01(_lifetime - _age);
                _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, alpha);
            }

            // Hủy object khi hết thời gian
            if (_age >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
