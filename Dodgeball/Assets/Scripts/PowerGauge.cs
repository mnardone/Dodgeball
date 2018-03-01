using UnityEngine;
using UnityEngine.SceneManagement;

public class PowerGauge : MonoBehaviour
{
    [SerializeField] private Ball m_ball = null;
    [SerializeField] private BallProjectile m_proj = null;
    [SerializeField] private Transform m_powerGaugeSlider = null;
    [SerializeField] private float m_sliderSpeed = 1f;

    private bool m_currentDirLeft = false;
    private Vector3 m_nextMovement = Vector3.zero;

    private const float RIGHT_BOUNDARY = 220f;
    private const float LEFT_BOUNDARY = -220f;

    private void Start ()
    {
        if (!m_powerGaugeSlider)
            m_powerGaugeSlider = GameObject.Find("Image_Slider").GetComponent<RectTransform>();

        m_currentDirLeft = true;
        m_sliderSpeed = Mathf.Max(m_sliderSpeed, 1f);   // Ensures m_sliderSpeed >= 1
        m_nextMovement = transform.up * m_sliderSpeed;
	}

    private void FixedUpdate ()
    {
        if (m_powerGaugeSlider.localPosition.x <= LEFT_BOUNDARY || m_powerGaugeSlider.localPosition.x >= RIGHT_BOUNDARY)
            m_nextMovement *= -1f;

        m_powerGaugeSlider.transform.Translate(m_nextMovement);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float multiplier = 0f;
            m_nextMovement = Vector3.zero;

            // Shuffle board: centre is correct force, left/right are less force
            if (SceneManager.GetActiveScene().buildIndex == 1)
            {
                multiplier = (220f - Mathf.Abs(m_powerGaugeSlider.localPosition.x)) / 220f;
                m_ball.ShuffleBoard(multiplier);
            }
            // Projectile: centre is correct force,  left is less force, right is more force
            else if (SceneManager.GetActiveScene().buildIndex == 2)
            {
                multiplier = (220f + m_powerGaugeSlider.localPosition.x) / 220f;
                m_proj.ApplyGaugeMultiplier(multiplier);
            }

            Debug.LogFormat("Gauge Multiplier %: {0}", multiplier * 100f);
        }
	}
}
