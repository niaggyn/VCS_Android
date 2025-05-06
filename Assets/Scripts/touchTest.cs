using UnityEngine;

public class touchTest : MonoBehaviour
{

    public GameObject objectTest; // Reference to the object to be instantiated

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Check if the left mouse button is pressed
        {
            Debug.Log("Mouse clicked!"); // Log to the console

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Create a ray from the camera to the mouse position
            RaycastHit hit; // Variable to store the hit information

            if (Physics.Raycast(ray, out hit, 100)) // Perform the raycast
            {
                Debug.Log("Raycast hit!"); // Log if the raycast hits something
                Debug.Log(hit.transform.name + hit.transform.tag); // Log the name and tag of the hit object

                if (hit.transform.tag == "construction") // Check if the hit object has the tag "ARObject"
                {
                    Debug.Log("Hit construction!"); // Log if the hit object is an ARObject
                }
            }


        }
    }
}
