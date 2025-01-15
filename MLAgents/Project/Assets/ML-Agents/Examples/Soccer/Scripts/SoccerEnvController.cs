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

    void Start()
    {
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);

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
        // Reward for scoring
        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(1.0f); // Significant reward for scoring
            m_PurpleAgentGroup.AddGroupReward(-0.5f); // Penalize the opposing team
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(1.0f); // Significant reward for scoring
            m_BlueAgentGroup.AddGroupReward(-0.5f); // Penalize the opposing team
        }

        // End the episode after a goal
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();

        // Reset the scene after a goal
        ResetScene();
    }

    public void ResetScene()
    {
        m_ResetTimer = 0;

        // Reset Agents
        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = new Vector3(randomPosX, 0.5f, 0f); // Example of dynamic positioning
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f); // Use rotSign for rotation
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        // Reset Ball
        ResetBall();
    }

}
