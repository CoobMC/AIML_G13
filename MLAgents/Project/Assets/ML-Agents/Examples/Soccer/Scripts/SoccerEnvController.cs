using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }
    public string blueGoalTag;
    public string purpleGoalTag;

    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;
    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;

    public AgentSoccer lastAgentTouched;

    void Start()
    {
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();

        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                Debug.LogError("Rigidbody component not found on the assigned ball.");
            }
            else
            {
                Debug.Log("Ball Rigidbody assigned successfully.");
            }
        }
        else
        {
            Debug.LogError("Ball GameObject is not assigned.");
        }

        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        m_BallStartingPos = ball != null ? ball.transform.position : Vector3.zero;

        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        if (ball == null)
        {
            ball = GameObject.FindWithTag("ball");
            if (ball != null)
            {
                ballRb = ball.GetComponent<Rigidbody>();
                if (ballRb == null)
                {
                    Debug.LogError("Rigidbody component not found on the ball.");
                }
                else
                {
                    Debug.Log("Ball and Rigidbody found and assigned successfully.");
                }
            }
        }

        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    public void ResetBall()
    {
        var randomPosX = Random.Range(-2.5f, 2.5f);
        var randomPosZ = Random.Range(-2.5f, 2.5f);

        ball.transform.position = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    public void GoalTouched(Team scoredTeam)
    {
        if (lastAgentTouched == null)
        {
            Debug.LogWarning("No agent was recorded as the last to touch the ball.");
            return;
        }

        if (lastAgentTouched.team != scoredTeam) // Own goal
        {
            lastAgentTouched.AddReward(-1f); // Penalty for scoring an own goal
            Debug.LogWarning("own goal. by " + lastAgentTouched.team);
        }
        else
        {
            lastAgentTouched.AddReward(1f); 
        }

        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();
    }

    public void ResetScene()
    {
        m_ResetTimer = 0;

        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        ResetBall();
    }

    public void UpdateLastAgentTouched(AgentSoccer agent)
    {
        lastAgentTouched = agent;
    }
}
