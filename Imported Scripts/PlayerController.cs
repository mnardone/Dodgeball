using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public Agent m_agent = null;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(!m_agent)
        {
            return;
        }

        bool isLinearIdle = true;
        bool isAngularIdle = true;

        if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            isLinearIdle = false;
            m_agent.StrafeRight();
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            isLinearIdle = false;
            m_agent.StrafeLeft();
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            isLinearIdle = false;
            m_agent.MoveForwards();
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            isLinearIdle = false;
            m_agent.MoveBackwards();
        }

        if (Input.GetKey(KeyCode.Q))
        {
            isAngularIdle = false;
            m_agent.TurnLeft();
        }

        if (Input.GetKey(KeyCode.E))
        {
            isAngularIdle = false;
            m_agent.TurnRight();
        }

        if(isLinearIdle)
        {
            m_agent.StopLinearVelocity();
        }

        if(isAngularIdle)
        {
            m_agent.StopAngularVelocity();
        }
    }

}
