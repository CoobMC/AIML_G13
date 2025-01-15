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

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Reward for proximity to the ball
        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        float rewardForProximity = Mathf.Clamp(1 - (distanceToBall / maxAllowedDistance), 0, 1);
        AddReward(rewardForProximity * 0.01f);

        // Penalize for moving too far from the center
        float distanceFromCenter = Vector3.Distance(transform.position, fieldCenter);
        if (distanceFromCenter > maxAllowedDistance)
        {
            AddReward(-0.01f);
        }

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

        transform.Rotate(rotateDir, Time.deltaTime * 50f); // Reduced rotation speed
        agentRb.AddForce(dirToGo * 5f, ForceMode.VelocityChange); // Reduced movement force
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ball"))
        {
            AddReward(0.5f); // Positive reward for ball interaction
        }
    }
}
