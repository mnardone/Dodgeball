using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    private Rigidbody m_rb = null;
    private float m_timeElapsed = 0.0f;

    [Header("Stationary Target, Minimum Speed")]
    [SerializeField] private Transform m_targetTransform;
    [SerializeField] private float m_minSpeed;
    [Header("Moving Target, Desired Air Time")]
    [SerializeField] private MovingTarget m_movingTarget = null;
    [SerializeField] private float m_desiredAirTime = 1.0f;

    // Use this for initialization
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
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

        float horizontalSpeed = useDesiredTime ? horizontalDisplacement / m_desiredAirTime : m_minSpeed;

        Vector3 initialYVelocity = (Physics.gravity * -0.5f) - new Vector3(0f, transform.position.y, 0f);

        displacement.Normalize();
        Vector3 initialHorizontalVelocity = displacement * horizontalSpeed;
        return initialHorizontalVelocity + initialYVelocity;
    }

    public void ApplyGaugeMultiplier(float multiplier)
    {
        m_rb.velocity = CalculateInitialVelocityMovingTarget() * multiplier;
    }

    private void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.tag == "Target")
        {
            Debug.Log("Time Elapsed: " + m_timeElapsed);
        }
    }
}
