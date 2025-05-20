using UnityEngine;
using TMPro; // Importing TextMeshPro namespace for text rendering
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;


public class ButtonAction : MonoBehaviour
{
    public ARSessionController arSessionController; // Reference to the ARSessionController script


    public TMP_Text text;
    public Image image; // Reference to the Image component
    public RawImage rawImage; // Reference to the RawImage component

    public Button close; // Reference to the Button component
    public Button triggerButton; // Reference to the Button component
    public Canvas canvas; // Reference to the Canvas component
    //public ImageTracker trackedImageHandler; // Reference to the TrackedImageHandler script
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        

        // Hide the button initially
        close.gameObject.SetActive(false);
        canvas.gameObject.SetActive(false); // Set the canvas to be inactive
        
        // Add a listener to the trigger button to show this button
        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(ShowButton);
        }

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void clickButton()
    {
        Debug.Log("Botao Iniciar Clicado.Desativar canvas inicial!");

        // Verifica se o ARSessionController está atribuído
        if (arSessionController == null)
        {
            Debug.LogError("ARSessionController não está atribuído!");
            return;
        }

        // Inicia o XR
        arSessionController.StartAR();
        
        image.gameObject.SetActive(false); // Set the image to be active
        this.gameObject.SetActive(false); // Set the button to be inactive
        rawImage.gameObject.SetActive(false); // Set the raw image to be active
        canvas.gameObject.SetActive(true); // Set the canvas to be active
        text.gameObject.SetActive(false); // Hide the text

    }
    public void ShowButton()
    {
        Debug.Log("Botao Iniciar Clicado.Exibindo o canvas de funcoes!");
        close.gameObject.SetActive(true); // Show the button
        triggerButton.gameObject.SetActive(false); // Set the button to be active
    }

    public void closeButton()
    {
        Debug.Log("Close button clicked!");
        image.gameObject.SetActive(true); // Set the image to be active
        close.gameObject.SetActive(false); // Set the button to be inactive
        triggerButton.gameObject.SetActive(true); // Show the trigger button again
        text.gameObject.SetActive(true); // Show the text again
        rawImage.gameObject.SetActive(true); // Set the raw image to be active
        canvas.gameObject.SetActive(false); // Set the canvas to be inactive
    }

}
