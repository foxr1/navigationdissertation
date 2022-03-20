using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class NewAgent : Agent
{
    Rigidbody rbd;
    RayPerceptionSensorComponent3D rayPerception;

    private Vector3 startPos;
    private Vector3 startRot;

    public GameObject target; // Target in parent environment
    [SerializeField] public GameObject targetPrefab;
    [SerializeField] public bool shouldSpawnOwnTarget = false; // Used for when there is multiple agents
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private Material agentMaterial;
    [SerializeField] public MeshRenderer floorMeshRenderer;
    [SerializeField] public GridWithParams grid;
    [SerializeField] private bool isRandomGrid;
    [SerializeField, Tooltip("Is Agent Random Start Position?")] private bool randomStartPos;
    private Material originalMaterial;

    private int randomSeedNo = 0;

    public override void Initialize()
    {
        rbd = GetComponent<Rigidbody>();
        rayPerception = GetComponent<RayPerceptionSensorComponent3D>();
        startPos = transform.localPosition;
        startRot = transform.localEulerAngles;

        originalMaterial = floorMeshRenderer.material;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPos;
        transform.localEulerAngles = startRot;

        // Get random building in environment
        int randomX = Random.Range(-Mathf.FloorToInt(grid.parameters.height / 2), grid.parameters.height - Mathf.FloorToInt(grid.parameters.height / 2)); 
        int randomZ = Random.Range(-Mathf.FloorToInt(grid.parameters.height / 2), grid.parameters.width - Mathf.FloorToInt(grid.parameters.height / 2));

        if (shouldSpawnOwnTarget)
        {
            Destroy(target);
            target = GameObject.Instantiate(targetPrefab);
            target.transform.parent = transform.parent;
            target.tag = "Goal" + name.Substring(name.Length - 1, 1);
        }

        // Position target in centre of a building so that it is always reachable despite roadblocks
        target.transform.localPosition = new Vector3(randomX * grid.parameters.marginBetweenShapes.x * 2, target.transform.localScale.y / 2, randomZ * grid.parameters.marginBetweenShapes.z * 2);
        
        rbd.velocity = Vector3.zero;

        if (isRandomGrid)
        {
            if (randomSeedNo > 99)
            {
                randomSeedNo = 0;
            } 
            else
            {
                randomSeedNo++;
            }
            grid.parameters.randomSeed = randomSeedNo;

            grid.BuildGrid();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // First attempt
        //sensor.AddObservation(rbd.velocity.x);
        //sensor.AddObservation(rbd.velocity.z);
        //sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition)); // distance to target

        // Second attempt
        //sensor.AddObservation(transform.InverseTransformDirection(rbd.velocity)); 

        // Third attempt
        //sensor.AddObservation(transform.InverseTransformPoint(target.transform.localPosition));

        // Fourth attempt
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target.transform.localPosition);

        sensor.AddObservation(rbd.velocity.x);
        sensor.AddObservation(rbd.velocity.z);

        // Fifth attempt
        //sensor.AddObservation(transform.InverseTransformDirection(rbd.velocity)); 
        //sensor.AddObservation(transform.InverseTransformPoint(target.transform.localPosition));
        //sensor.AddObservation(transform.InverseTransformPoint(transform.localPosition));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-1f / MaxStep);
        MoveAgent(actions.DiscreteActions);
    }

    private void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        rbd.AddForce(dirToGo * 2f, ForceMode.VelocityChange);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!shouldSpawnOwnTarget)
        {
            if (other.CompareTag("Goal"))
            {
                SetReward(+2f);
                StartCoroutine(ChangeFloorMaterial(winMaterial));
                EndEpisode();
            }
        }
        else
        {
            if (other.CompareTag("Goal" + name.Substring(name.Length - 1, 1))) // if it is goal for other agent
            {
                SetReward(+2f);
                StartCoroutine(ChangeFloorMaterial(loseMaterial));
                EndEpisode();
            }
        }
        
        if (other.CompareTag("Wall"))
        {
            SetReward(-1f);
            StartCoroutine(ChangeFloorMaterial(loseMaterial));
            EndEpisode();
        }
        if (other.CompareTag("Agent"))
        {
            SetReward(-1f);
            StartCoroutine(ChangeFloorMaterial(agentMaterial));
            EndEpisode();
        }
    }

    private IEnumerator ChangeFloorMaterial(Material material)
    {
        floorMeshRenderer.material = material;

        yield return new WaitForSeconds(1.5f);

        floorMeshRenderer.material = originalMaterial;
    }
}
