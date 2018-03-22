using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Target : MonoBehaviour
{
    private Rigidbody m_rb = null;

    [SerializeField] private Vector3 m_position = Vector3.zero;
    [SerializeField] private Vector3 m_velocity = Vector3.zero;

    private void Awake ()
    {
        m_rb = GetComponent<Rigidbody>();
	}

    private void FixedUpdate()
    {
        if (m_rb.velocity != Vector3.zero)
        {
            PhysicsGameManager.Instance.CheckBoundaries(m_rb);
        }
    }

    // Called by spawner when object is created
    public void Initialize()
    {
        transform.position = m_position;
        m_rb.velocity = m_velocity;
    }

    // Score added based on point of collision
    private void OnCollisionEnter(Collision c)
    {
        // 1) Check point of collision
        // 2) Check distance to centre
        // 3) if dist < (bullseye radius) then add 100
        //    else if dist < (inner radius) then add 50
        //    else if dist < (outer radius) then add 25
        //    else do nothing?
    }
}
