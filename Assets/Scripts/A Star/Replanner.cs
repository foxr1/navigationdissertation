using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Replanner : MonoBehaviour
{
    private float timerSeconds = 0; // Internal timer seconds
    private bool activeTimer = false;
    [SerializeField] private bool isTimedRoadblocks = false;
    [SerializeField, Range(0, 60)] private float seconds = 30; // Value to reset back to when timer is finished

    [SerializeField]
    private GameObject target;

    [SerializeField]
    private Vector3 startPosition;

    // Obstacle Avoidance
    public float targetVelocity = 10.0f;
    public int numberOfRays = 7;
    public float angle = 90;
    public float rayRange = 2;

    [SerializeField]
    private AStarGrid aStarGrid;
    [SerializeField]
    private Pathfinding pathfinding;
    [SerializeField]
    private GridWithParams grid;

    private bool resetting = false;

    [SerializeField]
    private Text lastTimeCompletedText;
    [SerializeField]
    private Text meanTimeText;
    [SerializeField]
    private Text episodesCompletedText;

    // Start is called before the first frame update
    void Start()
    {
        if (isTimedRoadblocks)
        {
            timerSeconds = seconds;
            activeTimer = true;
        }
    }

    // Update is called once per frame
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

        Vector3 deltaPosition = Vector3.zero;
        for (int i = 0; i < numberOfRays; i++)
        {
            Quaternion rotation = transform.localRotation;
            Quaternion rotationMod = Quaternion.AngleAxis((i / ((float)numberOfRays - 1)) * angle * 2 - angle, transform.up);
            Vector3 direction = rotation * rotationMod * Vector3.forward;

            Ray ray = new Ray(transform.localPosition, direction);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayRange) && !resetting)
            {
                // If the agent detects anything other than the goal (wall/roadblock) then recalculate the best path
                if (!hitInfo.transform.gameObject.CompareTag("Goal"))
                {
                    RecalculatePath();
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Goal")) // If agent reaches goal, end episode and reset
        {
            SetText();
            ResetEnvironment();
        } 
        else if (other.CompareTag("Wall")) // If the agent collides with a roadblock then reset from start
        {
            ResetEnvironment(); 
        }
    }

    // Function for displaying text on comparison scene
    private void SetText()
    {
        lastTimeCompletedText.text = pathfinding.Timer.ToString("0.00");

        pathfinding.totalTime += pathfinding.Timer;
        pathfinding.episodesCompleted++;
        meanTimeText.text = (pathfinding.totalTime / pathfinding.episodesCompleted).ToString("0.00");

        episodesCompletedText.text = pathfinding.episodesCompleted.ToString();

        WriteToCSVFile.WriteToCSV.addRecord(pathfinding.episodesCompleted, pathfinding.Timer, pathfinding.totalTime, (pathfinding.totalTime / pathfinding.episodesCompleted), "a_star_results.csv");
    }

    private void TimedRoadblocks()
    {
        grid.ClearRoadblocks();
        grid.BuildRoadblocks();
        timerSeconds = seconds;
        activeTimer = true;
    }

    public void ResetEnvironment()
    {
        resetting = true;
        pathfinding.Timer = 0;
        pathfinding.StopAllCoroutines();
        pathfinding.FollowPath(false);
        pathfinding.CurrentNode = 0;
        transform.localPosition = startPosition;
        aStarGrid.RandomiseTargetPos();
        RecalculatePath();
        resetting = false;
    }

    private void RecalculatePath()
    {
        pathfinding.shouldFollow = false;
        aStarGrid.CreateGrid();
        pathfinding.CurrentNode = 0;
        pathfinding.FindPath(transform.localPosition, target.transform.localPosition);
        pathfinding.shouldFollow = true;
    }

    // Displays the raycasts in the editor
    private void OnDrawGizmos()
    {
        for (int i = 0; i < numberOfRays; i++)
        {
            Quaternion rotation = transform.localRotation;
            Quaternion rotationMod = Quaternion.AngleAxis((i / ((float)numberOfRays - 1)) * angle * 2 - angle, transform.up);
            Vector3 direction = rotation * rotationMod * Vector3.forward;

            Gizmos.DrawRay(transform.localPosition, direction * rayRange);
        }
    }
}
