using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour {

    private static ScoreManager _instance = null;

    public static ScoreManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }
    }

    private static int m_highScore = 0;

    public static int HighScore
    {
        get
        {
            return m_highScore;
        }
        set
        {
            m_highScore = value;
        }
    }

    private void Awake ()
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
}
