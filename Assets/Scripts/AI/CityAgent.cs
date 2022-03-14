using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CityAgent : Agent
{
    [SerializeField] private float speed = 10.0f;
    [SerializeField] private GameObject target;
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;
    [SerializeField] private GridWithParams grid;
    [SerializeField] private bool isRandomGrid;
    [SerializeField, Tooltip("Agent is random on episode end")] private bool randomStartLocation;

    private Rigidbody playerRigidbody;

    private Vector3 originalPosition;

    private float distanceRequired = 1.5f;

    private bool settingLocation = true;

    private Material originalMaterial;

    public override void Initialize()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        originalPosition = transform.localPosition;

        originalMaterial = floorMeshRenderer.material;
    }

    public override void OnEpisodeBegin()
    {
        transform.LookAt(target.transform.localPosition);
        transform.localPosition = originalPosition;
        target.transform.localPosition = new Vector3(Random.Range(-90, 0), 0, Random.Range(-170, 0));
        playerRigidbody.velocity = Vector3.zero;

        if (randomStartLocation)
        {
            SetRandomPosition();
        }

        if (isRandomGrid)
        {
            grid.parameters.randomSeed = Random.Range(1, 100);
            grid.BuildGrid();
        }
    }

    private void SetRandomPosition()
    {
        settingLocation = true;
        transform.localPosition = new Vector3(Random.Range(-170, -100), -2.75f, Random.Range(-170, 10));
        settingLocation = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target.transform.localPosition);

        sensor.AddObservation(playerRigidbody.velocity.x);
        sensor.AddObservation(playerRigidbody.velocity.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector3 vectorForce = new Vector3();
        vectorForce.x = actions.ContinuousActions[0];
        vectorForce.z = actions.ContinuousActions[1];

        //playerRigidbody.AddForce(vectorForce * speed);
        transform.position += new Vector3(actions.ContinuousActions[0], 0, actions.ContinuousActions[1]) * Time.deltaTime * speed;

        float distanceFromTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);

        if (distanceFromTarget <= distanceRequired)
        {
            SetReward(1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            SetReward(+1f);
            StartCoroutine(ChangeFloorMaterial(winMaterial));
            EndEpisode();
        }
        if (other.CompareTag("Wall"))
        {
            //if (!settingLocation)
            //{
            //    SetReward(-1f);
            //    StartCoroutine(ChangeFloorMaterial(loseMaterial));
            //    EndEpisode();
            //}
            //else
            //{
            //    SetRandomPosition();
            //}
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
