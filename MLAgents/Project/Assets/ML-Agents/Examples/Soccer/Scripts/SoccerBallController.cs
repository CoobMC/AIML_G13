using UnityEngine;
using Unity.MLAgents.Sensors;
public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag; // Used to check if collided with purple goal
    public string blueGoalTag;   // Used to check if collided with blue goal
    public RayPerceptionSensorComponent3D sensor1;    // The sensor to enable/disable
    public RayPerceptionSensorComponent3D sensor2;    // The sensor to enable/disable
    public RayPerceptionSensorComponent3D sensor3;    // The sensor to enable/disable
    public RayPerceptionSensorComponent3D sensor4;    // The sensor to enable/disable
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
        StartCoroutine(EnableSensorTemporarily());
    }
}
