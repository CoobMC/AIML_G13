using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;      // Observation handling (e.g., VectorSensor)
using System.Collections.Generic;  // Data structures like Queue for observation history

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

    // Walking sound variables
    public AudioClip walkingSound1; // First step sound
    public AudioClip walkingSound2; // Second step sound
    private bool playFirstSound = true; // Track which sound to play
    private float stepCooldown = 0.1f; // Time between steps (in seconds)
    private float stepTimer = 0; // Timer for stepping
    private AudioSource audioSource; // Reference to AudioSource
    public RayPerceptionSensorComponent3D soundSensor;

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }
    // TODO
    // Queue to store past observations
    private Queue<float[]> observationHistory;

    // Number of past observations to store
    public int observationHistorySize = 4;

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

        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        soundSensor.enabled = false;

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

        if (observationHistory == null)
        {
            Debug.LogError("observationHistory is null. Check if Initialize() is called properly.");
            return;
        }

        // Collect the current observations
        float[] currentObservations = GetCurrentObservations();

        if (currentObservations == null || currentObservations.Length == 0)
        {
            Debug.LogError("GetCurrentObservations returned null or empty array.");
            return;
        }

        // Add the current observations to the sensor
        sensor.AddObservation(currentObservations);

        // Enforce the size constraint of the observation history
        if (observationHistory.Count >= observationHistorySize)
        {
            observationHistory.Dequeue(); // Remove the oldest observation
        }

        // Add the current observations to the history queue
        observationHistory.Enqueue(currentObservations);

        // Add past observations to the sensor
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

        //m_KickPower = 0f;

        var forwardAxis = act[0];
        var lateralAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo += transform.forward * m_ForwardSpeed;
                //m_KickPower = 1f;
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
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            AddReward(-m_Existential);
        }
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

     void FixedUpdate()
    {
        // Check agent velocity
        float velocityMagnitude = agentRb.velocity.magnitude;

        if (velocityMagnitude > 0.3f) // Movement threshold
        {
            stepTimer -= Time.fixedDeltaTime;

            if (stepTimer <= 0)
            {
                // Alternate between the two sounds
                if (!audioSource.isPlaying)
                {
                    if (playFirstSound && walkingSound1 != null)
                    {
                        audioSource.clip = walkingSound1;
                    }
                    else if (walkingSound2 != null)
                    {
                        audioSource.clip = walkingSound2;
                    }

                    audioSource.Play();
                    

                    // Toggle sound for next step
                    playFirstSound = !playFirstSound;

                    // Set a cooldown time between steps
                    stepCooldown = Mathf.Clamp(0.2f / velocityMagnitude, 0.2f, 1.0f); // Adjust range for timing
                    stepTimer = stepCooldown;
                }
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            stepTimer = 0; // Reset the timer
        }
    }
}