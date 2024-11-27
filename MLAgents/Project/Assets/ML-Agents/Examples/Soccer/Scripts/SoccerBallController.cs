using UnityEngine;

public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag; // Used to check if collided with purple goal
    public string blueGoalTag;   // Used to check if collided with blue goal

    void Start()
    {
        envController = area.GetComponent<SoccerEnvController>();
    }

    void OnCollisionEnter(Collision col)
    {
        // Play sound for any collision with agent, wall, or goal
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Handle goal collision logic
        if (col.gameObject.CompareTag(purpleGoalTag)) // Ball touched purple goal
        {
            envController.GoalTouched(Team.Blue);
        }
        else if (col.gameObject.CompareTag(blueGoalTag)) // Ball touched blue goal
        {
            envController.GoalTouched(Team.Purple);
        }
    }
}
