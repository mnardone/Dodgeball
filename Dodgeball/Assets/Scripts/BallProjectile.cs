using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    private Rigidbody m_rb = null;
    private float m_timeElapsed = 0.0f;

    [Header("Stationary Target, Minimum Speed")]
    [SerializeField] private Transform m_targetTransform;
    [SerializeField] private float m_maxSpeed = 25f;
    [Header("Moving Target, Desired Air Time")]
    [SerializeField] private MovingTarget m_movingTarget = null;
    [SerializeField] private float m_desiredAirTime = 1.0f;

    // Use this for initialization
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        //m_rb.velocity = CalculateWithMaxVel(m_movingTarget.transform.position);
    }

    private void FixedUpdate()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    //m_rb.velocity = CalculateInitialVelocity(m_targetTransform.position,false);
        //    //m_rb.velocity = CalculateInitialVelocityMovingTarget();
        //}

        //m_rb.useGravity = m_isRunning;

        if (m_rb.velocity.magnitude > Mathf.Epsilon)
        {
            m_timeElapsed += Time.deltaTime;
        }

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    m_rb.isKinematic = false;
        //    //m_rb.velocity = CalculateWithMaxVel(m_movingTarget.transform.position).velocity;
        //    m_rb.velocity = CalculateWithMaxVel(CalculateTargetPosition()).velocity;
        //}
    }

    public void Throw()
    {
        this.transform.SetParent(null);
        Vector3 velocity = CalculateWithMaxVel(CalculateTargetPosition()).velocity;
        m_rb.isKinematic = false;
        m_rb.velocity = velocity;
    }

    private Vector3 CalculateInitialVelocityMovingTarget()
    {
        //find out where the target will be in our desired time
        //aim for that position
        Vector3 targetVelocity = m_movingTarget.GetVelocity();
        Vector3 targetDisplacement = targetVelocity * m_desiredAirTime;
        Vector3 targetPosition = m_movingTarget.transform.position + targetDisplacement;
        return CalculateInitialVelocity(targetPosition, true);
    }

    private Vector3 CalculateInitialVelocity(Vector3 targetPosition, bool useDesiredTime)
    {
        Vector3 displacement = targetPosition - this.transform.position;
        float yDisplacement = displacement.y;
        displacement.y = 0.0f;  // only desire horizontal displacement here
        float horizontalDisplacement = displacement.magnitude;

        if (horizontalDisplacement < Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        float horizontalSpeed = useDesiredTime ? horizontalDisplacement / m_desiredAirTime : m_maxSpeed;

        Vector3 initialYVelocity = (Physics.gravity * -0.5f) - new Vector3(0f, transform.position.y, 0f);

        displacement.Normalize();
        Vector3 initialHorizontalVelocity = displacement * horizontalSpeed;
        return initialHorizontalVelocity + initialYVelocity;
    }

    public void ApplyGaugeMultiplier(float multiplier)
    {
        m_rb.velocity = CalculateInitialVelocityMovingTarget() * multiplier;
    }

    public struct VelocityTimeData
    {
        public float time;
        public Vector3 velocity;
    }

    private Vector3 CalculateIntersection()
    {
        Vector3 targetPos = m_movingTarget.transform.position;
        Vector3 targetVel = m_movingTarget.GetVelocity();
        Vector3 originPos = this.transform.position;
        float dot = Vector3.Dot((originPos - targetPos), targetVel);
        return targetPos + targetVel * dot / targetVel.sqrMagnitude;
    }

    private VelocityTimeData CalculateWithMaxVel(Vector3 targetPos)
    {
        VelocityTimeData data = new VelocityTimeData();
        Vector3 displacement = targetPos - this.transform.position;
        float heightDisplacement = displacement.y;
        displacement.y = 0f;
        float horizontalDisplacement = displacement.magnitude;

        float a = (Physics.gravity.y * Mathf.Pow(horizontalDisplacement, 2)) / (2f * Mathf.Pow(m_maxSpeed, 2));
        float c = a - heightDisplacement;
        float tanThetaPlus = QuadraticFormula(a, horizontalDisplacement, c, 1);
        float tanThetaMinus = QuadraticFormula(a, horizontalDisplacement, c, -1);
        float thetaPlus = Mathf.Atan(tanThetaPlus);
        float thetaMinus = Mathf.Atan(tanThetaMinus);
        float theta = Mathf.Min(thetaMinus, thetaPlus);
        //Debug.LogFormat("Theta = {0}", theta * Mathf.Rad2Deg);

        Vector3 velocity = displacement.normalized * m_maxSpeed * Mathf.Cos(theta);
        velocity.y = m_maxSpeed * Mathf.Sin(theta);

        data.velocity = velocity;
        data.time = horizontalDisplacement / (m_maxSpeed * Mathf.Cos(theta));

        return data;
    }

    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPosFinal = m_movingTarget.transform.position;

        if (m_movingTarget.GetVelocity() == Vector3.zero)
        {
            return targetPosFinal;
        }

        Vector3 pos1 = m_movingTarget.transform.position;
        Vector3 pos2 = CalculateIntersection();
        RaycastHit hitInfo;
        int layermask = 1 << LayerMask.NameToLayer("Wall");
        // 71 = sqrt(50^2 + 50^2), ie court dimensions
        Physics.Raycast(pos1, m_movingTarget.GetVelocity(), out hitInfo, 71f, layermask);
        Vector3 pos3 = hitInfo.point;
        VelocityTimeData data = CalculateWithMaxVel(pos2);

        Vector3 temp = FindNewTargetPosition(data.time);

        if (Vector3.Distance(pos1, temp) < Vector3.Distance(pos1, pos2))
        {
            // between pos1 and pos2
            targetPosFinal = PositionBinarySearch(pos1, pos2);
        }
        else if (Vector3.Distance(pos1, temp) > Vector3.Distance(pos1, pos2))
        {
            // between pos2 and pos3
            targetPosFinal = PositionBinarySearch(pos2, pos3);
        }
        else //if (Vector3.Distance(pos1, temp) - Vector3.Distance(pos1, pos2) < 0.5f)
        {
            return pos2;
        }

        return targetPosFinal;
    }

    private Vector3 PositionBinarySearch(Vector3 pos1, Vector3 pos2)
    {
        Vector3 aimPos = Vector3.zero;
        Vector3 targetPos = Vector3.zero;
        float newTime = 0f;
        bool notCloseEnough = true;

        while (notCloseEnough)
        {
            aimPos = 0.5f * (pos1 + pos2);
            newTime = CalculateWithMaxVel(aimPos).time;
            targetPos = FindNewTargetPosition(newTime);

            if (Vector3.Distance(targetPos, aimPos) <= 0.5f)
            {
                notCloseEnough = false;
            }
            else if (Vector3.Distance(pos1, targetPos) < Vector3.Distance(pos1, aimPos))
            {
                pos2 = aimPos;
            }
            else
            {
                pos1 = aimPos;
            } 
        }

        return aimPos;
    }

    private Vector3 FindNewTargetPosition(float time)
    {
        return m_movingTarget.transform.position + m_movingTarget.GetVelocity() * time;
    }

    private float QuadraticFormula(float a, float b, float c, int sign)
    {
        //Debug.Log("A = " + a);
        //Debug.Log("B = " + b);
        //Debug.Log("C = " + c);
        //Debug.Log("Sign = " + sign);
        //Debug.LogFormat("SQRT = {0}", Mathf.Pow(b, 2) - 4 * a * c);
        return (-b + Mathf.Sqrt(Mathf.Pow(b, 2) - 4 * a * c) * sign) / (2 * a);
    }

    private void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.tag == "Target")
        {
            //Debug.Log("Time Elapsed: " + m_timeElapsed);
        }
    }
}
