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

    // Walking sound variables
    public AudioClip walkingSound1; // First step sound
    public AudioClip walkingSound2; // Second step sound
    private bool playFirstSound = true; // Track which sound to play
    private float stepCooldown = 0.1f; // Time between steps (in seconds)
    private float stepTimer = 0; // Timer for stepping
    private AudioSource audioSource; // Reference to AudioSource
    private RayPerceptionSensorComponent3D soundSensor;

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

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
        RayPerceptionSensorComponent3D[] sensors = GetComponents<RayPerceptionSensorComponent3D>();

        // Find the one with the specific name
        foreach (var sensor in sensors)
        {
            if (sensor.name == "soundSensor") // Replace with the actual name
            {
                soundSensor = sensor;
                break;
            }
        }
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
                    //soundSensor.enabled = true;

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