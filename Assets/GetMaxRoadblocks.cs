using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GetMaxRoadblocks : MonoBehaviour
{
    [SerializeField]
    private GridWithParams grid;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Slider>().maxValue = (grid.parameters.width * (grid.parameters.width - 1)); 
    }
}
