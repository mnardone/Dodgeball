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
        if (m_rb.velocity != Vector3.zero && m_rb.useGravity == false)
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

    public void EndRound()
    {
        //Debug.Log("Ending round for " + name);
        transform.position = Vector3.up * -20f;
        m_rb.useGravity = false;
        m_rb.velocity = Vector3.zero;
    }

    // Score added based on point of collision
    private void OnCollisionEnter(Collision c)
    {
        //Debug.Log(name + " is colliding with something");
        if (m_rb.useGravity == false && c.gameObject.tag != "Target")
        {
            float distanceToCentre = Vector3.Distance(this.transform.position, c.contacts[0].point);
            int multiplier = Mathf.FloorToInt(distanceToCentre * 2f);
            int score = Mathf.Max(0, 100 - 20 * multiplier);
            PhysicsGameManager.Instance.CurrentScore += score;
            m_rb.useGravity = true;
            PhysicsGameManager.Instance.CycleNextTarget();
            
        }
    }
}
