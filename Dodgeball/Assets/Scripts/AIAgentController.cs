using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIAgentController : MonoBehaviour {

    //scan for specified tags within a distance
    //if something is there, move towards it

    [SerializeField] private Agent m_agent = null;

    [SerializeField] private float m_maxAngularSpeedAngle = 45.0f;
    [SerializeField] private float m_minAngularSpeedAngle = 10.0f;

    [SerializeField] private float m_maxSpeedDistance = 5.0f;
    [SerializeField] private float m_destinationBuffer = 2.0f;
    [SerializeField] private float m_scanDistance = 10.0f;

    private List<Vector3> m_pathList = new List<Vector3>();

    private bool m_isRunning = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_isRunning = !m_isRunning;
        }

        if (m_isRunning)
        {
            ScanForObjects();
            MoveTowardsDestination();
        }
        else
        {
            m_agent.StopAngularVelocity();
            m_agent.StopLinearVelocity();
        }
    }

    private bool HasDestination()
    {
        if (m_pathList.Count > 0)
        {
            Vector3 destination = m_pathList[0];
            Vector3 toDestination = destination - m_agent.transform.position;
            float distanceToDestination = toDestination.magnitude;
            if (distanceToDestination - 1f <= m_destinationBuffer)
            {
                //m_pathList.Remove(destination);
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
        foreach (float considerationScore in considerationList)
        {
            float modificationFactor = 1.0f - (1.0f / numConsiderations);
            float makeupValue = (1.0f - considerationScore) * modificationFactor;
            finalScore *= considerationScore + (makeupValue * considerationScore);
        }
        return finalScore;
    }

    private void MoveTowardsDestination()
    {
        if (!HasDestination())
        {
            m_agent.StopAngularVelocity();
            return;
        }

        Debug.Log("Current dest: " + m_pathList[0]);
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
        m_agent.linearSpeed = speed;

        float angularSpeed = angleConsideration * m_agent.angularMaxSpeed;
        m_agent.angularSpeed = angularSpeed;

        //how do we face our destination
        bool shouldTurnRight = rightToDestinationDot > Mathf.Epsilon;

        if (shouldTurnRight)
        {
            m_agent.TurnRight();
        }
        else
        {
            m_agent.TurnLeft();
        }

        if (distanceToDestination >= m_destinationBuffer)
        {
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
                Debug.Log("Path Pos: " + pathNode);
            }

            foreach (Vector3 dest in m_pathList)
            {
                Debug.Log("List Pos: " + dest);
            }
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
        Collider[] hitColliders = Physics.OverlapSphere(agentPosition, m_scanDistance, layerMask);
        foreach (Collider hitCollider in hitColliders)
        {
            float distanceToObject = Vector3.Distance(agentPosition, hitCollider.transform.position);
            if (distanceToObject < m_destinationBuffer)
            {
                continue;
            }

            OnDestinationFound(hitCollider.transform.position);

            break;
        }
    }
}
