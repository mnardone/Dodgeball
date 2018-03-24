using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIAgentController : MonoBehaviour
{
    [Header("Agent")]
    [SerializeField] private Agent m_agent = null;
    [SerializeField] private int m_team = 0;
    [SerializeField] private GameObject m_ball = null;
    [SerializeField] private bool m_ballCollected = false;

    private int m_state = -1;
    private bool m_enteredDefenseState = false;

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
                break;
            case 1: //attack
                ScanForTargets();
                FindAttackDestination();
                TryThrow();
                break;
            case 2: //defend
                GenerateRandomPosition();
                MoveTowardsDestination();
                break;
            default:
                Debug.Log("How did you get here?");
                break;
        }

        CheckIfStuck();
        CheckState();
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
            m_agent.StopLinearVelocity();
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

        //if (m_agent.AgentProximity)
        //{
        //    shouldTurnRight = true;
        //}

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
                if (m_agent.AgentProximity)
                {
                    //if (shouldTurnRight)
                    //{
                    //    m_agent.StrafeRight();
                    //}
                    //else
                    //{
                    //    m_agent.StrafeLeft();
                    //}

                    m_agent.StrafeRight();
                }
                //else
                //{
                //    m_agent.MoveForwards();
                //}

                m_agent.MoveForwards();
            }
        }
        //else
        //{
        //    Debug.Log("Cannot MoveTo (Too close)");
        //}
    }

    // Any State -> Generate a path on the NavMesh to the parameter destination.
    private void OnDestinationFound(Vector3 destination, int areaMask = -1)
    {
        NavMeshPath path = new NavMeshPath();
        
        bool isSuccess = NavMesh.CalculatePath(m_agent.transform.position, destination, areaMask, path);

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

    // Any state -> State transitions
    private void CheckState()
    {
        if (m_ballCollected)
        {
            if (m_agent.GetState() != Agent.State.Attack)
            {
                m_pathList.Clear();
                m_agent.ChangeState(1);
            }
        }
        else if (ScanForLiveBall())
        {
            if (m_agent.GetState() != Agent.State.Wander)
            {
                m_pathList.Clear();
                m_agent.ChangeState(0);
            }
        }
        else
        {
            if (m_agent.GetState() != Agent.State.Defend)
            {
                m_pathList.Clear();
                m_agent.ChangeState(2);
                m_enteredDefenseState = true;
            }
        }
    }

    // Any state -> Wander state transition. Returns true if the Dodgeball is on this agent's side of the court, false otherwise.
    private bool ScanForLiveBall()
    {
        int layer = LayerMask.NameToLayer("Interactable");
        int layerMask = 1 << layer;
        bool liveBallExists = false;

        Collider[] hitInfo = Physics.OverlapBox(m_courtCentre, m_courtDimensions, Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitInfo.Length; ++i)
        {
            // Ignore any ball that is in the air or currently being held by another agent (isKinematic)
            if (hitInfo[i].GetComponent<BallProjectile>().inAir || hitInfo[i].GetComponent<Rigidbody>().isKinematic)
            {
                continue;
            }
            else
            {
                liveBallExists = true;

                int agentLayerMask = 1 << LayerMask.NameToLayer("Target");
                Collider[] agentHitInfo = Physics.OverlapBox(m_courtCentre, m_courtDimensions, Quaternion.identity, agentLayerMask, QueryTriggerInteraction.Ignore);
                float distanceToBall = Vector3.Distance(m_agent.transform.position, hitInfo[i].transform.position);

                for (int j = 0; j < agentHitInfo.Length; ++j)
                {
                    if (agentHitInfo[j].transform == m_agent.transform)
                    {
                        continue;
                    }

                    if (distanceToBall > Vector3.Distance(agentHitInfo[j].transform.position, hitInfo[i].transform.position))
                    {
                        //Debug.Log(this.name + " is not going to the ball anymore.");
                        liveBallExists = false;
                    }
                }
            }
        }

        return liveBallExists;
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

        Collider[] hitInfo = Physics.OverlapBox(m_courtCentre, m_courtDimensions, Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitInfo.Length; ++i)
        {
            //Debug.Log("Scanned ball. Ball.inAir = " + hitInfo[i].GetComponent<BallProjectile>().inAir);

            // If the ball is in the air or currently being held by another agent (isKinematic) do not create a path
            if (hitInfo[i].GetComponent<BallProjectile>().inAir || hitInfo[i].GetComponent<Rigidbody>().isKinematic)
            {
                continue;
            }

            float distanceToObject = Vector3.Distance(agentPosition, hitInfo[i].transform.position);
            m_ball = hitInfo[i].gameObject;
            

            // If the ball is close to the agent, do not create a path
            if (distanceToObject < m_destinationBuffer)
            {
                continue;
            }

            OnDestinationFound(hitInfo[i].transform.position/*, m_agent.GetAreaMask()*/);
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

        Vector3 destination = m_pathList[m_pathList.Count - 1];

        // If the ball has been picked up OR the ball is on the opposite side of the court
        if (m_ball.GetComponent<Rigidbody>().isKinematic || Mathf.Sign(destination.z) != m_zPosSign)
        {
            Debug.Log("Ball is picked up OR on the other side.");
            m_pathList.Clear();
            return;
        }
        
        Vector3 ballPos = m_ball.transform.position;

        if (Vector3.Distance(destination, ballPos) > 1f)
        {
            OnDestinationFound(ballPos/*, m_agent.GetAreaMask()*/);
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
            m_ball.transform.localPosition = Vector3.up * 1.8f;
            m_ballCollected = true;
            m_ball.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void PickUpBall(GameObject ball)
    {
        Debug.Log("Catching the ball");
        m_ball = ball;
        m_ball.transform.SetParent(m_agent.transform);
        m_ball.transform.localPosition = Vector3.up * 1.8f;
        m_ballCollected = true;
        m_ball.GetComponent<Rigidbody>().isKinematic = true;
    }

    // Wander State Transition -> If the agent collects the ball, change to Attack State; if the agent cannot find a ball, change to Defend State.
    private void CheckWanderState()
    {
        if (m_ballCollected)
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
        if (!m_ballCollected)
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
        Collider[] hitInfo = Physics.OverlapBox(m_courtCentre * -1f, m_courtDimensions, Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore);
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
    }

    // Attack State -> If agent is facing target and passes probability check then they throw, otherwise they move closer to target.
    private void TryThrow()
    {
        if (!m_ballCollected || !HasTarget())
        {
            return;
        }

        if (CheckFacingTarget(m_target) && CheckThrowProbablity())
        {
            m_ball.GetComponent<BallProjectile>().Throw(m_target.gameObject);
            m_ballCollected = false;
            m_ball = null;
            m_target = null;

            //m_pathList.Clear();
            //m_agent.StopLinearVelocity();
            //m_agent.StopAngularVelocity();

            //Debug.Log("Path count = " + m_pathList.Count);
        }
        else
        {
            //Debug.Log("TryThrow failed, moving to destination");
            MoveTowardsDestination();
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

            Random.InitState(System.DateTime.Now.Millisecond);
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
        if (!m_ballCollected || !HasTarget())
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
            Random.InitState(System.DateTime.Now.Millisecond);
            float xRand = Random.Range(-22f, 22f);
            Random.InitState(System.DateTime.Now.Millisecond + 10);
            float zRand = Random.Range(5f, 22f) * m_zPosSign;
            nextPos = new Vector3(xRand, 1f, zRand);
            distanceToNext = Vector3.Distance(m_agent.transform.position, nextPos);

        } while (distanceToNext < RANDOM_POSITION_MIN_DISTANCE || distanceToNext > RANDOM_POSITION_MAX_DISTANCE);


        if (m_enteredDefenseState)
        {
            m_enteredDefenseState = false;

            int layerMask = 1 << LayerMask.NameToLayer("Target");

            Collider[] hitInfo = Physics.OverlapSphere(m_agent.transform.position, 5f, layerMask, QueryTriggerInteraction.Collide);
            float nearDistance = Mathf.Infinity;
            float distanceToTeammate = 0f;
            Transform nearTeammate = null;

            for (int i = 0; i < hitInfo.Length; ++i)
            {
                if (hitInfo[i].transform == m_agent.transform)
                {
                    continue;
                }

                distanceToTeammate = Vector3.Distance(m_agent.transform.position, hitInfo[i].transform.position);

                if (distanceToTeammate < nearDistance)
                {
                    nearDistance = distanceToTeammate;
                    nearTeammate = hitInfo[i].transform;
                }
            }

            if (nearTeammate)
            {
                if (m_agent.transform.position.x < nearTeammate.position.x)
                {
                    nextPos = new Vector3(Mathf.Max(-22f, m_agent.transform.position.x - 5f), m_agent.transform.position.y, m_agent.transform.position.z);
                }
                else
                {
                    nextPos = new Vector3(Mathf.Min(22f, m_agent.transform.position.x + 5f), m_agent.transform.position.y, m_agent.transform.position.z);
                }
            }
        }

        OnDestinationFound(nextPos/*, m_agent.GetAreaMask()*/);
    }

    // ?
    private void CheckIfStuck()
    {
        RaycastHit hitInfo;

        if (Physics.Raycast(m_agent.transform.position, m_agent.transform.forward, out hitInfo, 1.5f, m_unstuckLayer, QueryTriggerInteraction.Ignore))
        {
            m_agent.transform.Translate(m_agent.transform.forward * -2f, Space.World);
        }
    }
}
