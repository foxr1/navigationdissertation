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

    [SerializeField] private GameObject target;
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;
    [SerializeField] private GridWithParams grid;
    [SerializeField] private bool isRandomGrid;
    [SerializeField, Tooltip("Is Agent Random Start Position?")] private bool randomStartPos;
    [SerializeField, Range(-250, 250)] private int minSpawnLoc;
    [SerializeField, Range(-250, 250)] private int maxSpawnLoc;
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
       // transform.LookAt(target.transform.localPosition);
        transform.localPosition = startPos;
        transform.localEulerAngles = startRot; 
        
        target.transform.localPosition = new Vector3(Random.Range(minSpawnLoc, maxSpawnLoc), 10, Random.Range(minSpawnLoc, maxSpawnLoc));// For larger grid
        //target.transform.localPosition = new Vector3(Random.Range(-90, 0), 0, Random.Range(-170, 0)); // For small grid
        //target.transform.localPosition = new Vector3(Random.Range(0, 3750), 0, Random.Range(0, 3750)); // For big grid
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
        sensor.AddObservation(transform.InverseTransformDirection(rbd.velocity));
        //sensor.AddObservation(target.transform.localPosition);   
        //sensor.AddObservation(transform.localPosition);
        //sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition)); // distance to target#
        //sensor.AddObservation(transform.InverseTransformPoint(target.transform.localPosition));
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
        if (other.CompareTag("Goal"))
        {
            SetReward(+2f);
            StartCoroutine(ChangeFloorMaterial(winMaterial));
            //SetRandomGoalPosition();
            //target.transform.localPosition = new Vector3(Random.Range(-215, 55), 10, Random.Range(-215, 55));// For larger grid
            EndEpisode();
        }
        if (other.CompareTag("Wall"))
        {
            SetReward(-1f);
            StartCoroutine(ChangeFloorMaterial(loseMaterial));
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
