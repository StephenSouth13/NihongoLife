using System.Collections;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>Door of the waiting train: validates the ticket and starts the ride.</summary>
    public sealed class IslandTrainDoor : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? "Board for Hibari" : "ひばりゆき に のる";
        public string GetpromptEn() => IslandTrainService.HasTicket ? NihongoLife.Core.Loc.T("Soát vé và lên tàu về Hibari", "Show your ticket and board for Hibari", "きっぷを見せて、ひばり行きに乗る") : NihongoLife.Core.Loc.T("Cần vé — mua ở máy bán vé", "You need a ticket — buy one at the machine", "きっぷが必要です — 券売機で買いましょう");
        public Transform GetTransform() => transform;

        /// <summary>
        /// The way home must read from across the platform: a beacon over the door ("ひばり ゆき · [F] board") and the
        /// green のりば (boarding) mark painted on the platform in front of it, like a Japanese platform.
        /// </summary>
        private void Start()
        {
            var box = GetComponent<BoxCollider>();
            float half = box != null ? box.size.y * 0.5f * transform.lossyScale.y : 1.1f;
            Vector3 floor = transform.position + Vector3.down * half;
            PortalBeacon.AttachAt(transform, floor + Vector3.up * 2.75f,
                () => "ひばり ゆき  <size=70%>" + Loc.T("Tàu về khu phố Hibari", "Train home to Hibari", "ひばり行き") + "</size>",
                () => IslandTrainService.HasTicket ? Loc.T("Lên tàu", "Board", "のる") : Loc.T("Lên tàu (cần vé — máy bán vé bên phải)", "Board (ticket from the machine on the right)", "のる（きっぷが ひつよう）"), true);
            BoardingMark(floor + Vector3.up * 0.215f + Vector3.forward * 0.55f);
        }

        private void BoardingMark(Vector3 at)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = "BoardingMark";
            Destroy(mark.GetComponent<Collider>());
            mark.transform.SetParent(transform, true);
            mark.transform.SetPositionAndRotation(at, Quaternion.Euler(90f, 0f, 0f));
            mark.transform.localScale = new Vector3(2.8f / transform.lossyScale.x, 1.3f / transform.lossyScale.z, 1f);
            var renderer = mark.GetComponent<Renderer>();
            var source = Resources.Load<Material>("Animation/NL_GestureProp");
            if (source != null)
            {
                var material = new Material(source) { name = "BoardingMark" };
                var green = new Color(0.13f, 0.55f, 0.3f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", green);
                if (material.HasProperty("_Color")) material.SetColor("_Color", green);
                renderer.sharedMaterial = material;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var text = new GameObject("BoardingText").AddComponent<TMPro.TextMeshPro>();
            text.transform.SetParent(transform, true);
            // Lying on the platform, its top toward the train: read by someone walking up to the door.
            text.transform.SetPositionAndRotation(at + Vector3.up * 0.01f, Quaternion.Euler(90f, 180f, 0f));
            text.text = "のりば  ▲\n<size=60%>ひばり ゆき · Hibari</size>";
            text.fontSize = 3.2f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(2.8f, 1.3f);
        }
        public void Interact(GameObject player)
        {
            if (IslandVisit.Active) { IslandVisit.End(); return; } // visitors go back where they came from
            string error = IslandTrainService.Depart(this);
            if (error != null) IslandUI.Toast(error, true);
        }
    }
}
