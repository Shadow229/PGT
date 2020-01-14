using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotFollow : MonoBehaviour
{
    public GameObject player;
    public float distance;


    // Update is called once per frame
    void Update()
    {
        FollowPlayer();
    }

    //follow above the player
    void FollowPlayer()
    {
        Vector3 Newpos = player.transform.position + new Vector3(0, distance, 0);

        transform.position = Newpos;
    }
}
