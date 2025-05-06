using TMPro;
using UnityEngine;

public class ARUI : MonoBehaviour
{

    public Canvas canvas; // Reference to the Canvas component
    public TMP_Text text; // Reference to the TextMeshPro component

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void displayCanvas()
    {
        // Set the canvas to be active
        canvas.gameObject.SetActive(true);
    }
    public void hideCanvas()
    {
        // Set the canvas to be inactive
        canvas.gameObject.SetActive(false);
    }
}
