using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIAgentController : MonoBehaviour
{
    [Header("Agent")]
    [SerializeField] private Agent m_agent = null;
    [SerializeField] private int m_team = 0;
    [SerializeField] private GameObject m_ball = null;
    [SerializeField] private bool ballCollected = false;
    private int m_state = -1;

    private bool m_isRunning = false;

    [Header("Navigation")]
    [SerializeField] private float m_maxAngularSpeedAngle = 45.0f;
    [SerializeField] private float m_minAngularSpeedAngle = 10.0f;
    [SerializeField] private float m_maxSpeedDistance = 5.0f;
    [SerializeField] private float m_destinationBuffer = 2.0f;

    private List<Vector3> m_pathList = new List<Vector3>();

    [Header("Dodgeball Court")]
    [SerializeField] private float m_blueCourtCentreZ = -12.5f;
    [SerializeField] private float m_redCourtCentreZ = 12.5f;
    [SerializeField] private Vector3 m_courtDimensions = new Vector3(25f, 1f, 12.5f);

    private Vector3 m_courtCentre = Vector3.zero;

    [Header("Target and Attacking")]
    [SerializeField] private Transform m_target = null;
    [SerializeField] private float m_targetDistance = 0f;
    [SerializeField] private float m_maxThrowDistance = 20f;    // SHOULD BE A CONSTANT

    private void Start()
    {
        m_team = (int)m_agent.GetTeam();
        FindCourtCentre();
        
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    m_isRunning = !m_isRunning;
        //}

        //if (m_isRunning)
        //{
        //    ScanForObjects();
        //    MoveTowardsDestination();
        //}
        //else
        //{
        //    Debug.Log("Stop");
        //    m_agent.StopAngularVelocity();
        //    m_agent.StopLinearVelocity();
        //}
        m_state = (int)m_agent.GetState();

        switch (m_state)
        {
            case 0:
                ScanForObjects();
                MoveTowardsDestination();
                TryPickup();
                CheckWanderState();
                break;
            case 1:
                ScanForTargets();
                TryThrow();
                break;
            case 2:
                break;
            default:
                break;
        }
    }

    private void FindCourtCentre()
    {
        switch (m_team)
        {
            case 0:
                m_courtCentre = new Vector3(0f, 0f, m_redCourtCentreZ);
                break;
            case 1:
                m_courtCentre = new Vector3(0f, 0f, m_blueCourtCentreZ);
                break;
            default:
                Debug.LogWarningFormat("Team did not initialize correctly. Team {0}", m_team);
                break;
        }
    }

    private bool HasDestination()
    {
        if (m_pathList.Count > 0)
        {
            Vector3 destination = m_pathList[0];
            Vector3 toDestination = destination - m_agent.transform.position;
            float distanceToDestination = toDestination.magnitude;
            //Debug.LogFormat("Distance to Destination = {0}", distanceToDestination);
            //Debug.LogFormat("Destination Buffer = {0}", m_destinationBuffer);
            if (distanceToDestination <= m_destinationBuffer)
            {
                Debug.LogFormat("Reached destination: {0}", m_pathList[0]);
                m_pathList.RemoveAt(0);
            }
        }

        return m_pathList.Count > 0;
    }

    private float CalculateConsiderationValue(float val, float min, float max)
    {
        float range = max - min;
        float value = Mathf.Clamp(val, min, max);
        float considerationValue = (value - min) / range;
        return considerationValue;
    }

    private float CalculateConsiderationUtil(List<float> considerationList)
    {
        float numConsiderations = (float)considerationList.Count;
        float finalScore = numConsiderations > 0.0f ? 1.0f : 0.0f;

        for (int i = 0; i < considerationList.Count; ++i)
        {
            float modificationFactor = 1.0f - (1.0f / numConsiderations);
            float makeupValue = (1.0f - considerationList[i]) * modificationFactor;
            finalScore *= considerationList[i] + (makeupValue * considerationList[i]);
        }

        //foreach (float considerationScore in considerationList)
        //{
        //    float modificationFactor = 1.0f - (1.0f / numConsiderations);
        //    float makeupValue = (1.0f - considerationScore) * modificationFactor;
        //    finalScore *= considerationScore + (makeupValue * considerationScore);
        //}

        return finalScore;
    }

    private void MoveTowardsDestination()
    {
        if (!HasDestination())
        {
            m_agent.StopAngularVelocity();
            return;
        }

        //Debug.Log("Current dest: " + m_pathList[0]);
        Vector3 destination = m_pathList[0];
        Vector3 toDestination = destination - m_agent.transform.position;
        float distanceToDestination = toDestination.magnitude;
        toDestination.Normalize();
        float lookAtToDestinationDot = Vector3.Dot(m_agent.transform.forward, toDestination);
        float rightToDestinationDot = Vector3.Dot(m_agent.transform.right, toDestination);
        float toDestinationAngle = Mathf.Rad2Deg * Mathf.Acos(lookAtToDestinationDot);

        List<float> speedConsiderations = new List<float>();

        float distanceConsideration = CalculateConsiderationValue(distanceToDestination, m_destinationBuffer, m_maxSpeedDistance);
        float angleConsideration = CalculateConsiderationValue(toDestinationAngle, m_minAngularSpeedAngle, m_maxAngularSpeedAngle);
        float speedAngleConsideration = 1.0f - angleConsideration;
        speedConsiderations.Add(distanceConsideration);
        speedConsiderations.Add(speedAngleConsideration);

        float speed = CalculateConsiderationUtil(speedConsiderations) * m_agent.linearMaxSpeed;
        m_agent.linearSpeed = Mathf.Max(2f, speed);

        float angularSpeed = angleConsideration * m_agent.angularMaxSpeed;
        m_agent.angularSpeed = angularSpeed;

        //how do we face our destination
        bool shouldTurnRight = rightToDestinationDot > Mathf.Epsilon;

        if (distanceToDestination > m_destinationBuffer)
        {
            if (shouldTurnRight)
            {
                m_agent.TurnRight();
            }
            else
            {
                m_agent.TurnLeft();
            }

            Debug.Log("forwards");
            m_agent.MoveForwards();
        }
    }

    private void OnDestinationFound(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        bool isSuccess = NavMesh.CalculatePath(m_agent.transform.position, destination, NavMesh.AllAreas, path);
        if (isSuccess)
        {
            //draw out the path
            //set the destination
            foreach (Vector3 pathNode in path.corners)
            {
                m_pathList.Add(pathNode);
                //Debug.Log("Path Pos: " + pathNode);
            }

            //Debug.LogFormat("Path length: {0}", path.corners.Length);

            //foreach (Vector3 dest in m_pathList)
            //{
            //    Debug.Log("List Pos: " + dest);
            //}
        }
    }

    private void ScanForObjects()
    {
        //don't scan when we have somewhere to go
        if (HasDestination())
        {
            return;
        }

        Vector3 agentPosition = m_agent.transform.position;
        int layer = LayerMask.NameToLayer("Interactable");
        int layerMask = 1 << layer;

        Collider[] hitInfo = Physics.OverlapBox(m_courtCentre, m_courtDimensions, Quaternion.identity, layerMask);
        //Collider[] hitColliders = Physics.OverlapSphere(agentPosition, m_scanDistance, layerMask);

        for (int i = 0; i < hitInfo.Length; ++i)
        {
            float distanceToObject = Vector3.Distance(agentPosition, hitInfo[i].transform.position);

            if (distanceToObject < m_destinationBuffer)
            {
                continue;
            }

            m_ball = hitInfo[i].gameObject;

            OnDestinationFound(hitInfo[i].transform.position);

            i = hitInfo.Length;
        }

        //foreach (Collider hitCollider in hitInfo)
        //{
        //    float distanceToObject = Vector3.Distance(agentPosition, hitCollider.transform.position);
        //    if (distanceToObject < m_destinationBuffer)
        //    {
        //        continue;
        //    }
        //    OnDestinationFound(hitCollider.transform.position);
        //    break;
        //}
    }

    private void TryPickup()
    {
        if (HasDestination())
        {
            return;
        }

        if (m_ball)
        {
            m_ball.transform.SetParent(m_agent.transform);
            ballCollected = true;
        }
    }

    private void CheckWanderState()
    {
        if (ballCollected)
        {
            m_agent.ChangeState(1);
        }
        else if (!HasDestination())
        {
            m_agent.ChangeState(2);
        }
    }

    private bool HasTarget()
    {
        return m_target;
    }

    private void ScanForTargets()
    {
        if (HasDestination() || HasTarget())
        {
            return;
        }

        Vector3 agentPos = m_agent.transform.position;
        int layerMask = 1 << LayerMask.NameToLayer("Target");
        // m_courtCentre * -1f ==> the opposite side of the court
        Collider[] hitInfo = Physics.OverlapBox(m_courtCentre * -1f, m_courtDimensions, Quaternion.identity, layerMask);
        float nearDistance = Mathf.Infinity;

        for (int i = 0; i < hitInfo.Length; ++i)
        {
            float distanceToTarget = Vector3.Distance(agentPos, hitInfo[i].transform.position);

            if (distanceToTarget < nearDistance)
            {
                nearDistance = distanceToTarget;
                m_target = hitInfo[i].transform;
            }
        }

        if (nearDistance < Mathf.Infinity)
        {
            m_targetDistance = nearDistance;
        }
    }

    private bool TargetInRange()
    {
        return (m_targetDistance < m_maxThrowDistance);
    }

    private Vector3 FindNearestThrowPosition()
    {
        Vector3 throwPos = Vector3.zero;

        // Find distance required to move to be within range
        // Find position in world space

        return throwPos;
    }

    private void TryThrow()
    {
        if (!ballCollected)
        {
            return;
        }

        if (TargetInRange())
        {
            m_agent.transform.LookAt(m_target);
            m_ball.GetComponent<BallProjectile>().Throw();
            ballCollected = false;
        }
    }
}
