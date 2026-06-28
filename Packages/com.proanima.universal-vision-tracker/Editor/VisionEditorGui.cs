using System;
using UnityEditor;
using UnityEngine;

namespace UniversalTracker.Editor
{
    internal static class VisionEditorGui
    {
        private static GUIStyle titleStyle;
        private static GUIStyle subtitleStyle;
        private static GUIStyle sectionStyle;

        public static void DrawHeader(string title, string subtitle)
        {
            EnsureStyles();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(title, titleStyle);
            EditorGUILayout.LabelField(subtitle, subtitleStyle);
            EditorGUILayout.Space(8f);
        }

        public static IDisposable Section(string title)
        {
            EnsureStyles();
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            return new Scope(EditorGUILayout.EndVertical);
        }

        public static bool PrimaryButton(string label)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.42f, 0.86f, 0.72f, 1f);
            bool clicked = GUILayout.Button(label, GUILayout.Height(34f));
            GUI.backgroundColor = previous;
            return clicked;
        }

        private static void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.92f, 0.96f, 0.98f) }
            };
            subtitleStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                normal = { textColor = new Color(0.58f, 0.68f, 0.72f) }
            };
            sectionStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 6, 8)
            };
        }

        private sealed class Scope : IDisposable
        {
            private readonly Action close;

            public Scope(Action close)
            {
                this.close = close;
            }

            public void Dispose()
            {
                close?.Invoke();
            }
        }
    }
}
