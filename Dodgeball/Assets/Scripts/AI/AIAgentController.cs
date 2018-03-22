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
    [SerializeField] private LayerMask m_unstuckLayer;

    private List<Vector3> m_pathList = new List<Vector3>();
    private float m_zPosSign = 0f;

    private const float RANDOM_POSITION_MIN_DISTANCE = 5f;
    private const float RANDOM_POSITION_MAX_DISTANCE = 15f;

    [Header("Dodgeball Court")]
    [SerializeField] private float m_blueCourtCentreZ = -12.5f;
    [SerializeField] private float m_redCourtCentreZ = 12.5f;
    [SerializeField] private Vector3 m_courtDimensions = new Vector3(25f, 0.5f, 12.1f);
    [SerializeField] private Transform m_bluePost = null;
    [SerializeField] private Transform m_redPost = null;

    private Vector3 m_courtCentre = Vector3.zero;

    [Header("Target and Attacking")]
    [SerializeField] private Transform m_target = null;
    [SerializeField] private float m_targetDistance = 0f;
    [SerializeField] private float m_maxThrowDistance = 0f;

    private void Start()
    {
        m_zPosSign = Mathf.Sign(m_agent.transform.position.z);
        m_team = (int)m_agent.GetTeam();
        FindCourtCentre();
        m_maxThrowDistance = 63f;   // (maxSpeed ^ 2 / gravity)
    }

    private void FixedUpdate()
    {
        m_state = (int)m_agent.GetState();  // haha i never use this -> generic change state function using this?

        switch (m_state)
        {
            case 0: //wander
                ScanForObjects();
                UpdateDestination();
                MoveTowardsDestination();
                TryPickup();
                CheckWanderState();
                break;
            case 1: //attack
                CheckIfStuck();
                ScanForTargets();
                FindAttackDestination();
                TryThrow();
                CheckAttackState();
                break;
            case 2: //defend
                GenerateRandomPosition();
                MoveTowardsDestination();
                CheckDefendState();
                break;
            default:
                Debug.Log("How did you get here?");
                break;
        }
    }

    // Startup -> Determine the centre of the court for the agent attached to this controller
    private void FindCourtCentre()
    {
        switch (m_team)
        {
            case 0:
                m_courtCentre = new Vector3(0f, 0f, m_redCourtCentreZ);
                m_agent.name += " (RED)";
                break;
            case 1:
                m_courtCentre = new Vector3(0f, 0f, m_blueCourtCentreZ);
                m_agent.name += " (BLUE)";
                break;
            default:
                Debug.LogWarningFormat("Team did not initialize correctly. Team {0}", m_team);
                break;
        }
    }

    // Any State -> Determines the legitimacy of our current path. Returns true if path exists, false otherwise.
    private bool HasDestination()
    {
        if (m_pathList.Count > 0)
        {
            Vector3 destination = m_pathList[0];

            if (Mathf.Sign(destination.z) != m_zPosSign)
            {
                m_pathList.Clear();
                return m_pathList.Count > 0;
            }

            Vector3 toDestination = destination - m_agent.transform.position;
            float distanceToDestination = toDestination.magnitude;

            if (distanceToDestination <= m_destinationBuffer)
            {
                m_pathList.RemoveAt(0);
            }
        }

        return m_pathList.Count > 0;
    }

    // Any State -> Algorithm which computes the weight of various factors for our modifier algorithm.
    private float CalculateConsiderationValue(float val, float min, float max)
    {
        float range = max - min;
        float value = Mathf.Clamp(val, min, max);
        float considerationValue = (value - min) / range;
        return considerationValue;
    }

    // Any State -> Algorithm which computes a modifier for our linear or angular speed.
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

        // Old Method - Nothing changed, typically better to use FOR loop instead of FOREACH
        //foreach (float considerationScore in considerationList)
        //{
        //    float modificationFactor = 1.0f - (1.0f / numConsiderations);
        //    float makeupValue = (1.0f - considerationScore) * modificationFactor;
        //    finalScore *= considerationScore + (makeupValue * considerationScore);
        //}

        return finalScore;
    }

    // Any State -> Move towards our current destination based on our generated path.
    private void MoveTowardsDestination()
    {
        if (!HasDestination())
        {
            m_agent.StopAngularVelocity();
            return;
        }

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
        m_agent.angularSpeed = Mathf.Max(0.2f, angularSpeed);

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

            if (Mathf.Abs(m_agent.transform.position.z) > 0.5f
                || Vector3.Dot(m_agent.transform.forward, -m_zPosSign * Vector3.forward) <= 0f)
            {
                m_agent.MoveForwards();
            }
        }
        //else
        //{
        //    Debug.Log("Cannot MoveTo (Too close)");
        //}
    }

    // Any State -> Generate a path on the NavMesh to the parameter destination.
    private void OnDestinationFound(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        
        bool isSuccess = NavMesh.CalculatePath(m_agent.transform.position, destination, NavMesh.AllAreas, path);

        if (isSuccess)
        {
            m_pathList.Clear();

            foreach (Vector3 pathNode in path.corners)
            {
                m_pathList.Add(pathNode);
            }
        }
    }

    // Any state -> Check if agent is facing parameter target with a 120 degree horizontal view.
    private bool CheckFacingTarget(Transform target)
    {
        Vector3 agentWorldZ = m_agent.transform.forward;
        Vector3 targetDir = (target.position - m_agent.transform.position).normalized;

        float dot = Vector3.Dot(agentWorldZ, targetDir);
        float radAngle = Mathf.Acos(dot);

        if (radAngle < (Mathf.PI / 3)) // 120 degree horizontal view
        {
            return true;
        }

        return false;
    }

    // Wander State -> Scan this agent's side of the court for a ball to pick up.
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

        for (int i = 0; i < hitInfo.Length; ++i)
        {
            float distanceToObject = Vector3.Distance(agentPosition, hitInfo[i].transform.position);

            // If the ball is close to the agent, remember it's there (so TryPickup will occur) but do not create a path
            if (distanceToObject < m_destinationBuffer)
            {
                m_ball = hitInfo[i].gameObject;
                continue;
            }

            // If the ball is in the air or currently being held by another agent (isKinematic) do not create a path
            if (hitInfo[i].GetComponent<BallProjectile>().inAir || hitInfo[i].GetComponent<Rigidbody>().isKinematic)
            {
                continue;
            }

            m_ball = hitInfo[i].gameObject;
            OnDestinationFound(hitInfo[i].transform.position);
            i = hitInfo.Length;
        }

        // Old Method - Nothing changed, using FOR loop is typically better than FOREACH
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

    // Wander State -> Updates the path to the ball if it moves more than 1 unit away from first detected location.
    private void UpdateDestination()
    {
        if (!HasDestination() || !m_ball)
        {
            return;
        }

        if (m_ball.GetComponent<Rigidbody>().isKinematic)
        {
            m_pathList.Clear();
            return;
        }

        Vector3 destination = m_pathList[m_pathList.Count - 1];
        Vector3 ballPos = m_ball.transform.position;

        if (Vector3.Distance(destination, ballPos) > 1f)
        {
            OnDestinationFound(ballPos);
        }
    }

    // Wander State -> If the agent is close enough to the ball, pick it up.
    private void TryPickup()
    {
        if (HasDestination())
        {
            return;
        }

        if (m_ball && Vector3.Distance(m_agent.transform.position, m_ball.transform.position) < m_destinationBuffer)
        {
            m_ball.transform.SetParent(m_agent.transform);
            m_ball.transform.localPosition = Vector3.forward * 1.5f;
            ballCollected = true;
            m_ball.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    // Wander State Transition -> If the agent collects the ball, change to Attack State; if the agent cannot find a ball, change to Defend State.
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

    // Attack State Transition -> If the agent no longer has a ball, change to Wander State.
    private void CheckAttackState()
    {
        if (!ballCollected)
        {
            m_agent.ChangeState(0);
        }
    }

    // Defend State Transition -> If there is a ball to pickup, change to Wander State.
    private void CheckDefendState()
    {
        ScanForObjects();
        if (HasDestination())
        {
            m_agent.ChangeState(0);
        }
    }

    // Attack State -> Returns true if agent has a target, false otherwise.
    private bool HasTarget()
    {
        return m_target;
    }

    // Attack State -> Agent scans the opposite side of the court for other agents (Layer: Target).
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

        // Currently not used, but saves the distance to target
        if (nearDistance < Mathf.Infinity)
        {
            m_targetDistance = nearDistance;
        }
    }

    // Attack State -> If agent is facing target and passes probability check then they throw, otherwise they move closer to target.
    private void TryThrow()
    {
        if (!ballCollected || !HasTarget())
        {
            return;
        }

        if (CheckFacingTarget(m_target) && CheckThrowProbablity())
        {
            m_ball.GetComponent<BallProjectile>().Throw(m_target.gameObject);
            ballCollected = false;
            m_ball = null;
            m_pathList.Clear();
            m_agent.StopLinearVelocity();
            m_agent.StopAngularVelocity();
            //Debug.Log("Path count = " + m_pathList.Count);
        }
        else
        {
            //Debug.Log("TryThrow failed, moving to destination");
            MoveTowardsDestination();
        }
    }

    // Attack State -> If the ball lands against the edge of court, chance the ball clips through the wall and causes the agent to become stuck.
    // This detects the clipping as it occurs (even if it won't produce the bug) and teleports them away from the wall.
    private void CheckIfStuck()
    {
        RaycastHit hitInfo;

        if (Physics.Raycast(m_agent.transform.position, m_agent.transform.forward, out hitInfo, 1.5f, m_unstuckLayer, QueryTriggerInteraction.Ignore))
        {
            m_agent.transform.Translate(m_agent.transform.forward * -2f, Space.World);
        }
    }

    // Attack State -> Agent will have increasing probablity of throwing as they move closer to the centre line (0% in the back half of court).
    private bool CheckThrowProbablity()
    {
        float probablity = 0f;
        float zPos = Mathf.Abs(m_agent.transform.position.z);

        if (zPos <= 12.5f)
        {
            probablity = 0.1f + 0.9f * ((12.5f - zPos) / 11.5f);

            float random = Random.Range(Mathf.Epsilon, 1f);

            if (random < probablity)
            {
                return true;
            }
        }

        return false;
    }

    // Attack State -> If too close to the centre line with the ball, find a position farther back to attack.
    private void FindAttackDestination()
    {
        if (!ballCollected || !HasTarget())
        {
            return;
        }

        if (Mathf.Abs(m_agent.transform.position.z) < 2f)
        {
            Vector3 nearby = m_agent.transform.position - (Vector3.forward * 4f) * m_zPosSign;
            OnDestinationFound(nearby);
        }
        else
        {
            OnDestinationFound(m_target.transform.position);
        }
    }

    // Defense State -> Generate a random position to move to.
    private void GenerateRandomPosition()
    {
        if (HasDestination())
        {
            return;
        }

        Vector3 nextPos;
        float distanceToNext;

        do
        {
            float xRand = Random.Range(-22f, 22f);
            float zRand = Random.Range(5f, 22f) * m_zPosSign;
            nextPos = new Vector3(xRand, 1f, zRand);
            distanceToNext = Vector3.Distance(m_agent.transform.position, nextPos);

        } while (distanceToNext < RANDOM_POSITION_MIN_DISTANCE || distanceToNext > RANDOM_POSITION_MAX_DISTANCE);

        //Debug.Log("Generated Position = " + nextPos);

        OnDestinationFound(nextPos);
    }
}
