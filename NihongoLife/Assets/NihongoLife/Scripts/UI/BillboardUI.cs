using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>
    /// Gắn Script này vào các Canvas (World Space) hoặc TextMeshPro nổi trên đầu nhân vật/vật phẩm.
    /// Nó sẽ luôn quay mặt về phía Camera của người chơi, giải quyết lỗi chữ bị lật ngược.
    /// </summary>
    public class BillboardUI : MonoBehaviour
    {
        [Tooltip("Nếu true, chữ sẽ giữ nguyên góc nghiêng (Pitch/Roll) và chỉ xoay trục Y (Yaw) theo Camera. Thường dùng cho biển tên trên đầu.")]
        public bool lockRotationXAndZ = false;

        private Camera _mainCam;

        private void Start()
        {
            _mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCam == null)
            {
                _mainCam = Camera.main;
                if (_mainCam == null) return;
            }

            // Xoay hướng về phía Camera (lật ngược lại 180 độ so với hướng nhìn của cam để chữ không bị soi gương)
            transform.LookAt(transform.position + _mainCam.transform.rotation * Vector3.forward,
                             _mainCam.transform.rotation * Vector3.up);

            if (lockRotationXAndZ)
            {
                Vector3 euler = transform.eulerAngles;
                euler.x = 0f;
                euler.z = 0f;
                transform.eulerAngles = euler;
            }
        }
    }
}
