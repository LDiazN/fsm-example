using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public bool gameOver;
    public GameObject gameOverPanel;

    [Header("Camera Game Over")]
    public Camera gameCamera;
    public float finalCameraSize;
    public float zoomSpeed = 1;

    [Header("Music")]
    public AudioMixerSnapshot stealthSnapshot;
    public AudioMixerSnapshot chaseSnapshot;

    private int enemiesChasing;

    public static GameManager instance;

    void Awake()
    {
        gameOverPanel.SetActive(false);
        instance = this;
    }

    void Update()
    {
        if(gameOver)
            gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, finalCameraSize, Time.deltaTime * zoomSpeed);
    }

    public void GameOver()
    {
        if(!gameOver)
        {
            gameOver = true;
            gameOverPanel.SetActive(true);
            FindObjectOfType<PlayerController>().DisableMovement();
        }
    }

    public void UpdateEnemiesChasing(int amount)
    {
        enemiesChasing += amount;
        if(enemiesChasing == 0)
            stealthSnapshot.TransitionTo(1f);
        else
            chaseSnapshot.TransitionTo(0.25f);
    }
}
