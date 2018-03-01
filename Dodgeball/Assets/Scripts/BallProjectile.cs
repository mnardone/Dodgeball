using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallProjectile : MonoBehaviour {

    public bool m_isRunning = false;
    private Rigidbody m_rb = null;

    public Vector3 m_initialVelocity = Vector3.zero;
    private float m_timeElapsed = 0.0f;

	// Use this for initialization
	void Start () {
        m_rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Space))
        {
            m_isRunning = !m_isRunning;
            m_rb.velocity = m_initialVelocity;
        }

        //m_rb.useGravity = m_isRunning;

        if(m_rb.velocity.magnitude > 0.0001f)
        {
            m_timeElapsed += Time.deltaTime;
        }
	}

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Time Elapsed: " + m_timeElapsed);
    }
}
