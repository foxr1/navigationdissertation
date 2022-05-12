using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.UI;

public class CreateEnvironments : MonoBehaviour
{
    [SerializeField]
    private GameObject mainCamera;
    [SerializeField]
    private GameObject agentPrefab;
    [SerializeField]
    private GameObject environmentPrefab;
    [SerializeField, Range(1, 100), Tooltip("Enter a square number")]
    private int noOfEnvironments;
    [SerializeField, Range(1, 10)]
    private int noOfAgents;

    private GameObject plane;
    private Bounds bounds;

    // Start is called before the first frame update
    void Start()
    {
        plane = environmentPrefab.transform.GetChild(1).gameObject;
        bounds = plane.GetComponent<MeshRenderer>().bounds;

        SetupEnvironments();
        MoveCamera();
    }

    public void SetupEnvironments()
    {
        float rows = Mathf.Sqrt(noOfEnvironments);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject environment = GameObject.Instantiate(environmentPrefab);
                environment.transform.position = new Vector3(i * (bounds.size.x * 1.5f), 0, j * (bounds.size.z * 1.5f));

                if (noOfAgents > 1)
                {
                    SetupAgents(environment);
                } 
            }
        }
    }

    private void MoveCamera()
    {
        float position = ((bounds.size.x * Mathf.Sqrt(noOfEnvironments)) + ((bounds.size.x / 2) * (Mathf.Sqrt(noOfEnvironments) - 1))) / 2 - (bounds.size.x / 2);

        mainCamera.transform.position = new Vector3(position, position * 2.5f, position);
    }

    private void SetupAgents(GameObject environment)
    {
        int inv = 1; // top flip sides of grid
        for (int i = 0; i < noOfAgents - 1; i++)
        {
            RayPerceptionSensorComponent3D[] rayPerceptions = agentPrefab.GetComponents<RayPerceptionSensorComponent3D>();
            agentPrefab.GetComponent<NewAgent>().grid = environment.GetComponentInChildren<GridWithParams>();
            agentPrefab.GetComponent<NewAgent>().shouldSpawnOwnTarget = true;
            agentPrefab.GetComponent<NewAgent>().floorMeshRenderer = environment.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>();
            GameObject agent = GameObject.Instantiate(agentPrefab);
            agent.name = $"Agent_{i}";
            agent.transform.parent = environment.transform;
            agent.tag = "Agent";

            Transform randomStartPos = NewAgent.RandomAgentStartPos();

            agent.transform.localPosition = randomStartPos.position;
            agent.transform.eulerAngles = randomStartPos.eulerAngles;
        
            inv *= -1;
        }
    }
}
