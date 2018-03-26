using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PhysicsGameManager : MonoBehaviour
{
    private static PhysicsGameManager _instance = null;

    public static PhysicsGameManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    [Header("Gameplay")]
    [SerializeField] private Player m_player = null;
    [SerializeField] private GameObject m_ball = null;
    [SerializeField] private Transform m_ballSpawnPosition = null;

    [Header("Round Selection and Targets")]
    [SerializeField] private GameObject m_roundSelectionHUD = null;
    [SerializeField] private Button m_roundOneButton = null;
    [SerializeField] private Button m_roundTwoButton = null;
    [SerializeField] private Button m_menuButton = null;
    [SerializeField] private GameObject[] m_roundOneTargets = new GameObject[3];
    [SerializeField] private GameObject[] m_roundTwoTargets = new GameObject[3];
    [SerializeField] private GameObject[] m_roundThreeTargets = new GameObject[3];

    [Header("Gameplay HUD")]
    [SerializeField] private Text m_textRound = null;
    [SerializeField] private Text m_textShots = null;
    [SerializeField] private Text m_textScore = null;
    [SerializeField] private Text m_textHighscore = null;

    private int m_currentRound = 0;
    private int m_shotsLeft = 3;
    private const int TOTAL_SHOTS_PER_ROUND = 3;
    private int m_currentScore = 0;
    private int m_highScore = 0;

    public bool RoundActive
    {
        get;
        set;
    }

    public int ShotsLeft
    {
        get
        {
            return m_shotsLeft;
        }
        set
        {
            m_shotsLeft = value;
            UpdateTextShots();
        }
    }

    public int CurrentScore
    {
        get
        {
            return m_currentScore;
        }
        set
        {
            m_currentScore = value;
            UpdateTextScore();
        }
    }

    public int HighScore
    {
        get
        {
            return m_highScore;
        }
        set
        {
            m_highScore = value;
            ScoreManager.HighScore = value;
            UpdateTextHighScore();
        }
    }

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
            //DontDestroyOnLoad(this);
        }

        RoundActive = false;
        Time.timeScale = 1f;
    }

    public void EndRound()
    {
        // remove current round targets from view
        // display round selection
        // disable the round just played

        RoundActive = false;

        if (m_currentRound == 1)
        {
            RemoveOldTargets(m_roundOneTargets);
        }
        else if (m_currentRound == 2)
        {
            RemoveOldTargets(m_roundTwoTargets);
        }
        else if (m_currentRound == 3)
        {
                RemoveOldTargets(m_roundThreeTargets);
        }
        else
        {
            Debug.Log("How did you get here? Current Round = " + m_currentRound);
        }

        m_shotsLeft = TOTAL_SHOTS_PER_ROUND;

        ToggleRoundSelectionHUD();
    }

    private void RemoveOldTargets(GameObject[] targets)
    {
        for (int i = 0; i < targets.Length; ++i)
        {
            targets[i].GetComponent<Target>().EndRound();
        }
    }

    private void UpdateTextShots()
    {
        m_textShots.text = string.Format("SHOTS LEFT\n{0}", ShotsLeft.ToString());
    }

    private void UpdateTextScore()
    {
        m_textScore.text = string.Format("SCORE\n{0}", CurrentScore.ToString());
        CompareHighScore();
    }

    private void CompareHighScore()
    {
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
        }
    }

    private void UpdateTextHighScore()
    {
        m_textHighscore.text = string.Format("HIGH SCORE\n{0}", HighScore.ToString());
    }

    public void ResetBall()
    {
        m_ball.transform.position = m_ballSpawnPosition.position;
        m_ball.GetComponent<Rigidbody>().isKinematic = true;
    }

    public void StartRoundOne()
    {
        m_roundOneButton.gameObject.SetActive(false);
        RoundActive = true;
        m_currentRound = 1;
        ToggleRoundSelectionHUD();
        SpawnRound(m_roundOneTargets);
        m_player.FindTargets(m_roundOneTargets);
        UpdateTextShots();
        m_textRound.text = "ROUND\n1";
    }

    public void StartRoundTwo()
    {
        m_roundTwoButton.gameObject.SetActive(false);
        RoundActive = true;
        m_currentRound = 2;
        ToggleRoundSelectionHUD();
        SpawnRound(m_roundTwoTargets);
        m_player.FindTargets(m_roundTwoTargets);
        UpdateTextShots();
        m_textRound.text = "ROUND\n2";
    }

    //public void StartRoundThree()
    //{
    //    ToggleRoundSelectionHUD();
    //    SpawnRound(m_roundThreeTargets);
    //}

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

    public void CycleNextTarget()
    {
        m_player.CycleNextTarget();
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(2);
    }

    private void OnLevelWasLoaded(int level)
    {
        if (level == 2)
        {
            if (this != _instance)
            {
                return;
            }

            HighScore = ScoreManager.HighScore;
        }
    }
}
