using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class randomtest : MonoBehaviour {

    float rand;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        Random.InitState(System.DateTime.Now.Millisecond);

        rand = Random.Range(0f, 1f);

        if (rand < 0.5f)
        {
            Debug.Log("Low");
        }
        else
        {
            Debug.Log("High");
        }

    }
}
