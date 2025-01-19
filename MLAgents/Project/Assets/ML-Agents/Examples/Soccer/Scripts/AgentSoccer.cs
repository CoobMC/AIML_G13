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
        // 1. Build the current observation array
        float[] currentObs = BuildCurrentObservations();

        // 2. Enqueue it in our observationHistory
        observationHistory.Enqueue(currentObs);

        // 3. Truncate if needed
        if (observationHistory.Count > observationHistorySize)
        {
            observationHistory.Dequeue();
        }

        // 4. Add *all* stored observations to the sensor
        //    This effectively stacks the last N timesteps.
        foreach (var pastObsArray in observationHistory)
        {
            foreach (float val in pastObsArray)
            {
                sensor.AddObservation(val);
            }
        }
    }

    private float[] BuildCurrentObservations()
{
    var obsList = new List<float>();

    // 1. Sound Intensity
    float soundIntensity = 0f;
    if (ball != null)
    {
        var audioSource = ball.GetComponent<AudioSource>();
        if (audioSource != null && audioSource.isPlaying)
        {
            float distance = Vector3.Distance(transform.position, ball.transform.position);
            soundIntensity = 1.0f / (distance + 1.0f); // Normalize
        }
    }
    obsList.Add(soundIntensity);

    // 2. Relative Ball Position
    Vector3 relativeBallPosition = ball.transform.position - transform.position;
    obsList.Add(relativeBallPosition.x);
    obsList.Add(relativeBallPosition.y);
    obsList.Add(relativeBallPosition.z);

    // 3. Ball Velocity
    if (ball.TryGetComponent<Rigidbody>(out Rigidbody ballRb))
    {
        obsList.Add(ballRb.velocity.x);
        obsList.Add(ballRb.velocity.y);
        obsList.Add(ballRb.velocity.z);
    }
    else
    {
        obsList.Add(0f);
        obsList.Add(0f);
        obsList.Add(0f);
    }

    // 4. Agent Velocity
    obsList.Add(agentRb.velocity.x);
    obsList.Add(agentRb.velocity.y);
    obsList.Add(agentRb.velocity.z);

    // 5. Distance to Goals
    SoccerEnvController envController = FindObjectOfType<SoccerEnvController>();
    if (envController != null)
    {
        Vector3 blueGoalPosition   = envController.ball.transform.position + new Vector3(-15, 0, 0);
        Vector3 purpleGoalPosition = envController.ball.transform.position + new Vector3( 15, 0, 0);

        float distanceToBlueGoal   = Vector3.Distance(transform.position, blueGoalPosition);
        float distanceToPurpleGoal = Vector3.Distance(transform.position, purpleGoalPosition);

        obsList.Add(distanceToBlueGoal);
        obsList.Add(distanceToPurpleGoal);
    }

    // 6. Teammates
    {
        var teammates = envController.AgentsList.FindAll(a => a.Agent.team == team).ToArray();
        foreach (var t in teammates)
        {
            if (t.Agent != this)
            {
                var relPos = t.Agent.transform.position - transform.position;
                obsList.Add(relPos.x);
                obsList.Add(relPos.y);
                obsList.Add(relPos.z);
            }
        }
    }

    // 7. Opponents
    {
        var opponents = envController.AgentsList.FindAll(a => a.Agent.team != team).ToArray();
        foreach (var op in opponents)
        {
            var relPos = op.Agent.transform.position - transform.position;
            obsList.Add(relPos.x);
            obsList.Add(relPos.y);
            obsList.Add(relPos.z);
        }
    }

    // Convert to float array
    return obsList.ToArray();
}

   public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // 1. Proximity to Ball
        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        float proximityReward = Mathf.Clamp(1 - (distanceToBall / maxAllowedDistance), 0, 1);
        AddReward(proximityReward * 0.005f); // Encourage moving closer to the ball

        // 2. Ball Interaction
        if (hasInteractedWithBall)
        {
            //AddReward(0.5f); // Reward for touching the ball
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
        AddReward(-0.0001f); // Small penalty for each step to encourage quicker decisions

        // 7. Field Awareness
        float distanceFromFieldCenter = Vector3.Distance(transform.position, fieldCenter);
        if (distanceFromFieldCenter > maxAllowedDistance)
        {
            AddReward(-0.04f); // Penalize for moving too far from the center
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
}
