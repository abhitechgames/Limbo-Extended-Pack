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

        // Keeps the live readout below ticking while the game is running.
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            PlayerController player = (PlayerController)target;

            // Add a space at the top
            GUILayout.Space(5);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 28;
            headerStyle.normal.textColor = Color.grey;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label("DARK 2D WORLD", headerStyle);
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

            DrawSetupChecks(player);
            DrawLiveState(player);

            GUILayout.Space(6);

            // Draw the default Inspector (after buttons)
            DrawDefaultInspector();
        }

        /// <summary>Catches the handful of mistakes that make the character feel broken.</summary>
        private void DrawSetupChecks(PlayerController p)
        {
            int ownLayer = 1 << p.gameObject.layer;
            string layerName = LayerMask.LayerToName(p.gameObject.layer);

            if (p.groundCheck == null)
            {
                EditorGUILayout.HelpBox(
                    "Ground Check is empty, so the character never counts as standing on anything and can never jump.\n" +
                    "Fix: make an empty child object at its feet and drag it into the Ground Check slot.",
                    MessageType.Error);
            }

            if ((p.groundLayers.value & ownLayer) != 0)
            {
                EditorGUILayout.HelpBox(
                    "Ground Layers includes the character's own layer (" + layerName + "), so it can detect itself as ground.\n" +
                    "Fix: untick " + layerName + " in Ground Layers.",
                    MessageType.Warning);
            }

            if ((p.grabLayers.value & ownLayer) != 0)
            {
                EditorGUILayout.HelpBox(
                    "Grab Layers includes the character's own layer (" + layerName + "), so the reach check hits the character instead of the crate.\n" +
                    "Fix: set Grab Layers to just the layer your crates are on.",
                    MessageType.Warning);
            }

            if (p.waterLayers.value == 0)
            {
                EditorGUILayout.HelpBox(
                    "Water Layers is empty, so swimming is switched off. That is fine for a level with no water.",
                    MessageType.Info);
            }
        }

        /// <summary>Shows what the controller thinks is happening, for tuning in Play mode.</summary>
        private void DrawLiveState(PlayerController p)
        {
            if (!Application.isPlaying) return;

            GUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Live State", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Grounded", p.IsGrounded ? "yes" : "no");
            EditorGUILayout.LabelField("On Ladder", p.IsClimbing ? "yes" : "no");
            EditorGUILayout.LabelField("In Water", p.IsInWater ? "yes" : "no");
            EditorGUILayout.LabelField("Pushing", p.IsPushing ? "yes" : "no");
            EditorGUILayout.LabelField("Facing", p.Facing > 0 ? "right" : "left");
            EditorGUILayout.LabelField("Speed", p.Velocity.magnitude.ToString("F2"));
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }
    }
}
