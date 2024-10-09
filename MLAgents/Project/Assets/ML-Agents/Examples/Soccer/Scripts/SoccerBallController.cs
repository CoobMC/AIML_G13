using UnityEngine;

public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag;
    public string blueGoalTag; 

    void Start()
    {
        envController = area.GetComponent<SoccerEnvController>();
    }

    void OnCollisionEnter(Collision col)
    {

        AgentSoccer agent = col.gameObject.GetComponent<AgentSoccer>();
        if (agent != null)
        {
            
            envController.UpdateLastAgentTouched(agent);
        }

        // Check for goal scoring
        if (col.gameObject.CompareTag(purpleGoalTag)) // ball touched purple goal
        {
            envController.GoalTouched(Team.Blue);
        }
        else if (col.gameObject.CompareTag(blueGoalTag)) // ball touched blue goal
        {
            envController.GoalTouched(Team.Purple);
        }
    }
}
