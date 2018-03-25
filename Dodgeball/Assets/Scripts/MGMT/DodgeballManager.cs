using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DodgeballManager : MonoBehaviour
{
    private static DodgeballManager _instance = null;

    public static DodgeballManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    [Header("Gameplay HUD")]
    [SerializeField] private Text m_timerText = null;
    [SerializeField] private Text m_scoreTextRed = null;
    [SerializeField] private Text m_scoreTextBlue = null;
    [SerializeField] private GameObject m_panelPause = null;
    [SerializeField] private GameObject m_panelEnd = null;
    [SerializeField] private Text m_winnerText = null;
    [SerializeField] private Text m_scoreText = null;

    private int m_scoreRed = 0;
    private int m_scoreBlue = 0;
    private float m_timer = 0f;
    private bool m_gameOver = false;

    [SerializeField] private GameObject m_ball = null;

    public int ScoreRed
    {
        get
        {
            return m_scoreRed;
        }
        set
        {
            m_scoreRed = value;
            m_scoreTextRed.text = string.Format("RED TEAM\n{0}", m_scoreRed);
        }
    }

    public int ScoreBlue
    {
        get
        {
            return m_scoreBlue;
        }
        set
        {
            m_scoreBlue = value;
            m_scoreTextBlue.text = string.Format("BLUE TEAM\n{0}", m_scoreBlue);
        }
    }

    public float Timer
    {
        get
        {
            return m_timer;
        }
        set
        {
            m_timer = value;
            UpdateTimer();
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    private void Start ()
    {
        TogglePause();
        m_panelEnd.SetActive(false);
        CheckTimeScale(m_panelEnd);
        SpawnBall();
	}

    private void Update ()
    {
		if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        Timer += Time.deltaTime;

        CheckGameState();

        if (m_gameOver)
        {
            //Debug.Log("continuing to run");
            GameOver();
            m_panelEnd.SetActive(true);
        }
	}

    private void SpawnBall()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        float randX = Random.Range(-24f, 24f);
        float randZ = Random.Range(-24f, 24f);
        Vector3 spawnPos = new Vector3(randX, 1f, randZ);

        Instantiate(m_ball, spawnPos, Quaternion.identity);
    }

    private void UpdateTimer()
    {
        int minutes = (int)(m_timer / 60f);
        int seconds = (int)(m_timer % 60f);

        if (seconds < 10)
        {
            m_timerText.text = string.Format("{0}:0{1}", minutes, seconds);
        }
        else
        {
            m_timerText.text = string.Format("{0}:{1}", minutes, seconds);
        }
    }

    private void CheckGameState()
    {
        // 5 minutes or 5 points
        if (m_timer > 10f || m_scoreBlue == 5 || m_scoreRed == 5)
        {
            m_gameOver = true;
        }
    }

    private void GameOver()
    {
        Time.timeScale = 0f;

        if (m_scoreRed > m_scoreBlue)
        {
            m_winnerText.text = "RED TEAM WINS!";
        }
        else if (m_scoreBlue > m_scoreRed)
        {
            m_winnerText.text = "BLUE TEAM WINS!";
        }
        else
        {
            m_winnerText.text = "IT'S A DRAW...";
        }

        m_scoreText.text = string.Format("RED TEAM\t{0}\n\nBLUE TEAM\t{1}", m_scoreRed, m_scoreBlue);
    }

    private void ToggleEndPanel()
    {
        m_panelEnd.SetActive(!m_panelEnd.activeSelf);
        CheckTimeScale(m_panelEnd);
    }

    public void TogglePause()
    {
        m_panelPause.SetActive(!m_panelPause.activeSelf);
        CheckTimeScale(m_panelPause);
    }

    private void CheckTimeScale(GameObject obj)
    {
        if (obj.activeSelf)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
