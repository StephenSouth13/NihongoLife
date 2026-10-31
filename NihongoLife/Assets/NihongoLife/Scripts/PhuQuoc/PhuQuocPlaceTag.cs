using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    /// <summary>Marks a shop / hotel / guesthouse / market lot in the 1:1 Dương Đông area (names for tests, signs and quests).</summary>
    public sealed class PhuQuocPlaceTag : MonoBehaviour
    {
        public string nameVi, nameJa;
        public void Set(string vi, string ja) { nameVi = vi; nameJa = ja; }
    }
}
