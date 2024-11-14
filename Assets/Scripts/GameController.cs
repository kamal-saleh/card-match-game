using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    // Singleton instance
    public static GameController instance;

    // Public references
    public GameObject cardPrefab; // The card prefab to instantiate
    public Transform gameBoard;   // The GameBoard panel
    public TextMeshProUGUI scoreText; // UI Text to display the score
    public GameObject gameOverPanel; // Panel to display when the game is over

    // Audio sources
    public AudioSource flipAudioSource;     // Sound to play when a card is flipped
    public AudioSource matchAudioSource;    // Sound to play when a match is found
    public AudioSource mismatchAudioSource; // Sound to play when a mismatch is found
    public AudioSource gameOverAudioSource; // Sound to play when the game is over

    // Grid configuration
    public int rows = 2;       // Number of rows in the grid
    public int columns = 2;    // Number of columns in the grid
    public float spacing = 10f;    // Spacing between cards

    // Game logic variables
    [HideInInspector]
    public bool canFlip = true; // Indicates if the player can flip cards

    private CardController firstFlippedCard;
    private CardController secondFlippedCard;
    private int score = 0;
    private int matchesFound = 0;
    private int totalMatches;

    // Added variables for grid layout
    private GridLayoutGroup gridLayoutGroup;

    void Awake()
    {
        // Initialize the singleton instance
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Get the GridLayoutGroup component from the GameBoard
        gridLayoutGroup = gameBoard.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup == null)
        {
            Debug.LogError("GridLayoutGroup component not found on GameBoard.");
            return;
        }

        // Configure the grid layout based on rows and columns
        ConfigureGridLayout();

        // Calculate the total number of matches needed to win
        totalMatches = (rows * columns) / 2;

        // Initialize the score display
        UpdateScore(0);

        // Generate the cards on the game board
        GenerateCards();
    }

    // Method to configure the grid layout dynamically
    void ConfigureGridLayout()
    {
        // Set spacing between cards
        gridLayoutGroup.spacing = new Vector2(spacing, spacing);

        // Set padding (adjust if necessary)
        gridLayoutGroup.padding = new RectOffset(10, 10, 10, 10);

        // Set the grid constraint to fixed column count
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = columns;

        // Get the RectTransform of the GameBoard
        RectTransform gameBoardRectTransform = gameBoard.GetComponent<RectTransform>();

        // Calculate available width and height for cards
        float availableWidth = gameBoardRectTransform.rect.width - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right - (gridLayoutGroup.spacing.x * (columns - 1));
        float availableHeight = gameBoardRectTransform.rect.height - gridLayoutGroup.padding.top - gridLayoutGroup.padding.bottom - (gridLayoutGroup.spacing.y * (rows - 1));

        // Calculate cell size based on available space and number of rows/columns
        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;

        // Optionally, maintain a specific aspect ratio (e.g., 2:3)
        float aspectRatio = 2f / 3f; // Width / Height
        if (cellWidth / cellHeight > aspectRatio)
        {
            // Adjust cellWidth to maintain aspect ratio
            cellWidth = cellHeight * aspectRatio;
        }
        else
        {
            // Adjust cellHeight to maintain aspect ratio
            cellHeight = cellWidth / aspectRatio;
        }

        // Set minimum and maximum cell sizes to prevent cards from becoming too small or too large
        float minCellSize = 50f;
        float maxCellSize = 200f;
        cellWidth = Mathf.Clamp(cellWidth, minCellSize, maxCellSize);
        cellHeight = Mathf.Clamp(cellHeight, minCellSize, maxCellSize);

        // Set the calculated cell size to the GridLayoutGroup
        gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);
    }

    // Method to generate and place cards on the game board
    void GenerateCards()
    {
        // Ensure the total number of cards is even
        if ((rows * columns) % 2 != 0)
        {
            Debug.LogError("The total number of cards must be even.");
            return;
        }

        // Create a list of card values (pairs)
        List<string> cardValues = new List<string>();
        int numPairs = (rows * columns) / 2;

        for (int i = 1; i <= numPairs; i++)
        {
            // Add two of each value to the list
            cardValues.Add(i.ToString());
            cardValues.Add(i.ToString());
        }

        // Shuffle the card values
        Shuffle(cardValues);

        // Instantiate cards
        foreach (string value in cardValues)
        {
            GameObject cardObj = Instantiate(cardPrefab, gameBoard);
            CardController card = cardObj.GetComponent<CardController>();

            // Set the card's value and display
            card.SetCardValue(value);
        }
    }

    // Method to shuffle the list of card values
    void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            string temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Method called by a CardController when a card is flipped
    public void CardFlipped(CardController card)
    {
        if (firstFlippedCard == null)
        {
            firstFlippedCard = card;
        }
        else if (secondFlippedCard == null && card != firstFlippedCard)
        {
            secondFlippedCard = card;
            StartCoroutine(CheckMatch());
        }
    }

    // Coroutine to check if two flipped cards match
    IEnumerator CheckMatch()
    {
        canFlip = false;

        // Wait until both cards have finished animating
        yield return new WaitWhile(() => firstFlippedCard.IsAnimating || secondFlippedCard.IsAnimating);

        if (firstFlippedCard.cardValue == secondFlippedCard.cardValue)
        {
            // Match found
            score += 10;
            matchesFound++;

            // Play match sound
            if (matchAudioSource != null)
                matchAudioSource.Play();

            // Wait a moment to allow the player to see the matched cards
            yield return new WaitForSeconds(0.5f);

            // Disable the matched cards
            firstFlippedCard.gameObject.SetActive(false);
            secondFlippedCard.gameObject.SetActive(false);

            UpdateScore(score);

            // Check for game over
            if (matchesFound >= totalMatches)
            {
                // Start GameOver as a coroutine to handle sound timing
                StartCoroutine(GameOver());
            }
        }
        else
        {
            // No match, wait before flipping the cards back over
            yield return new WaitForSeconds(0.5f); // Wait to let the player see the cards

            // Flip the cards back over
            firstFlippedCard.FlipCard(false); // Flip to show back
            secondFlippedCard.FlipCard(false);

            // Play mismatch sound
            if (mismatchAudioSource != null)
                mismatchAudioSource.Play();

            score -= 5;
            UpdateScore(score);

            // Wait until both cards have finished animating
            yield return new WaitWhile(() => firstFlippedCard.IsAnimating || secondFlippedCard.IsAnimating);
        }

        // Reset the flipped cards
        firstFlippedCard = null;
        secondFlippedCard = null;
        canFlip = true;
    }

    // Method to update the score display
    void UpdateScore(int newScore)
    {
        scoreText.text = newScore.ToString();
    }

    // Coroutine called when all matches are found
    IEnumerator GameOver()
    {
        // Optionally wait for the match sound to finish
        if (matchAudioSource != null)
        {
            yield return new WaitWhile(() => matchAudioSource.isPlaying);
        }

        // Play game over sound
        if (gameOverAudioSource != null)
            gameOverAudioSource.Play();

        // Display the game over panel
        gameOverPanel.SetActive(true);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
