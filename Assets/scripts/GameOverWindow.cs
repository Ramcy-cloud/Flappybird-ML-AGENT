using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeMonkey.Utils;

public class GameOverWindow : MonoBehaviour
{

    private Text scoreText;
    private Text highscoreText;

    private void Awake()
    {
        scoreText = transform.Find("scoreText").GetComponent<Text>();
        highscoreText = transform.Find("highscoreText").GetComponent<Text>();

        transform.Find("retryBtn").GetComponent<Button_UI>().ClickFunc = () => { Loader.Load(Loader.Scene.GameScene); };
        transform.Find("retryBtn").GetComponent<Button_UI>().AddButtonSounds();

        transform.Find("mainMenuBtn").GetComponent<Button_UI>().ClickFunc = () => { Loader.Load(Loader.Scene.MainMenu); };
        transform.Find("mainMenuBtn").GetComponent<Button_UI>().AddButtonSounds();

        transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    private void Start()
    {
        Bird.GetInstance().OnDied += Bird_OnDied;
        // ← NOUVEAU : se cache quand l'agent repart
        Bird.GetInstance().OnStartedPlaying += Bird_OnStartedPlaying;
        Hide();
    }

    private void Bird_OnStartedPlaying(object sender, System.EventArgs e)
    {
        // Cache le Game Over dès que l'agent commence un nouvel épisode
        Hide();
    }

    private void Bird_OnDied(object sender, System.EventArgs e)
    {
        scoreText.text = Level.GetInstance().GetPipesPassedCount().ToString();

        if (Level.GetInstance().GetPipesPassedCount() >= Score.GetHighscore())
        {
            highscoreText.text = "NEW HIGHSCORE";
        }
        else
        {
            highscoreText.text = "HIGHSCORE: " + Score.GetHighscore();
        }

        Show();
    }

    // Update supprimé — le Space ne doit pas recharger la scène pendant l'entraînement !

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

}