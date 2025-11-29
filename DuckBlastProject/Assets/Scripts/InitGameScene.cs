using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InitGameScene : MonoBehaviour
{
    [SerializeField] private Animator fallingSpritesAnimator;

    private void Start()
    {
        PlayTransition();
    }

    private void PlayTransition()
    {
        if (fallingSpritesAnimator != null)
        {
            fallingSpritesAnimator.SetTrigger("PlayOpening");
        }
    }
}