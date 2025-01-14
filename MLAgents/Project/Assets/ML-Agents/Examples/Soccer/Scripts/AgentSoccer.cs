using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    [HideInInspector]
    public Team team;
    float m_KickPower;
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;
    EnvironmentParameters m_ResetParams;

    public GameObject ball; // Reference to the ball object
    public Vector3 fieldCenter = Vector3.zero; // Center of the field
    public float maxAllowedDistance = 15f; // Maximum allowed distance from the field center

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>(); // Assign the Rigidbody
        if (agentRb == null)
        {
            Debug.LogError("agentRb is null!");
        }

        ball = GameObject.FindWithTag("ball"); // Find the ball with tag "ball"
        if (ball == null)
        {
            Debug.LogError("Ball object with tag 'ball' not found!");
        }
        else
        {
            Debug.Log("Ball found: " + ball.name);
        }

        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }

        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }

        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void CollectObservations(VectorSensor sensor)
{
    Debug.Log($"CollectObservations called for: {gameObject.name}");

    // Check transform
    if (transform != null)
    {
        sensor.AddObservation(transform.localPosition); // Agent's position
    }
    else
    {
        Debug.LogError($"Transform is null for: {gameObject.name}");
    }

    // Check agentRb
    if (agentRb != null)
    {
        sensor.AddObservation(agentRb.velocity); // Agent's velocity
    }
    else
    {
        Debug.LogError($"agentRb is null for: {gameObject.name}. Make sure the Rigidbody is attached.");
    }

    // Check ball
    if (ball != null)
    {
        sensor.AddObservation(ball.transform.localPosition); // Ball's position
    }
    else
    {
        Debug.LogError($"Ball is null for: {gameObject.name}. Ensure the ball is assigned correctly.");
    }
}


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (position == Position.Goalie)
        {
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            AddReward(-m_Existential);
        }
        MoveAgent(actionBuffers.DiscreteActions);

        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        float rewardForProximity = Mathf.Clamp(1 - (distanceToBall / maxAllowedDistance), 0, 1);
        AddReward(rewardForProximity * 0.01f);

        float distanceFromCenter = Vector3.Distance(transform.position, fieldCenter);
        if (distanceFromCenter > maxAllowedDistance)
        {
            AddReward(-0.01f);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = act[0];
        var lateralAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo += transform.forward * m_ForwardSpeed;
                break;
            case 2:
                dirToGo += -transform.forward * m_ForwardSpeed;
                break;
        }

        switch (lateralAxis)
        {
            case 1:
                dirToGo += transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo += -transform.right * m_LateralSpeed;
                break;
        }

        if (dirToGo != Vector3.zero)
        {
            dirToGo.Normalize();
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo, ForceMode.VelocityChange);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ball"))
        {
            AddReward(0.5f); // Positive reward for ball interaction
        }
    }
}