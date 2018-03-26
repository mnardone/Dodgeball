using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject m_panelPhysicsControls = null;
    [SerializeField] private GameObject m_panelDodgeballControls = null;
    [SerializeField] private GameObject m_panelOptions = null;
    [SerializeField] private GameObject m_panelScoreboard = null;

    private void Awake()
    {
        m_panelPhysicsControls.SetActive(false);
        m_panelDodgeballControls.SetActive(false);
        m_panelOptions.SetActive(false);
        m_panelScoreboard.SetActive(false);
        //TogglePhysicsControls();
        //ToggleDodgeballControls();
        //ToggleOptions();
        //ToggleScoreboard();
    }

    public void PlayPhysics()
    {
        SceneManager.LoadScene(2);
    }

    public void TogglePhysicsControls()
    {
        m_panelPhysicsControls.SetActive(!m_panelPhysicsControls.activeSelf);
    }

    public void PlayDodgeball()
    {
        SceneManager.LoadScene(1);
    }

    public void ToggleDodgeballControls()
    {
        m_panelDodgeballControls.SetActive(!m_panelDodgeballControls.activeSelf);
    }

    public void ToggleOptions()
    {
        m_panelOptions.SetActive(!m_panelOptions.activeSelf);
    }

    public void ToggleScoreboard()
    {
        m_panelScoreboard.SetActive(!m_panelScoreboard.activeSelf);
    }

    public void Quit()
    {
        Debug.Log("Application Quit");
        Application.Quit();
    }
}
