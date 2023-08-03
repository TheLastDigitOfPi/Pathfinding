using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    public GameObject PauseButton;
    public GameObject PlayButton;
    public void ToggleRunningButton()
    {
        PauseButton.SetActive(!PauseButton.activeSelf);
        PlayButton.SetActive(!PlayButton.activeSelf);
    }

    public void ResetButtons()
    {
        PauseButton.SetActive(false);
        PlayButton.SetActive(true);
    }
}
