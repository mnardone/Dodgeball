using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Agent : MonoBehaviour {

    [Header("Posts")]
    [SerializeField] private Transform m_redPost;
    [SerializeField] private Transform m_bluePost;
    [Header("Team Colours")]
    [SerializeField] private Material m_redMaterial;
    [SerializeField] private Material m_blueMaterial;
    [SerializeField] private Material m_defaultMaterial;
    [Header("Linear and Angular Motion")]
    [SerializeField] private float m_linearMaxSpeed = 0.0f;
    [SerializeField] private float m_angularMaxSpeed = 0.0f;
    [SerializeField] private float m_linearAcceleration = 0.0f;
    [SerializeField] private float m_angularAcceleration = 0.0f;
    [SerializeField] private AIAgentController m_controller = null;
    [SerializeField] private Text m_hitText = null;
    [SerializeField] private Text m_catchText = null;

    private int m_areaMask;

    public float linearSpeed { get; set; }

    public float linearMaxSpeed { get { return m_linearMaxSpeed; } }

    public float angularSpeed { get; set; }

    public float angularMaxSpeed { get { return m_angularMaxSpeed; } }

    private Rigidbody m_rb = null;

    public enum Team
    {
        Red = 0,
        Blue
    }

    private Team m_team;

    public enum State
    {
        Wander = 0,
        Attack,
        Defend
    }

    private State m_state;

    private bool m_agentProximity = false;

    public bool AgentProximity
    {
        get { return m_agentProximity; }
    }

    void Awake ()
    {
        m_rb = GetComponent<Rigidbody>();
        DetermineAgentColour();
        m_areaMask = 1 << NavMesh.GetAreaFromName(m_team.ToString());
        m_state = State.Wander;
        m_hitText.gameObject.SetActive(false);
        m_catchText.gameObject.SetActive(false);
    }

    //private void Update()
    //{
    //    Debug.LogFormat("{0} is in {1}", this.gameObject.name, m_state.ToString());
    //}

    protected void DetermineAgentColour()
    {
        Vector3 agentPosition = transform.position;
        float distanceToRedPost = Vector3.Distance(agentPosition, m_redPost.position);
        float distanceToBluePost = Vector3.Distance(agentPosition, m_bluePost.position);

        if (Mathf.Abs(distanceToBluePost - distanceToRedPost) < 0.5f)
        {
            SetMaterial(m_defaultMaterial);
        }
        else if (distanceToRedPost > distanceToBluePost)
        {
            SetMaterial(m_blueMaterial);
            m_team = Team.Blue;
        }
        else
        {
            SetMaterial(m_redMaterial);
            m_team = Team.Red;
        }
    }

    protected void SetMaterial(Material mat)
    {
        MeshRenderer rend = GetComponent<MeshRenderer>();
        rend.material = mat;
    }

    public Team GetTeam()
    {
        return m_team;
    }

    public int GetAreaMask()
    {
        return m_areaMask;
    }

    public State GetState()
    {
        return m_state;
    }

    public void ChangeState(int state)
    {
        m_state = (State)state;
    }

    public void MoveForwards()
    {
        m_rb.velocity = transform.forward * linearSpeed;
    }

    public void MoveBackwards()
    {
        m_rb.velocity = transform.forward * linearSpeed * -1.0f;
    }

    public void StrafeLeft()
    {
        //m_rb.velocity = transform.right * linearSpeed * -1.0f;

        m_rb.AddForce(transform.right * linearSpeed * -1f, ForceMode.Force);
    }

    public void StrafeRight()
    {
        //m_rb.velocity = transform.right * linearSpeed;

        m_rb.AddForce(transform.right * linearSpeed, ForceMode.Force);
    }

    public void TurnRight()
    {
        m_rb.angularVelocity = transform.up * angularSpeed;
    }

    public void TurnLeft()
    {
        m_rb.angularVelocity = transform.up * angularSpeed * -1.0f;
    }

    public void StopLinearVelocity()
    {
        Vector3 stopLinearVelocity = m_rb.velocity;
        stopLinearVelocity.x = 0.0f;
        stopLinearVelocity.z = 0.0f;
        m_rb.velocity = stopLinearVelocity;
    }

    public void StopAngularVelocity()
    {
        m_rb.angularVelocity = Vector3.zero;
    }

    private void ActivatePopUpText(Transform obj, Text text)
    {
        text.gameObject.SetActive(true);
        StartCoroutine(DisappearingText(text));
    }

    private IEnumerator DisappearingText(Text text)
    {
        yield return new WaitForSeconds(3f);
        text.gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.tag == "InteractableObject")
        {
            if (c.gameObject.GetComponent<BallProjectile>().inAir)
            {
                if (m_team == Team.Blue)
                {
                    DodgeballManager.Instance.ScoreRed += 1;
                }
                else
                {
                    DodgeballManager.Instance.ScoreBlue += 1;
                }

                ActivatePopUpText(c.gameObject.transform, m_hitText);
            }
            else
            {
                m_controller.PickUpBall(c.gameObject);
                ActivatePopUpText(c.gameObject.transform, m_catchText);
            }
        }
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.gameObject.tag == "Agent")
        {
            m_agentProximity = true;
        }
    }

    private void OnTriggerExit(Collider c)
    {
        if (c.gameObject.tag == "Agent")
        {
            m_agentProximity = false;
        }
    }
}
