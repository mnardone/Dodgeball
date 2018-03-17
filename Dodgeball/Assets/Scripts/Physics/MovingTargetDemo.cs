using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingTargetDemo : MonoBehaviour
{
    private Rigidbody m_rb = null;
    [SerializeField] private Vector3 m_velocity = Vector3.zero;

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();

		if (m_velocity == Vector3.zero) // if no value has been input in the Inspector
        {
            m_velocity = Vector3.right * 5f;
        }
	}

    private void FixedUpdate()
    {
		if (Input.GetKey(KeyCode.Alpha1))
        {
            m_rb.velocity = m_velocity;
        }
	}

    public Vector3 GetVelocity()
    {
        return m_rb.velocity;
    }

    private void OnCollisionEnter(Collision c)
    {
        //Debug.Log(c.gameObject.name);
        //foreach (ContactPoint p in c)
        //{
        //    Debug.Log(p.point);
        //}
    }
}
