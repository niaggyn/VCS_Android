
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;



public class ImageTracker : MonoBehaviour
{
    private ARTrackedImageManager trackedImages;
    public GameObject[] ArPrefabs;
    public bool isObjectInstantiated = false;
    private GameObject selectedObject; // Objeto atualmente selecionado
    public bool newScan = false; // Flag to track if a new scan is initiated

    public List<GameObject> ARObjects = new List<GameObject>();
    

    void Awake()
    {
        trackedImages = GetComponent<ARTrackedImageManager>();
    }

    void Update()
    {
        
    }

    void OnEnable()
    {
        //trackedImages.trackedImagesChanged += OnTrackedImagesChanged;
        if (trackedImages != null)
        {
            trackedImages.trackedImagesChanged += OnTrackedImagesChanged;
        }
    }

    void OnDisable()
    {
        //trackedImages.trackedImagesChanged -= OnTrackedImagesChanged;
        if (trackedImages != null)
        {
            trackedImages.trackedImagesChanged -= OnTrackedImagesChanged;
        }
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Handle newly added tracked images
        foreach (var trackedImage in eventArgs.added)
        {
            Debug.Log("Marcador encontrado: " + trackedImage.referenceImage.name);
            foreach (var arPrefab in ArPrefabs)
            {
                if (trackedImage.referenceImage.name == arPrefab.name)
                {
                    // Verifica se o prefab já foi instanciado
                    var existingPrefab = ARObjects.Find(obj => obj.name == arPrefab.name);
                    if (existingPrefab != null && !isObjectInstantiated)
                    {
                        // Reactiva o prefab existente
                        existingPrefab.SetActive(true);
                        existingPrefab.transform.position = trackedImage.transform.position;
                        existingPrefab.transform.rotation = trackedImage.transform.rotation;
                        Debug.Log("Prefab foi Reativado: " + existingPrefab.name);
                        isObjectInstantiated = true; // Set to true after instantiation
                    }
                    else
                    {
                        // Instancia um novo prefab se não existir
                        var newPrefab = Instantiate(arPrefab, trackedImage.transform);
                        newPrefab.SetActive(true); // Ativa o prefab
                        newPrefab.name = arPrefab.name; // Garante que o nome corresponda à imagem de referência
                        ARObjects.Add(newPrefab);
                        isObjectInstantiated = true; // Set to true after instantiation
                        Debug.Log("Prefab Instanciado: " + arPrefab.name);
                    }

                }
            }
        }

        // Handle updated tracked images
        foreach (var trackedImage in eventArgs.updated)
        {
            foreach (var gameObject in ARObjects)
            {
                if (gameObject.name == trackedImage.referenceImage.name)
                {
                    //camera no marcador
                    if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                    {
                        //Debug.Log("Rastreamento ativo: " + trackedImage.referenceImage.name);
                        // Reactiva o prefab se ele estiver desativado
                        if (newScan && !isObjectInstantiated)
                        {
                            // Se newScan não estiver ativo, ativa o objeto
                            Debug.Log("Rastreamento ativo e newScan ativo. Ativando objeto.");
                            gameObject.SetActive(true);
                            Debug.Log("Prefab Reativado: " + gameObject.name);
                            isObjectInstantiated = true;
                            newScan = false; // Reset newScan flag after reactivation
                        }

                    }
                    else
                    {
                        Debug.Log("camera fora do marcador");
                        if (newScan && gameObject.activeSelf)
                        {
                            // Se o rastreamento não estiver ativo e newScan estiver ativo, ativa o objeto
                            Debug.Log("newScan ativo, marcador fora da camera. Desativando objeto.");
                            gameObject.SetActive(false);
                            gameObject.transform.position = trackedImage.transform.position;
                            gameObject.transform.rotation = trackedImage.transform.rotation;
                            Debug.Log("Prefab Desativado: " + gameObject.name);
                            isObjectInstantiated = false; // Set to true after instantiation
                        }
                        else
                        {
                            Debug.Log("Rastreamento ativo e camera fora do marcador: " + trackedImage.referenceImage.name);
                            if (newScan && gameObject.activeSelf)
                            {
                                // Se o rastreamento não estiver ativo e newScan estiver ativo, ativa o objeto
                                Debug.Log("newScan ativo, marcador fora da camera. Desativando objeto.");
                                gameObject.SetActive(false);
                                gameObject.transform.position = trackedImage.transform.position;
                                gameObject.transform.rotation = trackedImage.transform.rotation;
                                Debug.Log("Prefab Desativado: " + gameObject.name);
                                isObjectInstantiated = false; // Set to true after instantiation
                            }
                        }
                    }
                }
            }
        }

        // Handle removed tracked images
        foreach (var trackedImage in eventArgs.removed)
        {
            foreach (var gameObject in ARObjects)
            {
                if (gameObject.name == trackedImage.referenceImage.name)
                {
                    // Desativa o prefab se o rastreamento for removido
                    if (gameObject.activeSelf)
                    {
                        gameObject.SetActive(false);
                        Debug.Log("Prefab Desativado (removed): " + gameObject.name);
                    }
                }
            }
        }
    }

    public void InstantiateObject()
    {
        if (!isObjectInstantiated)
        {
            isObjectInstantiated = true; // Set to true after instantiation
        }
    }

    public void OnNewScan()
    {
        Debug.Log("Novo scan solicitado.");
        foreach (var obj in ARObjects)
        {
            obj.SetActive(false);
            Debug.Log("Prefab Desativado: " + obj.name);
        }
        isObjectInstantiated = false;
        newScan = true;
    }

    // Método para definir o objeto selecionado
    public void SetSelectedObject(GameObject obj)
    {
        selectedObject = obj;
    }

    // Método para obter o objeto selecionado
    public GameObject GetSelectedObject()
    {
        return selectedObject; // Retorna o objeto atualmente selecionado
    }
}