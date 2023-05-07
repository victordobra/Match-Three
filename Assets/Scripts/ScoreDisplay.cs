using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour {
    private GameManager gameManager;
    private TextMeshProUGUI text;

    void Start() {
        // Get the game manager
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();

        // Get the text component
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update() {
        // Set the text
        text.text = "Score: " + gameManager.score;
    }
}
