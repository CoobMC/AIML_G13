namespace ML_Agents.Examples.Soccer.Scripts
{
    using UnityEngine;
    using UnityEngine.UI;

    public class ScoreDisplay : MonoBehaviour
    {
        [Tooltip("Reference to the SoccerEnvController that tracks the scores.")]
        public SoccerEnvController soccerEnvController;

        [Tooltip("UI Text component that displays the Blue team’s score.")]
        public Text blueScoreText;

        [Tooltip("UI Text component that displays the Purple team’s score.")]
        public Text purpleScoreText;

        // Update is called once per frame
        void Update()
        {
            // Safety check
            if (soccerEnvController != null)
            {
                blueScoreText.text = "Blue: " + soccerEnvController.BlueScore;
                purpleScoreText.text = "Purple: " + soccerEnvController.PurpleScore;
            }
        }
    }
}
