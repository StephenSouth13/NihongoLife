using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Marks text written by people (names, bios, comments, chat): shown as is, in whatever language it was
    /// written — not interface text, so the one-language checks skip it.</summary>
    public sealed class UiUserContent : MonoBehaviour
    {
        public static T Mark<T>(T component) where T : Component
        {
            if (component != null && component.GetComponent<UiUserContent>() == null) component.gameObject.Need<UiUserContent>();
            return component;
        }
    }
}
