using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private GameObject[] m_targets;
    private int m_currentTarget = 0;

    [SerializeField] private Transform m_targetIndicator = null;
    [SerializeField] private BallProjectile m_ball = null;
    [SerializeField] private PowerGauge m_powerGauge = null;

    private int CurrentTarget
    {
        get
        {
            return m_currentTarget;
        }
        set
        {
            m_currentTarget = value;
            UpdateTargetIndicator();
        }
    }

    private void Start()
    {
        m_powerGauge.gameObject.SetActive(false);
    }

    private void Update ()
    {
        if (PhysicsGameManager.Instance.RoundActive)
        {
            HandleInput();
        }
	}

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TargetCycleLeft();
            CheckAllTargets();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            TargetCycleRight();
            CheckAllTargets();
        }

        // Start throw process
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (PhysicsGameManager.Instance.ShotsLeft <= 0)
            {
                Debug.Log("Shots left = " + PhysicsGameManager.Instance.ShotsLeft);
                PhysicsGameManager.Instance.EndRound();
            }
            else
            {
                if (m_powerGauge.gameObject.activeSelf == false)
                {
                    PhysicsGameManager.Instance.ResetBall();
                    m_powerGauge.gameObject.SetActive(true);
                }
                else
                {
                    m_ball.ApplyGaugeMultiplier(m_powerGauge.GetMultiplier(), m_targets[m_currentTarget]);
                    m_powerGauge.gameObject.SetActive(false);
                    PhysicsGameManager.Instance.ShotsLeft -= 1;
                }
            }
        }
    }

    private void TargetCycleLeft()
    {
        if (CurrentTarget == 0)
        {
            CurrentTarget = 2;
        }
        else
        {
            CurrentTarget -= 1;
        }
    }

    private void TargetCycleRight()
    {
        CurrentTarget = (CurrentTarget + 1) % 3;
    }

    public void CycleNextTarget()
    {
        CheckAllTargets();
    }

    public void FindTargets(GameObject[] targets)
    {
        m_targets = targets;
        UpdateTargetIndicator();
    }

    private bool CheckTargetAvailable()
    {
        if (m_targets[m_currentTarget].GetComponent<Rigidbody>().useGravity == false)
        {
            return true;
        }

        return false;
    }

    private void CheckAllTargets()
    {
        for (int i = 0; i < m_targets.Length; ++i)
        {
            if (!CheckTargetAvailable())
            {
                TargetCycleRight();
            }
            else
            {
                i = m_targets.Length;
            }
        }
    }

    private void UpdateTargetIndicator()
    {
        m_targetIndicator.SetParent(m_targets[m_currentTarget].transform);
        m_targetIndicator.localPosition = Vector3.zero + Vector3.up;
        m_targetIndicator.rotation = Quaternion.identity;
        m_targetIndicator.Rotate(180f, 0f, 0f);
        m_targetIndicator.localScale = new Vector3(2f, 2f, 10f);
    }
}
