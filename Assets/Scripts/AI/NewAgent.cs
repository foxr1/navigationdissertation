using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;

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
    [SerializeField] private bool isRandomGrid = false; // Randomise entire city with roadblocks
    [SerializeField] private bool isRandomRoadblocks; // Only randomise roadblocks instead of entire city
    [SerializeField] private bool isTimedRoadblocks = false; // Randomise the roadblocks on a timer instead of at end of episode
    [SerializeField] private bool isRandomStartPosition = false; // Should the agent start in a random position at the start of an episode
    [SerializeField, Range(0, 60)] private float seconds = 30; // Timer for roadblocks
    private Material originalMaterial;

    private int randomSeedNo = 0;

    // Variables for comparison scene
    private float episodeTimer = 0;
    private float totalTime = 0;
    private int episodesCompleted = 0;
    [SerializeField]
    private Text lastEpisodeCompletedText;
    [SerializeField]
    private Text meanTimeText;
    [SerializeField]
    private Text episodesCompletedText;

    enum observationMethod
    {
        one,
        two,
        three,
        four,
        five,
        six,
        seven,
        eight
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
        if (isRandomStartPosition)
        {
            Transform newPos = RandomAgentStartPos();
            transform.localPosition = newPos.position;
            transform.localEulerAngles = newPos.eulerAngles;
        }
        else
        {
            transform.localPosition = startPos;
            transform.localEulerAngles = startRot;
        }
        
        RandomiseTargetPos();
        
        rbd.velocity = Vector3.zero;

        if (isRandomGrid)
        {
            BuildRandomGrid();
        }
        if (isRandomRoadblocks)
        {
            grid.ClearRoadblocks();
            grid.BuildRoadblocks();
        }
        if (isTimedRoadblocks)
        {
            timerSeconds = seconds;
            activeTimer = true;
        }
    }

    // Code to determine a suitable place for the agent to start around the edge of the city
    public static Transform RandomAgentStartPos()
    {
        Transform newPos = new GameObject().transform;

        float randomXRange = Random.Range(-190, 190);
        float randomZRange = Random.Range(-190, 190);
        float[] randomXZs = new float[2];
        randomXZs[0] = randomXRange;
        randomXZs[1] = randomZRange;

        int randomIndex = Random.Range(0, randomXZs.Length);
        float result = randomXZs[randomIndex];

        if (result == randomXRange)
        {
            float probability = Random.Range(0.0f, 1.0f);

            if (probability > 0.5)
            {
                newPos.localPosition = new Vector3(randomXRange, 2.5f, 195);
                newPos.localRotation = Quaternion.Euler(new Vector3(0, -180, 0));
            }
            else
            {
                newPos.localPosition = new Vector3(randomXRange, 2.5f, -195);
                newPos.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }
        } 
        else if (result == randomZRange)
        {
            float probability = Random.Range(0.0f, 1.0f);

            if (probability > 0.5)
            {
                newPos.localPosition = new Vector3(195, 2.5f, randomZRange);
                newPos.localRotation = Quaternion.Euler(new Vector3(0, -90, 0));
            }
            else
            {
                newPos.localPosition = new Vector3(-195, 2.5f, randomZRange); 
                newPos.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
            }
        }

        return newPos;
    }

    // Find suitable location for target to spawn
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
            case observationMethod.eight:
                // Eighth attempt
                sensor.AddObservation(Vector3.Distance(transform.localPosition, target.transform.localPosition));

                sensor.AddObservation(transform.forward);
                sensor.AddObservation((target.transform.localPosition - transform.localPosition).normalized); // direction to target

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
        if (other.CompareTag("Goal"))
        {
            // If the agent is in the comaprison scene then update the scene with variables
            if (lastEpisodeCompletedText != null)
            {
                lastEpisodeCompletedText.text = episodeTimer.ToString("0.00");
                totalTime += episodeTimer;
                episodesCompleted++;
                SetText();
            }

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

    // Show values on scene for comparison scene
    private void SetText()
    {
        meanTimeText.text = (totalTime / episodesCompleted).ToString("0.00");
        episodesCompletedText.text = episodesCompleted.ToString();

        WriteToCSVFile.WriteToCSV.addRecord(episodesCompleted, episodeTimer, totalTime, (totalTime / episodesCompleted), "rl_results.csv");
    }

    private IEnumerator ChangeFloorMaterial(Material material)
    {
        floorMeshRenderer.material = material;

        // Episode timer was resetting to zero before being added to the scene and so small delay is added here to prevent this
        yield return new WaitForSeconds(0.1f); 
        episodeTimer = 0.1f;

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

    private void FixedUpdate()
    {
        episodeTimer += Time.fixedDeltaTime;
    }

    private void TimedRoadblocks()
    {
        grid.ClearRoadblocks();
        grid.BuildRoadblocks();
        timerSeconds = seconds;
        activeTimer = true;
    }

    public void ToggleTimer(bool active)
    {
        activeTimer = active;
    }

    public void SetTimerSeconds(float newSeconds)
    {
        seconds = newSeconds;
    }

    public void SetTimerText(Text text)
    {
        text.text = seconds.ToString();
    }

    public void ForecEndEpisode()
    {
        EndEpisode();
    }
}
