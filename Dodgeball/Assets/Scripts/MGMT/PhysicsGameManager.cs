using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsGameManager : MonoBehaviour
{
    [SerializeField] GameObject[] m_roundOneTargets = new GameObject[3];
    [SerializeField] GameObject[] m_roundTwoTargets = new GameObject[3];
    [SerializeField] GameObject[] m_roundThreeTargets = new GameObject[3];

    private int m_round = 0;

    private const float MIN_X_BOUNDARY = -20f;
    private const float MAX_X_BOUNDARY = 20f;
    private const float MIN_Y_BOUNDARY = 8f;
    private const float MAX_Y_BOUNDARY = 24f;
    private const float MIN_Z_BOUNDARY = 5f;
    private const float MAX_Z_BOUNDARY = 20f;

    private void Start ()
    {

	}

    private void Update ()
    {
		
	}

    private void InitializeTargetData()
    {

    }

    private void StartRound()
    {

    }

    private void SpawnTarget(Vector3 position, Vector3 velocity)
    {
        //Target temp = Instantiate()
    }
}
