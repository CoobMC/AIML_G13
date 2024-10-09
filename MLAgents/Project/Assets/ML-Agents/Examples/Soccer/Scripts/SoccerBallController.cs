using UnityEngine;

public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag;
    public string blueGoalTag; 

    public float minPassDistance = 2.0f; 
    void Start()
    {
        envController = area.GetComponent<SoccerEnvController>();
    }

    void OnCollisionEnter(Collision col)
    {
        AgentSoccer agent = col.gameObject.GetComponent<AgentSoccer>();
        if (agent != null)
        {
            // Check if the last agent was a different agent
            if (envController.lastAgentTouched != null && envController.lastAgentTouched != agent)
            {
                // Calculate the distance between the current and last agent
                float distance = Vector3.Distance(envController.lastAgentTouched.transform.position, agent.transform.position);

                if (distance >= minPassDistance)
                {
                    // Determine if the pass was to a teammate or an opponent
                    if (envController.lastAgentTouched.team == agent.team)
                    {
                        envController.lastAgentTouched.AddReward(0.5f);
                    }
                    else
                    {
                        envController.lastAgentTouched.AddReward(-0.5f);
                        agent.AddReward(0.2f);
                    }
                }
            }

            envController.UpdateLastAgentTouched(agent);
        }

        // Check for goal scoring
        if (col.gameObject.CompareTag(purpleGoalTag))
        {
            envController.GoalTouched(Team.Blue);
        }
        else if (col.gameObject.CompareTag(blueGoalTag))
        {
            envController.GoalTouched(Team.Purple);
        }
    }
}
