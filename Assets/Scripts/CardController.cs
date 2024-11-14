using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CardController : MonoBehaviour
{
    // Public properties
    public string cardValue; // The value or identifier of the card
    public bool isFlipped = false; // Indicates if the card is currently flipped

    // Private references
    private GameObject frontContent; // Reference to the FrontContent GameObject
    private GameObject backContent;  // Reference to the BackContent GameObject
    private AudioSource flipSound;   // Sound to play when a card is flipped

    private Animator animator;       // Animator component for the flip animation
    private bool isAnimating = false; // Indicates if the card is currently animating

    // Public property to expose isAnimating
    public bool IsAnimating
    {
        get { return isAnimating; }
    }

    void Awake()
    {
        // Get references to the content GameObjects
        frontContent = transform.Find("FrontContent").gameObject;
        backContent = transform.Find("BackContent").gameObject;

        // Ensure initial visibility
        frontContent.SetActive(false);
        backContent.SetActive(true);

        // Get the flip sound from the GameController
        flipSound = GameController.instance.flipAudioSource;

        // Get the Animator component
        animator = GetComponent<Animator>();
    }

    // Method called when the card is clicked
    public void OnCardClicked()
    {
        if (!isAnimating && GameController.instance != null && GameController.instance.canFlip && !isFlipped)
        {
            FlipCard(true); // Flip to show front
            GameController.instance.CardFlipped(this);
        }
    }

    // Method to flip the card
    public void FlipCard(bool flipToFront)
    {
        if (isAnimating)
            return;

        isAnimating = true;

        // Set IsFlipping to true to start the animation
        animator.SetBool("IsFlipping", true);

        // Play flip sound
        if (flipSound != null)
        {
            flipSound.Play();
        }

        // Start the coroutine to switch card content
        StartCoroutine(SwitchCardContent(flipToFront));
    }

    // Coroutine to switch card content during flip animation
    private IEnumerator SwitchCardContent(bool flipToFront)
    {
        // Wait until halfway through the animation
        float animationDuration = 0.5f;
        yield return new WaitForSeconds(animationDuration / 2f);

        // Set the content visibility
        isFlipped = flipToFront;
        frontContent.SetActive(isFlipped);
        backContent.SetActive(!isFlipped);

        // Wait until the animation ends
        yield return new WaitForSeconds(animationDuration / 2f);

        // Reset IsFlipping to false
        animator.SetBool("IsFlipping", false);

        isAnimating = false;
    }

    // Method to set the card's value and display it on the front
    public void SetCardValue(string value)
    {
        cardValue = value;

        // Find the FrontText component within FrontContent
        TextMeshProUGUI frontText = frontContent.transform.Find("FrontText").GetComponent<TextMeshProUGUI>();
        if (frontText != null)
        {
            frontText.text = value;
        }
        else
        {
            Debug.LogError("FrontContent does not have a child named 'FrontText' with a TextMeshProUGUI component.");
        }
    }
}
