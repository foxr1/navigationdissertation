using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateEnvironments : MonoBehaviour
{
    [SerializeField]
    private GameObject mainCamera;
    [SerializeField]
    private GameObject environmentPrefab;
    [SerializeField, Range(1, 100), Tooltip("Enter a square number")]
    private int noOfEnvironments;

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

    // Update is called once per frame
    void Update()
    {

    }

    public void SetupEnvironments()
    {
        GameObject plane = environmentPrefab.transform.GetChild(1).gameObject;
        Bounds bounds = plane.GetComponent<MeshRenderer>().bounds;

        float rows = Mathf.Sqrt(noOfEnvironments);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                if (!(i == 0 && j == 0))
                {
                    GameObject environment = GameObject.Instantiate(environmentPrefab);
                    environment.transform.position = new Vector3(i * (bounds.size.x * 1.5f), 0, j * (bounds.size.z * 1.5f));
                }
            }
        }
    }

    private void MoveCamera()
    {
        float position = ((bounds.size.x * Mathf.Sqrt(noOfEnvironments)) + ((bounds.size.x / 2) * (Mathf.Sqrt(noOfEnvironments) - 1))) / 2 - (bounds.size.x / 2);

        mainCamera.transform.position = new Vector3(position, position * 2.5f, position);
    }
}
