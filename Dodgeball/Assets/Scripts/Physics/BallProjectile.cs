﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BallProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody m_rb = null;

    [SerializeField] private float m_maxSpeed = 25f;

    // Moving target
    private GameObject m_target = null;
    private float m_desiredAirTime = 1.0f;

    private bool m_inAir = false;

    public bool inAir
    {
        get
        {
            return m_inAir;
        }
        set
        {
            m_inAir = value;
            //Debug.Log("inAir = " + inAir);
        }
    }

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        inAir = false;
    }

    // Generates a random value in a normal distribution
    private float GenerateRandomValue()
    {
        float mean = 0f;
        float stdDev = 1.2f;    // test test test --> 1.4 = approx 25% miss rate

        Random.InitState(System.DateTime.Now.Millisecond);
        float f1 = Random.Range(0f, 1f - Mathf.Epsilon);

        Random.InitState(System.DateTime.Now.Millisecond + 10);
        float f2 = Random.Range(0f, 1f - Mathf.Epsilon);

        //Debug.Log("F1 = " + f1);
        //Debug.Log("F2 = " + f2);

        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(f1)) * Mathf.Sin(2f * Mathf.PI * f2);
        float randNormal = mean + stdDev * randStdNormal;

        //Debug.Log("Random value = " + randNormal);

        return randNormal;
    }

    private Vector3 GenerateRandomUnitVector()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        Vector3 rand = Random.insideUnitSphere;

        //Debug.Log("Random unit vector = " + rand);

        return rand;
    }

    // This method is called by the Agent in Dodgeball
    public void Throw(GameObject target)
    {
        m_target = target;
        this.transform.SetParent(null);
        inAir = true;
        m_rb.isKinematic = false;

        Vector3 targetPos = CalculateTargetPosition();

        if (targetPos.y < -99f)
        {
            m_rb.velocity = m_target.transform.position.normalized * m_maxSpeed;
        }
        else
        {
            Random.InitState(System.DateTime.Now.Millisecond);
            //Debug.Log("Seed = " + System.DateTime.Now.Millisecond);

            targetPos += GenerateRandomUnitVector() * GenerateRandomValue();
            Vector3 velocity = CalculateWithMaxVel(targetPos).velocity;

            if (velocity == Vector3.zero)
            {
                m_rb.velocity = m_target.transform.position.normalized * m_maxSpeed;
            }
            else
            {
                //Debug.Log("Throwing... ");
                //Debug.Log("Target Pos = " + targetPos);
                //Debug.Log("Velocity = " + velocity);
                m_rb.velocity = velocity;
            }
        }
    }

    // This method is called by the ball in Hit The Target
    public void ApplyGaugeMultiplier(float multiplier, GameObject target)
    {
        m_target = target;
        m_rb.isKinematic = false;

        Vector3 targetPos = CalculateTargetPosition();

        if (targetPos.y < -99f)
        {
            m_rb.velocity = m_target.transform.position.normalized * m_maxSpeed * multiplier;
        }
        else
        {
            Vector3 velocity = CalculateWithMaxVel(targetPos).velocity * multiplier;

            if (velocity == Vector3.zero)
            {
                m_rb.velocity = m_target.transform.position.normalized * m_maxSpeed * multiplier;
            }
            else
            {
                m_rb.velocity = velocity;
            }
        }
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

        float radicand = Mathf.Pow(horizontalDisplacement, 2) - 4f * a * c;

        if (radicand < 0)
        {
            data.time = 0f;
        }
        else
        {
            float tanThetaPlus = QuadraticFormula(a, horizontalDisplacement, c, 1);
            float tanThetaMinus = QuadraticFormula(a, horizontalDisplacement, c, -1);
            float thetaPlus = Mathf.Atan(tanThetaPlus);
            float thetaMinus = Mathf.Atan(tanThetaMinus);
            float theta = Mathf.Min(thetaMinus, thetaPlus);

            velocity = displacement.normalized * m_maxSpeed * Mathf.Cos(theta);
            velocity.y = m_maxSpeed * Mathf.Sin(theta);

            data.time = horizontalDisplacement / (m_maxSpeed * Mathf.Cos(theta));
        }

        data.velocity = velocity;

        return data;
    }

    // Calculate the position to throw at based off the PositionBinarySearch's results
    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPosFinal = m_target.transform.position;
        Vector3 targetVel = m_target.GetComponent<Rigidbody>().velocity;

        if (targetVel == Vector3.zero)
        {
            return targetPosFinal;
        }

        Vector3 pos1 = m_target.transform.position;
        RaycastHit hitInfo;
        int layermask = 1 << LayerMask.NameToLayer("Wall");

        // 71 = sqrt(50^2 + 50^2), ie court dimensions
        bool rayHit = Physics.Raycast(pos1, m_target.GetComponent<Rigidbody>().velocity, out hitInfo, 71f, layermask);
        Vector3 pos3;

        if (rayHit)
        {
            pos3 = hitInfo.point;
        }
        else
        {
            pos3 = pos1 + targetVel * 5f;   // Current pos + Velocity * 5 seconds
        }

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
        Vector3 aimPos = new Vector3(0f, -100f, 0f);
        Vector3 targetPos = Vector3.zero;
        float newTime = 0f;
        bool notCloseEnough = true;
        int count = 0;

        while (notCloseEnough)
        {
            aimPos = 0.5f * (pos1 + pos2);                  // halfway point
            newTime = CalculateWithMaxVel(aimPos).time;     // time to throw to halfway point

            if (newTime == 0)
            {
                return aimPos;
            }

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
                return aimPos;
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

    private void OnCollisionEnter(Collision c)
    {
        //Debug.Log("Ball collided with... " + c.gameObject.tag);

        //if (c.gameObject.tag == "Agent")
        //{
        //    Random.InitState(System.DateTime.Now.Millisecond);
        //    float rand = Random.Range(0f, 1f);
        //    //Debug.Log("Hit/Catch Random = " + rand);

        //    if (rand < 0.5f)
        //    {
        //        inAir = false;
        //    }
        //}
        //else
        //{
        //    inAir = false;
        //}

        if (c.gameObject.tag != "Agent")
        {
            inAir = false;
        }

        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            //Debug.Log("In scene 2, collided with " + c.gameObject.name);
            if (c.gameObject.name == "Floor" && PhysicsGameManager.Instance.RoundActive && PhysicsGameManager.Instance.ShotsLeft != 0)
            {
                PhysicsGameManager.Instance.ResetBall();
            }
        }
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
