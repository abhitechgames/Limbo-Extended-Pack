using UnityEditor;
using UnityEngine;
using GameSeed.DarkPlatformer;

namespace GameSeed
{
    [CustomEditor(typeof(PlayerController))]
    public class CustomInspectorButtons : Editor
    {
        // Define constants for URLs
        public static string DiscordUrl = "https://discord.gg/sbjZXg2YJ9";
        public static string Email = "mailto:gameseedassets@gmail.com";
        public static string AssetReviewUrl = "https://u3d.as/3TAV";

        public override void OnInspectorGUI()
        {
            // Add a space at the top
            GUILayout.Space(5);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 28;
            headerStyle.normal.textColor = Color.grey;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("DARK PLATFORMER", headerStyle);
            GUILayout.Space(5);

            // ======= Buttons at the Top =======
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Join Discord", GUILayout.Height(25)))
            {
                Application.OpenURL(DiscordUrl);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // ======= Rate This Asset =======
            if (GUILayout.Button("Rate This Asset", GUILayout.Height(30)))
            {
                Application.OpenURL(AssetReviewUrl);
            }

            GUILayout.Space(10);

            // Draw the default Inspector (after buttons)
            DrawDefaultInspector();
        }
    }
}