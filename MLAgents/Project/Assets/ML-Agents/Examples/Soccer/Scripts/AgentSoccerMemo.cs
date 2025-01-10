using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;      // Observation handling (e.g., VectorSensor)
using System.Collections.Generic;  // Data structures like Queue for observation history

public class AgentSoccerMemo : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
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

    // TODO
    // Queue to store past observations
    private Queue<float[]> observationHistory;

    // Number of past observations to store
    public int observationHistorySize = 4;

    public Transform ballTransform;  // Reference to the ball's Transform
    public float maxDistanceToBall = 10f;  // Maximum distance for normalizing proximity reward

    public override void Initialize()
    {
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
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // TODO
        // Initialize the observation history queue
        observationHistory = new Queue<float[]>();

        // Validate the observation history size
        if (observationHistorySize <= 0)
        {
            observationHistorySize = 1; // Default to 1 if invalid
            Debug.LogWarning("Observation history size set to default of 1.");
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (sensor == null)
        {
            Debug.LogError("VectorSensor is null. Ensure Vector Observation Space Size is set correctly in Behavior Parameters.");
            return;
        }

        // Current relative position of the ball
        Vector3 relativeBallPosition = ballTransform.position - transform.position;
        sensor.AddObservation(relativeBallPosition);

        // Add current and historical raycast observations
        float[] currentObservations = GetCurrentObservations();
        if (currentObservations == null || currentObservations.Length == 0)
        {
            Debug.LogError("GetCurrentObservations returned null or empty array.");
            return;
        }

        sensor.AddObservation(currentObservations);

        // Enforce observation history size
        if (observationHistory.Count >= observationHistorySize)
        {
            observationHistory.Dequeue(); // Remove the oldest observation
        }

        observationHistory.Enqueue(currentObservations);

        // Add historical observations to the sensor
        foreach (var pastObservation in observationHistory)
        {
            sensor.AddObservation(pastObservation);
        }
    }

    private float[] GetCurrentObservations()
    {
        int numRaycasts = 5; // Rays Per Direction from Ray Perception Sensor
        float rayLength = 20f; // Ray Length from Ray Perception Sensor
        float rayAngleStart = -60f; // Maximum Ray Degrees (Half the Field of View)
        float rayAngleEnd = 60f;
        float rayAngleIncrement = (rayAngleEnd - rayAngleStart) / (numRaycasts - 1); // Increment per ray

        float[] observations = new float[numRaycasts]; // Array to hold raycast distances

        for (int i = 0; i < numRaycasts; i++)
        {
            float angle = rayAngleStart + i * rayAngleIncrement;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            // Perform the raycast
            if (Physics.Raycast(transform.position + new Vector3(0, 0.5f, 0), direction, out RaycastHit hit, rayLength)) // Start Vertical Offset = 0.5
            {
                float normalizedDistance = hit.distance / rayLength; // Normalize distance to [0, 1]
                observations[i] = normalizedDistance;
            }
            else
            {
                observations[i] = 1f; // Maximum distance (no hit)
            }
        }

        return observations;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var lateralAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo += transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
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

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // TODO Proximity reward: Encourage the agent to stay close to the ball
        float distanceToBall = Vector3.Distance(transform.position, ballTransform.position);
        AddReward(1f - Mathf.Clamp01(distanceToBall / maxDistanceToBall));

        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }
    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }
}
