using System.Collections.Generic;
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
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    // Number of rays for forward raycasts
    private int numRaycasts = 11;

    // Length of the rays
    private float rayLength = 10f;

    // Queue to store past observations
    private Queue<float[]> observationHistory;

    // Number of past observations to store
    public int observationHistorySize = 4;

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
        if (m_BehaviorParameters == null)
        {
            Debug.LogError("BehaviorParameters component is missing on the agent.");
        }

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
        if (m_SoccerSettings == null)
        {
            Debug.LogError("SoccerSettings object is missing in the scene.");
        }

        agentRb = GetComponent<Rigidbody>();
        if (agentRb == null)
        {
            Debug.LogError("Rigidbody component is missing from the agent!");
        }
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        if (m_ResetParams == null)
        {
            Debug.LogError("EnvironmentParameters are not set in the Academy.");
        }

        observationHistory = new Queue<float[]>(observationHistorySize > 0 ? observationHistorySize : 1);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (sensor == null)
        {
            Debug.LogError("VectorSensor is null. Ensure it is correctly set up in the Behavior Parameters.");
            return;
        }

        // Collect current observations
        float[] currentObservations = GetCurrentObservations();
        if (currentObservations != null)
        {
            // Add the current observations to the sensor
            sensor.AddObservation(currentObservations);

            // Add the current observations to the history queue
            if (observationHistory.Count >= observationHistorySize)
            {
                observationHistory.Dequeue(); // Remove the oldest observation
            }
            observationHistory.Enqueue(currentObservations); // Add the latest observation
        }
        else
        {
            Debug.LogWarning("Current observations are null.");
        }

        // Add historical observations to the sensor
        foreach (var pastObservation in observationHistory)
        {
            if (pastObservation != null)
            {
                sensor.AddObservation(pastObservation);
            }
            else
            {
                Debug.LogWarning("A past observation is null, skipping...");
            }
        }

        //Debug.Log($"Observation history size: {observationHistory.Count}");
    }





    private float[] GetCurrentObservations()
    {
        float[] observations = new float[numRaycasts];

        float rayAngleStart = -60f; // Leftmost ray angle
        float rayAngleEnd = 60f;    // Rightmost ray angle
        float rayAngleIncrement = (rayAngleEnd - rayAngleStart) / (numRaycasts - 1);

        for (int i = 0; i < numRaycasts; i++)
        {
            float angle = rayAngleStart + i * rayAngleIncrement;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, rayLength))
            {
                float normalizedDistance = hit.distance / rayLength;
                observations[i] = normalizedDistance;
                //Debug.Log($"Ray {i} hit: {hit.collider.name}, Distance: {hit.distance}");
            }
            else
            {
                observations[i] = 1f; // Max distance if nothing is hit
                //Debug.Log($"Ray {i} did not hit anything.");
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
        if (c.gameObject.CompareTag("ball"))
        {
            if (c.gameObject == null)
            {
                Debug.LogWarning("Ball object is null, skipping collision handling.");
                return;
            }

            var force = k_Power * m_KickPower;
            if (position == Position.Goalie)
            {
                force = k_Power;
            }

            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            Rigidbody ballRb = c.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.AddForce(dir * force);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

}
