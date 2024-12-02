using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public enum FieldZone
{
    PurpleGoal,
    BlueGoal,
    Middle
}

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

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    // List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;

    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;

    // === Task 1: Possession Tracking Variables ===
    private AgentSoccer lastPlayer = null;
    private AgentSoccer currentPlayer = null;
    // =============================================

    // === Task 2: Pass Detection Variables ===
    private bool passBeforeGoal = false;
    // Define goal lines to determine ball zones
    public float purpleGoalLineX = 6f;  // Adjust based on your field dimensions
    public float blueGoalLineX = -6f;   // Adjust based on your field dimensions
    // =======================================

    void Start()
    {
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        // Initialize TeamManager
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
        // Play goal sound
        var audioSource = ball.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // === Task 2: Modify rewards to include pass bonus ===
        // Base reward calculation
        float baseReward = Mathf.Max((2f - (float)m_ResetTimer / MaxEnvironmentSteps), 1f);

        // Bonus for passing before the goal
        float passBonus = passBeforeGoal ? 0.5f : 0f;

        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(baseReward + passBonus);
            m_PurpleAgentGroup.AddGroupReward(-1);
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(baseReward + passBonus);
            m_BlueAgentGroup.AddGroupReward(-1);
        }
        // ================================================

        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();
    }

    public void ResetScene()
    {
        m_ResetTimer = 0;

        // === Task 2: Reset pass flag ===
        ResetPassOccurred();
        // ===============================

        // Reset Agents
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

        // Reset Ball
        ResetBall();
    }

    // === Task 1: Possession Tracking Methods ===
    public AgentSoccer GetCurrentPossessor()
    {
        return currentPlayer;
    }

    public void SetCurrentPossessor(AgentSoccer player)
    {
        currentPlayer = player;
        Debug.Log($"Current Possessor: {(player != null ? player.name : "None")}");
    }

    public AgentSoccer GetLastPossessor()
    {
        return lastPlayer;
    }

    public void SetLastPossessor(AgentSoccer player)
    {
        lastPlayer = player;
        Debug.Log($"Last Possessor: {(player != null ? player.name : "None")}");
    }
    // ===========================================

    // === Task 2: Pass Detection Methods ===
    public void SetPassOccurred()
    {
        passBeforeGoal = true;
    }

    public void ResetPassOccurred()
    {
        passBeforeGoal = false;
    }

    public FieldZone GetBallZone()
    {
        if (ball.transform.position.x > purpleGoalLineX)
        {
            return FieldZone.PurpleGoal;
        }
        if (ball.transform.position.x < blueGoalLineX)
        {
            return FieldZone.BlueGoal;
        }
        return FieldZone.Middle;
    }
    // =======================================
}
