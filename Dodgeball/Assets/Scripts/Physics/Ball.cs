using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private Transform m_desiredDestination;

    private Rigidbody m_rb = null;
    private Vector3 m_startPos = Vector3.zero;
    private bool m_started = false;

    enum EFrictionType
    {
        EFT_Static = 0,
        EFT_Dynamic,
    };

    private void Start()
    {
        m_rb = GetComponent<Rigidbody>();
        m_startPos = this.transform.position;
        //Collider coll = GetComponent<Collider>();
        //coll.material.dynamicFriction;
    }

    private void Update()
    {
        if (m_started)
        {
            if (m_rb.velocity.magnitude < Mathf.Epsilon)
            {
                Debug.LogFormat("Distance %: {0}", (Vector3.Distance(m_startPos, this.transform.position) / Vector3.Distance(m_startPos, m_desiredDestination.position)) * 100f);
                m_started = false;
            }
        }
    }

    private float CalculateFrictionCoefficient(EFrictionType frictionType)
    {
        float frictionCoefficient = 0.0f;

        //determine our friction values
        Collider coll = GetComponent<Collider>();
        float ourFriction = (frictionType == EFrictionType.EFT_Static) ? coll.material.staticFriction : coll.material.dynamicFriction;
        PhysicMaterialCombine ourCombine = coll.material.frictionCombine;

        //check if we are colliding against an object
        Vector3 ourPosition = transform.position;
        float ourHeight = transform.localScale.y;
        float ourGroundBuffer = 0.25f;

        int layer = LayerMask.NameToLayer("Ball");
        int layerMask = 1 << layer;
        layerMask = ~layerMask;
        RaycastHit hit;

        if (Physics.Raycast(ourPosition, -Vector3.up, out hit, (ourHeight * 0.5f) + ourGroundBuffer, layerMask))
        {
            float hitFriction = (frictionType == EFrictionType.EFT_Static) ? hit.collider.material.staticFriction : hit.collider.material.dynamicFriction;
            PhysicMaterialCombine hitCombine = hit.collider.material.frictionCombine;
            //Average < Minimum < Multiply < Maximum
            bool isMax = hitCombine == PhysicMaterialCombine.Maximum || ourCombine == PhysicMaterialCombine.Maximum;
            bool isMultiply = hitCombine == PhysicMaterialCombine.Multiply || ourCombine == PhysicMaterialCombine.Multiply;
            bool isMin = hitCombine == PhysicMaterialCombine.Minimum || ourCombine == PhysicMaterialCombine.Minimum;
            bool isAverage = hitCombine == PhysicMaterialCombine.Average || ourCombine == PhysicMaterialCombine.Average;

            if (isMax)
            {
                frictionCoefficient = hitFriction > ourFriction ? hitFriction : ourFriction;
            }
            else if (isMultiply)
            {
                frictionCoefficient = hitFriction * ourFriction;
            }
            else if (isMin)
            {
                frictionCoefficient = hitFriction < ourFriction ? hitFriction : ourFriction;
            }
            else if (isAverage)
            {
                frictionCoefficient = (hitFriction + ourFriction) * 0.5f;
            }
        }

        return frictionCoefficient;
    }

    private float CalculateNormalForce()
    {
        return Physics.gravity.y * -1.0f * m_rb.mass;
    }

    private float CalculateFrictionalForce(EFrictionType frictionType)
    {
        return CalculateNormalForce() * CalculateFrictionCoefficient(frictionType);
    }

    private float ConvertForceToAcceleration(float force, float mass)
    {
        return mass > Mathf.Epsilon ? force / mass : 0.0f;
    }

    private float CalculateInitialVelocity(float finalVelocity, float acceleration, float distance)
    {
        //vf2 = vi2 + 2ad
        //vf2 - 2ad = vi2
        //vi2 = vf2 - 2ad
        //sqrt(vi2) = sqrt(vf2 - 2ad)
        return Mathf.Sqrt((finalVelocity * finalVelocity) - (2 * acceleration * distance));
    }

    public void ShuffleBoard(float gaugeMultiplier)
    {
        Vector3 toDestination = m_desiredDestination.position - transform.position;
        toDestination.y = 0f;   // added to so only X and Z axis used for magnitude
        float distance = Mathf.Abs(toDestination.magnitude);    // changed from toDestination.z
        float frictionalForce = -1.0f * CalculateFrictionalForce(EFrictionType.EFT_Dynamic);
        float acceleration = ConvertForceToAcceleration(frictionalForce, m_rb.mass);
        float speed = CalculateInitialVelocity(0.0f, acceleration, distance);
        toDestination.Normalize();  // added to create direction vector
        m_rb.velocity = toDestination * speed * gaugeMultiplier;    // added multiplier
        m_started = true;
    }
}
