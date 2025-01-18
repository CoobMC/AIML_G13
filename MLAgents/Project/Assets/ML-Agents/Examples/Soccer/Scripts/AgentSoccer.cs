using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
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

    public float rotSign; // For team-based rotation
    public GameObject ball; // Reference to the ball
    public Vector3 fieldCenter = Vector3.zero;
    public float maxAllowedDistance = 15f;
    [HideInInspector]
    public Rigidbody agentRb;
    private AgentSoccer lastAgentTouched;

    private bool hasInteractedWithBall = false; // Tracks if the agent interacted with the ball
    public Vector3 initialPos;

    // Observation history memory
    private Queue<float[]> observationHistory = new Queue<float[]>(); // Memory for past observations
    public int observationHistorySize = 4;

    public Transform ballTransform;  // Reference to the ball's Transform

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        ball = GameObject.FindWithTag("ball");

        if (ball == null)
        {
            Debug.LogError("Ball not found! Ensure it's tagged correctly.");
        }

        initialPos = transform.position;
        observationHistory.Clear(); // Clear the observation history
    }

    public override void OnEpisodeBegin()
    {
        observationHistory.Clear();
        transform.position = initialPos;
        transform.rotation = Quaternion.identity;
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;

        if (ball != null)
        {
            ball.transform.position = new Vector3(0, 0.5f, 0);
        }
    }


   public override void CollectObservations(VectorSensor sensor)
    {
        int observationCount = 0;

        // 1. Sound Intensity
        float soundIntensity = 0f;
        if (ball != null)
        {
            var audioSource = ball.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.isPlaying)
            {
                float distance = Vector3.Distance(transform.position, ball.transform.position);
                soundIntensity = 1.0f / (distance + 1.0f); // Normalize and avoid division by zero
            }
        }
        sensor.AddObservation(soundIntensity); // 1 observation
        observationCount++;

        // 2. Relative Ball Position
        Vector3 relativeBallPosition = ball.transform.position - transform.position;
        sensor.AddObservation(relativeBallPosition); // 3 observations (Vector3)
        observationCount += 3;

        // 3. Ball Velocity
        if (ball.TryGetComponent<Rigidbody>(out Rigidbody ballRb))
        {
            sensor.AddObservation(ballRb.velocity); // 3 observations (Vector3)
            observationCount += 3;
        }
        else
        {
            sensor.AddObservation(Vector3.zero); // Add zero velocity if Rigidbody is missing
            observationCount += 3;
        }

        // 4. Agent Velocity
        sensor.AddObservation(agentRb.velocity); // 3 observations (Vector3)
        observationCount += 3;

        // 5. Distance to Goals
        SoccerEnvController envController = FindObjectOfType<SoccerEnvController>();
        if (envController != null)
        {
            Vector3 blueGoalPosition = envController.ball.transform.position + new Vector3(-15, 0, 0);
            Vector3 purpleGoalPosition = envController.ball.transform.position + new Vector3(15, 0, 0);

            float distanceToBlueGoal = Vector3.Distance(transform.position, blueGoalPosition);
            float distanceToPurpleGoal = Vector3.Distance(transform.position, purpleGoalPosition);

            sensor.AddObservation(distanceToBlueGoal); // 1 observation
            sensor.AddObservation(distanceToPurpleGoal); // 1 observation
            observationCount += 2;
        }

        // 6. Teammates' Relative Positions
        SoccerEnvController.PlayerInfo[] teammates = envController.AgentsList.FindAll(a => a.Agent.team == team).ToArray();
        foreach (var teammate in teammates)
        {
            if (teammate.Agent != this) // Exclude self
            {
                Vector3 relativeTeammatePosition = teammate.Agent.transform.position - transform.position;
                sensor.AddObservation(relativeTeammatePosition); // 3 observations (Vector3)
                observationCount += 3;
            }
        }

        // 7. Opponents' Relative Positions
        SoccerEnvController.PlayerInfo[] opponents = envController.AgentsList.FindAll(a => a.Agent.team != team).ToArray();
        foreach (var opponent in opponents)
        {
            Vector3 relativeOpponentPosition = opponent.Agent.transform.position - transform.position;
            sensor.AddObservation(relativeOpponentPosition); // 3 observations (Vector3)
            observationCount += 3;
        }
    }



    private float[] GetCurrentObservations()
    {
        int numRaycasts = 5; // Rays Per Direction from Ray Perception Sensor
        float rayLength = 20f; // Ray Length from Ray Perception Sensor
        float rayAngleStart = -60f; // Maximum Ray Degrees (Half the Field of View)
        float rayAngleEnd = 60f;
        float rayAngleIncrement = (rayAngleEnd - rayAngleStart) / (numRaycasts - 1);

        float[] observations = new float[numRaycasts];

        for (int i = 0; i < numRaycasts; i++)
        {
            float angle = rayAngleStart + i * rayAngleIncrement;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            // Perform the raycast
            if (Physics.Raycast(transform.position + new Vector3(0, 0.5f, 0), direction, out RaycastHit hit, rayLength)) // Start Vertical Offset = 0.5
            {
                float normalizedDistance = hit.distance / rayLength;
                observations[i] = normalizedDistance;
            }
            else
            {
                observations[i] = 1f;
            }
        }

        return observations;
    }

   public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // 1. Proximity to Ball
        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        float proximityReward = Mathf.Clamp(1 - (distanceToBall / maxAllowedDistance), 0, 1);
        AddReward(proximityReward * 0.01f); // Encourage moving closer to the ball

        // 2. Ball Interaction
        if (hasInteractedWithBall)
        {
            AddReward(0.5f); // Reward for touching the ball
            hasInteractedWithBall = false;
        }

        lastAgentTouched = ball.GetComponent<SoccerBallController>().GetLastAgentTouched();

        // 4. Passing to Teammates
        if (lastAgentTouched != null && lastAgentTouched != this)
        {
            if (lastAgentTouched.team == this.team)
            {
                // Passed to a teammate
                AddReward(0.2f);
                //Debug.Log($"{lastAgentTouched.name} passed to teammate {name}");
            }
            else
            {
                // Passed to an opponent
                AddReward(-0.3f);
                //Debug.Log($"{lastAgentTouched.name} passed to opponent {name}");
            }
        }

        // 6. Time Penalty
        AddReward(-0.001f); // Small penalty for each step to encourage quicker decisions

        // 7. Field Awareness
        float distanceFromFieldCenter = Vector3.Distance(transform.position, fieldCenter);
        if (distanceFromFieldCenter > maxAllowedDistance)
        {
            AddReward(-0.05f); // Penalize for moving too far from the center
        }


        // Execute movement
        MoveAgent(actionBuffers.DiscreteActions);
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
                dirToGo += transform.forward;
                break;
            case 2:
                dirToGo += -transform.forward;
                break;
        }

        switch (lateralAxis)
        {
            case 1:
                dirToGo += transform.right;
                break;
            case 2:
                dirToGo += -transform.right;
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

        transform.Rotate(rotateDir, Time.deltaTime * 50f);
        agentRb.AddForce(dirToGo * 5f, ForceMode.VelocityChange); 
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ball"))
        {
            hasInteractedWithBall = true; 
            AddReward(0.5f); // Positive reward for ball interaction
        }
    }
}
