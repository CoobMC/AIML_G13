using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections;
public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
	public GameObject striker1;
	public GameObject striker2;
	public GameObject striker3;
	public GameObject striker4;
	
	private RayPerceptionSensorComponent3D sensor1;
	private RayPerceptionSensorComponent3D sensor2;
	private RayPerceptionSensorComponent3D sensor3;
	private RayPerceptionSensorComponent3D sensor4;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag; //will be used to check if collided with purple goal
    public string blueGoalTag; //will be used to check if collided with blue goal
	private bool isHearing;
	

    void Start()
    {
        envController = area.GetComponent<SoccerEnvController>();
		RayPerceptionSensorComponent3D[] sensors1 = striker1.GetComponents<RayPerceptionSensorComponent3D>();
		sensor1 = sensors1[1];
		RayPerceptionSensorComponent3D[] sensors2 = striker2.GetComponents<RayPerceptionSensorComponent3D>();
		sensor2 = sensors2[1];
		RayPerceptionSensorComponent3D[] sensors3 = striker3.GetComponents<RayPerceptionSensorComponent3D>();
		sensor3 = sensors3[1];
		RayPerceptionSensorComponent3D[] sensors4 = striker4.GetComponents<RayPerceptionSensorComponent3D>();
		sensor4 = sensors4[1];
		isHearing = false;
    }

    void OnCollisionEnter(Collision col)
    {
		if (!isHearing){
			StartCoroutine(SimulateHearing());
		}
		
        if (col.gameObject.CompareTag(purpleGoalTag)) //ball touched purple goal
        {
            envController.GoalTouched(Team.Blue);
        }
        if (col.gameObject.CompareTag(blueGoalTag)) //ball touched blue goal
        {
            envController.GoalTouched(Team.Purple);
        }
    }
	
    IEnumerator SimulateHearing()
    {
        isHearing = true;

        // Set RayLength to 7
        sensor1.RayLength = 7;
        sensor2.RayLength = 7;
        sensor3.RayLength = 7;
        sensor4.RayLength = 7;

        // Wait for 1 second (or the duration you want the "hearing" effect to last)
        yield return new WaitForSeconds(1f);

        // Set RayLength back to 0 after 1 second
        sensor1.RayLength = 0;
        sensor2.RayLength = 0;
        sensor3.RayLength = 0;
        sensor4.RayLength = 0;

        isHearing = false; // Allow the effect to be triggered again
    }
}
