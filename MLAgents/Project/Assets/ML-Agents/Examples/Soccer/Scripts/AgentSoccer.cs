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
    public RayPerceptionSensorComponent3D soundSensor;

    // Reference to SoccerEnvController (Class-level variable)
    private SoccerEnvController envController;

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    public override void Initialize()
    {
        // Correctly assign the class-level envController
        envController = GetComponentInParent<SoccerEnvController>();

        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
            Debug.LogError("SoccerEnvController not found in parent!");
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
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource component not found on AgentSoccer GameObject.");
        }

        // Ensure soundSensor is assigned
        if (soundSensor != null)
        {
            soundSensor.enabled = false;
        }
        else
        {
            Debug.LogWarning("soundSensor is not assigned in the Inspector.");
        }
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
                m_KickPower = 1f; // Ready to kick when moving forward
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

    /// <summary>
    /// Handles collision events with other objects.
    /// Specifically updates possession when colliding with the ball.
    /// </summary>
    /// <param name="c">Collision information.</param>
    void OnCollisionEnter(Collision c)
    {
        // Check if the collided object is the ball
        if (c.gameObject.CompareTag("ball"))
        {
            // Reward for touching the ball
            AddReward(0.5f * m_BallTouch);

            // Calculate and apply force to the ball
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * k_Power * m_KickPower);

            // === Task 1: Update Possession Tracking ===
            if (envController != null)
            {
                AgentSoccer previousPlayer = envController.GetCurrentPossessor();
                envController.SetLastPossessor(previousPlayer);
                envController.SetCurrentPossessor(this);

                // === Task 2: Pass Detection and Rewards ===
                if (previousPlayer != null && previousPlayer != this)
                {
                    if (previousPlayer.team == team)
                    {
                        // Successful pass to a teammate
                        AddReward(0.1f);
                        envController.SetPassOccurred();
                    }
                    else
                    {
                        // Ball taken from opponent
                        AddReward(0.2f);
                        envController.ResetPassOccurred();
                    }
                }
                // ==========================================
            }
            // ===========================================
        }

        // Handle collisions with goals or other objects if necessary
        // Existing collision handling code...
    }
}
