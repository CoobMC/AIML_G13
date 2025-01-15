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
    public int observationHistorySize = 1; // History size set to 1 for total space size of 6

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
        float[] currentObservations = GetCurrentObservations();
        sensor.AddObservation(currentObservations);
        
        if (observationHistory.Count >= observationHistorySize)
        {
            observationHistory.Dequeue();
        }
        observationHistory.Enqueue(currentObservations);

        foreach (var pastObservation in observationHistory)
        {
            sensor.AddObservation(pastObservation);
        }

        Debug.Log($"Observations Added: {currentObservations.Length + (observationHistorySize * currentObservations.Length)}");
    }


    private float[] GetCurrentObservations()
    {
        // Get the relative position of the agent to the ball
        Vector3 relativePosition = ball != null
            ? (ball.transform.localPosition - transform.localPosition) / maxAllowedDistance
            : Vector3.zero;

        return new float[] { relativePosition.x, relativePosition.y, relativePosition.z };
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