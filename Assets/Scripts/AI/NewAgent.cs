using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class NewAgent : Agent
{
    Rigidbody rbd;
    RayPerceptionSensorComponent3D[] rayPerceptions;

    private Vector3 startPos;
    private Vector3 startRot;
    private float timerSeconds = 0;
    private bool activeTimer = false;

    public GameObject target; // Target in parent environment
    [SerializeField] public GameObject targetPrefab;
    [SerializeField] public bool shouldSpawnOwnTarget = false; // Used for when there is multiple agents
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private Material agentMaterial;
    [SerializeField] public MeshRenderer floorMeshRenderer;
    [SerializeField] public GridWithParams grid;
    [SerializeField] private bool isRandomGrid = false;
    [SerializeField] private bool isRandomRoadblocks;
    [SerializeField] private bool isTimedRoadblocks = false;
    [SerializeField, Range(0, 60)] private float seconds = 30;
    [SerializeField, Tooltip("Is Agent Random Start Position?")] private bool randomStartPos;
    private Material originalMaterial;

    private int randomSeedNo = 0;

    enum observationMethod
    {
        one,
        two,
        three,
        four,
        five,
        six,
        seven
    };

    [SerializeField]
    private observationMethod method = observationMethod.six;

    public override void Initialize()
    {
        rbd = GetComponent<Rigidbody>();
        rayPerceptions = GetComponents<RayPerceptionSensorComponent3D>();
        startPos = transform.localPosition;
        startRot = transform.localEulerAngles;

        originalMaterial = floorMeshRenderer.material;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPos;
        transform.localEulerAngles = startRot;

        RandomiseTargetPos();
        
        rbd.velocity = Vector3.zero;

        if (isRandomGrid)
        {
            BuildRandomGrid();
        }
        if (isRandomRoadblocks)
        {
            grid.BuildGrid();
        }
        if (isTimedRoadblocks)
        {
            timerSeconds = seconds;
            activeTimer = true;
        }
    }

    private void RandomiseTargetPos()
    {
        // Get random building in environment
        int randomX = Random.Range(-Mathf.FloorToInt(grid.parameters.height / 2), grid.parameters.height - Mathf.FloorToInt(grid.parameters.height / 2));
        int randomZ = Random.Range(-Mathf.FloorToInt(grid.parameters.height / 2), grid.parameters.width - Mathf.FloorToInt(grid.parameters.height / 2));

        if (shouldSpawnOwnTarget)
        {
            Destroy(target);
            target = GameObject.Instantiate(targetPrefab);
            target.transform.parent = transform.parent;
            //target.tag = "Goal" + name.Substring(name.Length - 1, 1);

            randomX = Random.Range(-Mathf.FloorToInt(grid.parameters.height / 2), grid.parameters.height - Mathf.FloorToInt(grid.parameters.height / 2));
            randomZ = Random.Range(-Mathf.FloorToInt(grid.parameters.height / 2), grid.parameters.width - Mathf.FloorToInt(grid.parameters.height / 2));

            shouldSpawnOwnTarget = false;
        }

        // Position target in centre of a building so that it is always reachable despite roadblocks
        target.transform.localPosition = new Vector3(randomX * grid.parameters.marginBetweenShapes.x * 2, target.transform.localScale.y / 2, randomZ * grid.parameters.marginBetweenShapes.z * 2);
    }

    public void BuildRandomGrid()
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

    public override void CollectObservations(VectorSensor sensor)
    {
        switch (method)
        {
            case observationMethod.one:
                //First attempt
                sensor.AddObservation(rbd.velocity.x);
                sensor.AddObservation(rbd.velocity.z);
                sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition)); // distance to target

                return;
            case observationMethod.two:
                // Second attempt
                sensor.AddObservation(transform.InverseTransformDirection(rbd.velocity));

                return;
            case observationMethod.three:
                // Third attempt
                sensor.AddObservation(transform.InverseTransformPoint(target.transform.localPosition));

                return;
            case observationMethod.four:
                // Fourth attempt
                sensor.AddObservation(transform.localPosition);
                sensor.AddObservation(target.transform.localPosition);

                sensor.AddObservation(rbd.velocity.x);
                sensor.AddObservation(rbd.velocity.z);

                return;
            case observationMethod.five:
                // Fifth attempt
                sensor.AddObservation(transform.InverseTransformDirection(rbd.velocity));
                sensor.AddObservation(transform.InverseTransformPoint(target.transform.localPosition));
                sensor.AddObservation(transform.InverseTransformPoint(transform.localPosition));

                return;
            case observationMethod.six:
                // Sixth attempt
                sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition));

                sensor.AddObservation(rbd.velocity.x);
                sensor.AddObservation(rbd.velocity.z);

                sensor.AddObservation(transform.forward);
                sensor.AddObservation((target.transform.position - transform.position).normalized); // direction to target

                return;
            case observationMethod.seven:
                // Seventh attempt
                sensor.AddObservation(transform.localPosition);
                sensor.AddObservation(target.transform.localPosition);

                sensor.AddObservation(rbd.velocity.x);
                sensor.AddObservation(rbd.velocity.z);

                sensor.AddObservation(transform.forward);
                sensor.AddObservation((target.transform.position - transform.position).normalized); // direction to target

                return;
            default: break;
        }
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
        int number; 
        bool newAgent = int.TryParse(name.Substring(name.Length - 1, 1), out number);

        if (other.CompareTag("Goal"))
        {
            SetReward(+2f);
            StartCoroutine(ChangeFloorMaterial(winMaterial));
            EndEpisode();
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

    private void Update()
    {
        if (activeTimer)
        {
            timerSeconds -= Time.deltaTime;

            if (timerSeconds <= 0.0f)
            {
                activeTimer = false;
                TimedRoadblocks();
            }
        }
    }

    private void TimedRoadblocks()
    {
        Debug.Log("Here");
        grid.ClearRoadblocks();
        grid.BuildRoadblocks();
        timerSeconds = seconds;
        activeTimer = true;
    }

    public void ForecEndEpisode()
    {
        EndEpisode();
    }
}
