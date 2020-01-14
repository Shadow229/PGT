using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerCollect : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            //update the marker manager
            GameObject.Find("MarkerManager").GetComponent<MarkerManager>().ActiveFlag = false;

            //clear the marker
            Destroy(this.gameObject);
        }
    }
}
