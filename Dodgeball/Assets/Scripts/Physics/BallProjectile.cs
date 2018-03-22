using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    private Rigidbody m_rb = null;

    // Stationary target
    private Transform m_targetTransform;

    [SerializeField] private float m_maxSpeed = 25f;

    // Moving target
    private GameObject m_target = null;
    private float m_desiredAirTime = 1.0f;

    public bool inAir { get; private set; }

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        inAir = false;
    }

    // Generates a random value in a normal distribution
    private float GenerateRandomValue()
    {
        float mean = 0f;
        float stdDev = 1.4f;    // test test test --> approx 25% miss rate

        float f1 = Random.Range(0f, 1f - Mathf.Epsilon);
        float f2 = Random.Range(0f, 1f - Mathf.Epsilon);

        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(f1)) * Mathf.Sin(2f * Mathf.PI * f2);
        float randNormal = mean + stdDev * randStdNormal;

        //Debug.Log("Random value = " + randNormal);

        return randNormal;
    }

    private Vector3 GenerateRandomUnitVector()
    {
        Vector3 rand = Random.insideUnitSphere;
        return rand;
    }

    // This method is called by the Agent in Dodgeball
    public void Throw(GameObject target)
    {
        m_target = target;
        this.transform.SetParent(null);
        inAir = true;
        Vector3 velocity = CalculateWithMaxVel(CalculateTargetPosition() + GenerateRandomUnitVector() * GenerateRandomValue()).velocity;
        m_rb.isKinematic = false;
        m_rb.velocity = velocity;
    }

    // This method is called by the ball in Hit The Target
    public void ApplyGaugeMultiplier(float multiplier, GameObject target)
    {
        m_target = target;
        m_rb.isKinematic = false;
        m_rb.velocity = CalculateWithMaxVel(CalculateTargetPosition()).velocity * multiplier;
    }

    public struct VelocityTimeData
    {
        public float time;
        public Vector3 velocity;
    }

    private VelocityTimeData CalculateWithMaxVel(Vector3 targetPos)
    {
        VelocityTimeData data = new VelocityTimeData();
        Vector3 displacement = targetPos - this.transform.position;
        Vector3 velocity = Vector3.zero;

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

        velocity = displacement.normalized * m_maxSpeed * Mathf.Cos(theta);
        velocity.y = m_maxSpeed * Mathf.Sin(theta);

        data.velocity = velocity;
        data.time = horizontalDisplacement / (m_maxSpeed * Mathf.Cos(theta));

        return data;
    }

    // Calculate the position to throw at based off the PositionBinarySearch's results
    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPosFinal = m_target.transform.position;

        if (m_target.GetComponent<Rigidbody>().velocity == Vector3.zero)
        {
            return targetPosFinal;
        }

        Vector3 pos1 = m_target.transform.position;
        RaycastHit hitInfo;
        int layermask = 1 << LayerMask.NameToLayer("Wall");

        // 71 = sqrt(50^2 + 50^2), ie court dimensions
        Physics.Raycast(pos1, m_target.GetComponent<Rigidbody>().velocity, out hitInfo, 71f, layermask);
        Vector3 pos3 = hitInfo.point;

        targetPosFinal = PositionBinarySearch(pos1, pos3);

        // Breaks if the target is past the intersection point
        #region OldMethod - Position Binary Search
        //if (Vector3.Distance(pos1, temp) < Vector3.Distance(pos1, pos2))
        //{
        //    // between pos1 and pos2
        //    //Debug.Log("Pos1 -> Temp distance = " + Vector3.Distance(pos1, temp));
        //    //Debug.Log("Pos1 -> Pos2 distance = " + Vector3.Distance(pos1, pos2));
        //    Debug.Log("Between current pos and intersection");
            
        //    targetPosFinal = PositionBinarySearch(pos1, pos2);
        //}
        //else if (Vector3.Distance(pos1, temp) > Vector3.Distance(pos1, pos2))
        //{
        //    // between pos2 and pos3
        //    Debug.Log("Between intersection and wall");
        //    targetPosFinal = PositionBinarySearch(pos2, pos3);
        //}
        //else //if (Vector3.Distance(pos1, temp) - Vector3.Distance(pos1, pos2) < 0.5f)
        //{
        //    Debug.Log("Shot already accurate");
        //    return pos2;
        //}
        #endregion

        return targetPosFinal;
    }

    // Perform something similar to a binary search to find the position to throw the ball
    // The position of the target and the wall they are facing are used as end points.
    // Find the time to throw to the halfway point between the end points,
    //   find the location of the target at that time, 
    //   refine results until within acceptable margin.
    private Vector3 PositionBinarySearch(Vector3 pos1, Vector3 pos2)
    {
        Vector3 aimPos = Vector3.zero;
        Vector3 targetPos = Vector3.zero;
        float newTime = 0f;
        bool notCloseEnough = true;
        int count = 0;

        while (notCloseEnough)
        {
            aimPos = 0.5f * (pos1 + pos2);                  // halfway point
            newTime = CalculateWithMaxVel(aimPos).time;     // time to throw to halfway point
            targetPos = FindNewTargetPosition(newTime);     // target's position after that time

            if (Vector3.Distance(targetPos, aimPos) <= 0.1f)
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

            ++count;

            if (count == 15)    // Only attempt this process 15 times. Successes typically run this loop 5-10 times.
            {
                notCloseEnough = false;
            }
        }

        return aimPos;
    }

    // Find the target's new position based on their current velocity and the parameter time
    private Vector3 FindNewTargetPosition(float time)
    {
        return m_target.transform.position + m_target.GetComponent<Rigidbody>().velocity * time;
    }

    private float QuadraticFormula(float a, float b, float c, int sign)
    {
        return (-b + Mathf.Sqrt(Mathf.Pow(b, 2) - 4 * a * c) * sign) / (2 * a);
    }

    // inAir = false --> Allows agents to remember the ball when scanning
    private void OnCollisionEnter(Collision c)
    {
        inAir = false;
    }

    // OLD METHOD - Based on an AIR TIME instead of MAX VELOCITY
    #region Old Method - Calculate Velocity using Air Time
    //private Vector3 CalculateInitialVelocityMovingTarget()
    //{
    //    //find out where the target will be in our desired time
    //    //aim for that position
    //    Vector3 targetVelocity = m_movingTarget.GetComponent<Rigidbody>().velocity;
    //    Vector3 targetDisplacement = targetVelocity * m_desiredAirTime;
    //    Vector3 targetPosition = m_movingTarget.transform.position + targetDisplacement;
    //    return CalculateInitialVelocity(targetPosition, true);
    //}

    //private Vector3 CalculateInitialVelocity(Vector3 targetPosition, bool useDesiredTime)
    //{
    //    Vector3 displacement = targetPosition - this.transform.position;
    //    float yDisplacement = displacement.y;
    //    displacement.y = 0.0f;  // only desire horizontal displacement here
    //    float horizontalDisplacement = displacement.magnitude;

    //    if (horizontalDisplacement < Mathf.Epsilon)
    //    {
    //        return Vector3.zero;
    //    }

    //    float horizontalSpeed = useDesiredTime ? horizontalDisplacement / m_desiredAirTime : m_maxSpeed;

    //    Vector3 initialYVelocity = (Physics.gravity * -0.5f) - new Vector3(0f, transform.position.y, 0f);

    //    displacement.Normalize();
    //    Vector3 initialHorizontalVelocity = displacement * horizontalSpeed;
    //    return initialHorizontalVelocity + initialYVelocity;
    //}
    #endregion

    // Old Method - PositionBinary Search initially used more initial points when finding the target's future position
    #region Old Method - Calculate Intersection Point
    //private Vector3 CalculateIntersection()
    //{
    //    Vector3 targetPos = m_target.transform.position;
    //    Vector3 targetVel = m_target.GetComponent<Rigidbody>().velocity;
    //    Vector3 originPos = this.transform.position;
    //    float dot = Vector3.Dot((originPos - targetPos), targetVel);
    //    return targetPos + targetVel * dot / targetVel.sqrMagnitude;
    //}

    // Calculate the velocity vector3 required to hit the target.
    // -Uses a maximum velocity
    // -Calculates the angle required based on the position of the target (Takes the lower angle from the results of Quadratic Forumla)
    #endregion
}
