using UnityEngine;

namespace DefendAndCollect.Waves
{
    [DefaultExecutionOrder(200)]
    public sealed class MatchHud : MonoBehaviour
    {
        private const float PanelWidth = 318f;
        private const float PanelHeight = 142f;
        private const float Margin = 12f;

        private static GUIStyle bannerStyle;
        private static GUIStyle centerStyle;

        private WaveController waveController;

        private void Start()
        {
            waveController = GetComponent<WaveController>();

            if (waveController == null)
            {
                waveController = FindAnyObjectByType<WaveController>();
            }
        }

        private void OnGUI()
        {
            if (waveController == null)
            {
                return;
            }

            DrawPanel();
            DrawResultBanner();
        }

        private void DrawPanel()
        {
            Rect panelRect = new Rect(
                Screen.width - PanelWidth - Margin,
                Screen.height - PanelHeight - Margin,
                PanelWidth,
                PanelHeight);

            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(panelRect.x + 8f, panelRect.y + 6f, panelRect.width - 16f, panelRect.height - 12f));

            GUILayout.Label("Partida (spec 010)");

            if (waveController.State == MatchState.Idle)
            {
                GUILayout.Label("Sin partida en curso.");
            }
            else
            {
                GUILayout.Label($"Oleada {Mathf.Min(waveController.CurrentWave, waveController.WaveCount)}/{waveController.WaveCount} | Enemigos: {waveController.EnemiesRemaining}");
                GUILayout.Label($"Aliados vivos: {waveController.AlliesAlive}/{waveController.AlliesTotal}");
                GUILayout.Label(StateLabel());
            }

            if (GUILayout.Button("Reiniciar partida (R)"))
            {
                waveController.Restart();
            }

            GUILayout.Label("F1/F2/F3: cambiar plantilla");

            GUILayout.EndArea();
        }

        private string StateLabel()
        {
            switch (waveController.State)
            {
                case MatchState.InWave:
                    return "En curso";
                case MatchState.BetweenWaves:
                    return $"Siguiente oleada en {waveController.PauseRemaining:0.0} s";
                case MatchState.Victory:
                    return "Victoria";
                case MatchState.Defeat:
                    return "Derrota";
                default:
                    return "Sin partida";
            }
        }

        private void DrawResultBanner()
        {
            if (waveController.State != MatchState.Victory && waveController.State != MatchState.Defeat)
            {
                return;
            }

            InitStyles();

            string title = waveController.State == MatchState.Victory ? "¡Victoria!" : "Derrota";
            Rect bannerRect = new Rect((Screen.width - 460f) * 0.5f, (Screen.height - 130f) * 0.5f, 460f, 130f);

            GUI.Box(bannerRect, GUIContent.none);
            GUI.Label(new Rect(bannerRect.x, bannerRect.y + 22f, bannerRect.width, 44f), title, bannerStyle);
            GUI.Label(
                new Rect(bannerRect.x, bannerRect.y + 74f, bannerRect.width, 28f),
                "R para reiniciar la partida",
                centerStyle);
        }

        private static void InitStyles()
        {
            if (bannerStyle != null)
            {
                return;
            }

            bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            centerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
