using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private GameObject[] m_targets = new GameObject[3];
    private int m_currentTarget = 0;

    [SerializeField] private BallProjectile m_ball = null;

    private void Start ()
    {
		
	}

    private void Update ()
    {
        HandleInput();
	}

    private void HandleInput()
    {
        // Can cycle through targets
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (m_currentTarget == 0)
            {
                m_currentTarget = 3;
            }
            else
            {
                m_currentTarget -= 1;
            }
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            m_currentTarget += 1;
            m_currentTarget %= 3;
        }

        // Start throw process
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // show power gauge
            // if power guage active
                // fire
        }
    }

    private GameObject GetTarget()
    {
        return m_targets[m_currentTarget];
    }

    public void RecognizeTargets(GameObject[] targets)
    {
        for (int i = 0; i < targets.Length; ++i)
        {
            m_targets[i] = targets[i];
        }
    }
}
