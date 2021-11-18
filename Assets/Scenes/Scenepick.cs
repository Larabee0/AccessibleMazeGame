using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Scenepick : MonoBehaviour
{
    public void UIonClick()
    {
        SceneManager.LoadScene(1);
    }

    public void MazeonClick()
    {
        SceneManager.LoadScene(2);
    }
}
