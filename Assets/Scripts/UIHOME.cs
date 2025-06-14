using UnityEngine;
using TMPro; // Importing TextMeshPro namespace for text rendering
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;


public class ButtonAction : MonoBehaviour
{

    public ARSessionController arSessionController; // Reference to the ARSessionController script

    // References to UI components
    public GameObject UIHOME; // Reference to the UIHOME GameObject
    public TMP_Text text;
    public Image image; // Reference to the Image component
    public RawImage rawImage; // Reference to the RawImage component
    public bool isAppStarted = false; // Flag to check if the app has started
    public Button returnHome; // Reference to the Button component
    public Button triggerButton; // Reference to the Button component
    public Canvas UIFuncoes; // Reference to the Canvas component

    //public ImageTracker trackedImageHandler; // Reference to the TrackedImageHandler script
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (UIHOME != null)
        {
            AtivarUI(UIHOME); // Ensure UIHOME is set to the current GameObject if not assigned
            
        }

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void iniciarApp()
    {
        Debug.Log("Botao Iniciar Clicado.Desativar canvas inicial!");
        // Verifica se o ARSessionController está atribuído
        if (arSessionController == null)
        {
            Debug.LogError("ARSessionController não está atribuído!");
            return;
        }
        else
        {
            Debug.Log("ARSessionController atribuído com sucesso!");
            image.gameObject.SetActive(false); // Set the image to be active
            rawImage.gameObject.SetActive(false); // Set the raw image to be active
            if(UIFuncoes != null)
            {
                UIFuncoes.gameObject.SetActive(true); // Activate the UIFuncoes GameObject
            }
            else
            {
                Debug.LogError("UIFuncoes não está atribuído!");
            }
            text.gameObject.SetActive(false); // Hide the text
            triggerButton.gameObject.SetActive(false); // Set the button to be inactive
            isAppStarted = true; // Set the flag to true indicating the app has started
            arSessionController.StartAR();
        }
        Debug.Log("Botao Iniciar Clicado.Desativar canvas inicial!");
    }
    public void ShowButton()
    {
        Debug.Log("Botao Iniciar Clicado.Exibindo o canvas de funcoes!");
        //close.gameObject.SetActive(true); // Show the button
        triggerButton.gameObject.SetActive(false); // Set the button to be active
    }

    public void homeReturnButton()
    {
        /*
        Debug.Log("Close button clicked!");
        image.gameObject.SetActive(true); // Set the image to be active
        returnHome.gameObject.SetActive(false); // Set the button to be inactive
        triggerButton.gameObject.SetActive(true); // Show the trigger button again
        text.gameObject.SetActive(true); // Show the text again
        rawImage.gameObject.SetActive(true); // Set the raw image to be active
        canvas.gameObject.SetActive(false); // Set the canvas to be inactive
        */
        AtivarUI(UIHOME); // Activate the UIHOME GameObject
        UIFuncoes.gameObject.SetActive(false); // Deactivate the UIFuncoes GameObject
        arSessionController.StartAR(); // Restart the AR session
    }

    public void AtivarUI(GameObject pai)
    {
        foreach (Transform child in pai.transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    public void DesativarUI(GameObject pai)
    {
        foreach (Transform child in pai.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

}
