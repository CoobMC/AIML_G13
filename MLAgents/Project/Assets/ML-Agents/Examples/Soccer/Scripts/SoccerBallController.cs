using UnityEngine;
using Unity.MLAgents.Sensors;


public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag;
    public string blueGoalTag;
    public Rigidbody ballRb;

    private AgentSoccer lastAgentTouched;

    // Sensors for temporary activation
    public RayPerceptionSensorComponent3D sensor1;
    public RayPerceptionSensorComponent3D sensor2;
    public RayPerceptionSensorComponent3D sensor3;
    public RayPerceptionSensorComponent3D sensor4;

    void Start()
    {
        envController = area.GetComponent<SoccerEnvController>();
    }

    void OnCollisionEnter(Collision col)
    {
        // Play audio when the ball is touched
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        // Goal collision logic
        if (col.gameObject.CompareTag(purpleGoalTag))
        {
            envController.GoalTouched(Team.Blue); // Blue team scores
        }
        else if (col.gameObject.CompareTag(blueGoalTag))
        {
            envController.GoalTouched(Team.Purple); // Purple team scores
        }

        // Interaction with agents
        if (col.gameObject.CompareTag("purpleAgent") || col.gameObject.CompareTag("blueAgent"))
        {
            var currentAgent = col.gameObject.GetComponent<AgentSoccer>();

            if (currentAgent != null)
            {
                // Update the last agent that touched the ball
                lastAgentTouched = currentAgent;

                // Reward the current agent for interacting with the ball
                currentAgent.AddReward(0.5f);
            }
        }

        // Temporarily enable sensors
        StartCoroutine(EnableSensorTemporarily());
    }

    public AgentSoccer GetLastAgentTouched()
    {
        return lastAgentTouched;
    }

    // Enable sensors for a short duration
    private System.Collections.IEnumerator EnableSensorTemporarily()
    {
        sensor1.enabled = true;
        sensor2.enabled = true;
        sensor3.enabled = true;
        sensor4.enabled = true;
        yield return new WaitForSeconds(1); // Wait for 1 second
        sensor1.enabled = false;
        sensor2.enabled = false;
        sensor3.enabled = false;
        sensor4.enabled = false;
    }
}