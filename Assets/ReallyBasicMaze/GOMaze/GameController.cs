using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameObjectMaze
{
    [RequireComponent(typeof(MazeConstructor))]
    public class GameController : MonoBehaviour
    {
        public static GameController instance;
        [SerializeField] private FpsMovement player;
        [SerializeField] private Text timeLabel;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Color textColour;
        public Color TextColour
        {
            get
            {
                return timeLabel.color;
            }
            set
            {
                timeLabel.color = value;
                scoreLabel.color = value;
            }
        }
        private MazeConstructor generator;
        [SerializeField] private MazeConstructorWithBurst generatorBurst;
        [SerializeField] private ColourSettingsControllerV2 colourMenu;

        private DateTime startTime;
        private int timeLimit;
        private int reduceLimitBy;

        private int score;
        private bool goalReached;

        private void Awake()
        {
            TextColour = textColour;
            colourMenu.OnNewColours += ColourChanged_OnChangedEvent;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            instance = this;
        }

        void Start()
        {
            generator = GetComponent<MazeConstructor>();
            StartNewGame();
            colourMenu.gameObject.SetActive(false);
        }
        private void ColourChanged_OnChangedEvent(ColourChangedEventArgs e)
        {
            TextColour = e.textCurrent;
            generatorBurst.SetColours(e);
            player.enabled = true;
            player.charController.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void StartNewGame()
        {
            timeLimit = 80;
            reduceLimitBy = 5;
            startTime = DateTime.Now;
            score = 0;
            scoreLabel.text = "Score\n"+score.ToString();
            StartNewMazeBurst();
            //StartNewMaze();

        }

        private void StartNewMazeBurst()
        {
            float start = Time.realtimeSinceStartup;
            generatorBurst.GenerateNewMaze(13, 15, OnStartTrigger, OnGoalTrigger);
            Debug.Log("Total Maze Time " + (Time.realtimeSinceStartup - start) * 1000f + "ms");
            player.transform.position = new Vector3(generatorBurst.StartPosition.x, 1, generatorBurst.StartPosition.z);
            goalReached = false;
            player.charController.enabled = true;
            player.enabled = true;
            SetMinimapSettings();
            timeLimit -= reduceLimitBy;
            startTime = DateTime.Now;
        }

        public void SetMinimapSettings()
        {
            generatorBurst.miniMapBackground.gameObject.SetActive(colourMenu.MiniMapEnabled);
            generatorBurst.pathPlotter.enabled = colourMenu.MiniMapPathEnabled;
        }

        public void GetMinimapSettings()
        {
            colourMenu.MiniMapEnabled = generatorBurst.miniMapBackground.gameObject.activeInHierarchy;
            colourMenu.MiniMapPathEnabled = generatorBurst.pathPlotter.enabled;
        }

        private void StartNewMaze()
        {
            float start = Time.realtimeSinceStartup;
            generator.GenerateNewMaze(13, 15, OnStartTrigger, OnGoalTrigger);
            Debug.Log("Total Maze Time " + (Time.realtimeSinceStartup - start) * 1000f + "ms");

            player.transform.position = new Vector3(generator.StartPosition.x, 1, generator.StartPosition.z);
            goalReached = false;
            player.enabled = true;
            player.charController.enabled = true;

            timeLimit -= reduceLimitBy;
            startTime = DateTime.Now;
        }
        
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                player.enabled = false;

                player.charController.enabled = false;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                colourMenu.gameObject.SetActive(!colourMenu.gameObject.activeInHierarchy);
                if (!colourMenu.gameObject.activeInHierarchy)
                {
                    SetMinimapSettings();
                }

            }
            if (!player.enabled)
            {
                return;
            }
            int timeUsed = (int)(DateTime.Now - startTime).TotalSeconds;
            int timeLeft = timeLimit - timeUsed;
            if (timeLeft > 0)
            {
                timeLabel.text = "Time Left\n"+ timeLeft.ToString();
            }
            else
            {
                timeLabel.text = "TIME UP";
                player.enabled = false;

                player.charController.enabled = false;
                StartNewMazeBurst();
                //StartNewMaze();
            }
        }

        private void OnGoalTrigger(GameObject trigger, GameObject other)
        {
            Debug.Log("Goal");
            goalReached = true;
            score += 1;
            scoreLabel.text = "Score\n"+ score.ToString();
            generatorBurst.HideEnd();
            generatorBurst.CyclePathColour();
        }

        private void OnStartTrigger(GameObject trigger, GameObject other)
        {
            if (goalReached)
            {
                Debug.Log("Finish");
                timeLabel.text = "Finished, loading next maze...";
                player.enabled = false;

                player.charController.enabled = false;
                StartNewMazeBurst();
               // StartNewMaze();
            }
        }
    }
}