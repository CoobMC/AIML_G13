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
    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

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
    public AudioClip walkingSound1; 
    public AudioClip walkingSound2; 
    private bool playFirstSound = true;
    private float stepCooldown = 0.1f;
    private float stepTimer = 0; 
    private AudioSource audioSource; 
    public RayPerceptionSensorComponent3D soundSensor;

    public override void Initialize()
    {
        RayPerceptionSensorComponent3D[] sensors = GetComponents<RayPerceptionSensorComponent3D>();
        if (sensors.Length > 1)
        {
            sensors[1].RayLength = 0;
        }
        else
        {
            Debug.LogError("Less than 2 RayPerceptionSensor3D components found!");
        }

        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        m_Existential = envController != null
            ? 1f / envController.MaxEnvironmentSteps
            : 1f / MaxStep;

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        team = m_BehaviorParameters.TeamId == (int)Team.Blue ? Team.Blue : Team.Purple;
        initialPos = new Vector3(transform.position.x + (team == Team.Blue ? -5f : 5f), .5f, transform.position.z);
        rotSign = team == Team.Blue ? 1f : -1f;

        m_LateralSpeed = position == Position.Goalie ? 1.0f : 0.3f;
        m_ForwardSpeed = position == Position.Striker ? 1.3f : 1.0f;

        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        audioSource = GetComponent<AudioSource>();
        soundSensor.enabled = false;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = act[0];
        var lateralAxis = act[1];
        var rotateAxis = act[2];

        if (forwardAxis == 1) dirToGo += transform.forward * m_ForwardSpeed;
        else if (forwardAxis == 2) dirToGo += -transform.forward * m_ForwardSpeed;

        if (lateralAxis == 1) dirToGo += transform.right * m_LateralSpeed;
        else if (lateralAxis == 2) dirToGo += -transform.right * m_LateralSpeed;

        if (dirToGo != Vector3.zero) dirToGo.Normalize();

        rotateDir = rotateAxis switch
        {
            1 => transform.up * -1f,
            2 => transform.up * 1f,
            _ => Vector3.zero
        };

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed, ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(position == Position.Goalie ? m_Existential : -m_Existential);
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

    void FixedUpdate()
    {
        float velocityMagnitude = agentRb.velocity.magnitude;
        if (velocityMagnitude > 0.3f)
        {
            stepTimer -= Time.fixedDeltaTime;
            if (stepTimer <= 0 && !audioSource.isPlaying)
            {
                audioSource.clip = playFirstSound ? walkingSound1 : walkingSound2;
                if (audioSource.clip != null) audioSource.Play();
                playFirstSound = !playFirstSound;
                stepCooldown = Mathf.Clamp(0.2f / velocityMagnitude, 0.2f, 1.0f);
                stepTimer = stepCooldown;
            }
        }
        else if (audioSource.isPlaying)
        {
            audioSource.Stop();
            stepTimer = 0;
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("ball"))
        {
            AddReward(.2f * m_BallTouch);
            var dir = (c.contacts[0].point - transform.position).normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * k_Power * (position == Position.Goalie ? 1f : m_KickPower));
        }
    }
}
