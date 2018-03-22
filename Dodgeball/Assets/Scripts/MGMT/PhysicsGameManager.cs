using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhysicsGameManager : MonoBehaviour
{
    private static PhysicsGameManager _instance = null;

    public static PhysicsGameManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    [Header("Round Selection and Targets")]
    [SerializeField] private GameObject m_roundSelectionHUD = null;
    [SerializeField] private GameObject[] m_roundOneTargets = new GameObject[3];
    [SerializeField] private GameObject[] m_roundTwoTargets = new GameObject[3];
    [SerializeField] private GameObject[] m_roundThreeTargets = new GameObject[3];

    [Header("Gameplay HUD")]
    [SerializeField] private Text m_textRound = null;
    [SerializeField] private Text m_textShots = null;
    [SerializeField] private Text m_textScore = null;
    [SerializeField] private Text m_textHighscore = null;

    // Player number of shots
    private int m_shotsLeft = 3;
    private const int TOTAL_SHOTS_PER_ROUND = 3;
    // Player Score
    private int m_currentScore = 0;
    private int m_highScore = 0;

    // Boundaries for Targets in the play space
    private const float MIN_X_BOUNDARY = -20f;
    private const float MAX_X_BOUNDARY = 20f;
    private const float MIN_Y_BOUNDARY = 8f;
    private const float MAX_Y_BOUNDARY = 24f;
    private const float MIN_Z_BOUNDARY = 5f;
    private const float MAX_Z_BOUNDARY = 20f;

    // Used to determine score gained when a target is hit. Each circle is 0.5 units greater than the previous.
    private const float TARGET_CIRCLE_RADIUS = 0.5f;

    private void Awake()
    {
        if (_instance)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
    }

    public void StartRoundOne()
    {
        ToggleRoundSelectionHUD();
        SpawnRound(m_roundOneTargets);
    }

    public void StartRoundTwo()
    {
        ToggleRoundSelectionHUD();
        SpawnRound(m_roundTwoTargets);
    }

    public void StartRoundThree()
    {
        ToggleRoundSelectionHUD();
        SpawnRound(m_roundThreeTargets);
    }

    private void ToggleRoundSelectionHUD()
    {
        m_roundSelectionHUD.SetActive(!m_roundSelectionHUD.activeSelf);
    }

    private void SpawnRound(GameObject[] targets)
    {
        for (int i = 0; i < targets.Length; ++i)
        {
            SpawnTarget(targets[i]);
        }
    }

    private void SpawnTarget(GameObject target)
    {
        target.GetComponent<Target>().Initialize();
    }

    public void CheckBoundaries(Rigidbody rb)
    {
        Vector3 pos = rb.transform.position;

        if (CheckOutOfBounds(pos.x, MIN_X_BOUNDARY, MAX_X_BOUNDARY)
            || CheckOutOfBounds(pos.y, MIN_Y_BOUNDARY, MAX_Y_BOUNDARY)
            || CheckOutOfBounds(pos.z, MIN_Z_BOUNDARY, MAX_Z_BOUNDARY))
        {
            rb.velocity *= -1f;
        }
    }

    private bool CheckOutOfBounds(float value, float min, float max)
    {
        if (value < min || value > max)
        {
            return true;
        }

        return false;
    }
}
