using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


[System.Serializable]
public class ARModelMapping
{
    // O nome da imagem no seu AR Reference Image Library
    public string referenceImageName;

    // O Prefab 3D que será instanciado para esta imagem
    public GameObject prefabToInstantiate;

    // O Scriptable Object com as informações detalhadas para este modelo
    public ObjectInfoData objectInfo;
}

public class ImageTracker : MonoBehaviour
{
    private ARTrackedImageManager trackedImages;
    public List<ARModelMapping> arModelMappings; // Lista de mapeamentos entre imagens e prefabs
    private Dictionary<string, ARModelMapping> mappingDictionary = new Dictionary<string, ARModelMapping>();
    public bool isObjectInstantiated = false;
    private GameObject selectedObject; // Objeto atualmente selecionado
    public bool newScan = false; // Flag to track if a new scan is initiated
    public List<GameObject> ARObjects = new List<GameObject>();
    private Dictionary<GameObject, string> instanceIdMap = new Dictionary<GameObject, string>();


    void Awake()
    {
        trackedImages = GetComponent<ARTrackedImageManager>();
        foreach (var mapping in arModelMappings)
        {
            if (mappingDictionary.ContainsKey(mapping.referenceImageName))
            {
                Debug.LogWarning($"[ImageTracker] Mapeamento duplicado para imagem de referência: '{mapping.referenceImageName}'. Ignorando duplicata.");
                continue;
            }
            mappingDictionary.Add(mapping.referenceImageName, mapping);
        }
        Debug.Log($"[ImageTracker] Dicionário de mapeamentos carregado. Total: {mappingDictionary.Count} entradas.");
    }

    void Update()
    {

    }

    void OnEnable()
    {
        if (trackedImages != null)
        {
            trackedImages.trackedImagesChanged += OnTrackedImagesChanged;
            Debug.Log("[ImageTracker] Assinado ao evento trackedImagesChanged.");
        }
        else
        {
            Debug.LogError("[ImageTracker] ARTrackedImageManager não encontrado. Certifique-se de que este script está no mesmo GameObject ou referenciado corretamente.");
        }
    }

    void OnDisable()
    {
        if (trackedImages != null)
        {
            trackedImages.trackedImagesChanged -= OnTrackedImagesChanged;
            Debug.Log("[ImageTracker] Desassinado do evento trackedImagesChanged.");
        }
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // --- Imagens Adicionadas (Nova detecção de marcador) ---
        foreach (var trackedImage in eventArgs.added)
        {
            Debug.Log($"[ImageTracker] Marcador ADICIONADO: {trackedImage.referenceImage.name}. Estado: {trackedImage.trackingState}");
            HandleTrackedImageAddedOrUpdated(trackedImage);
        }

        // --- Imagens Atualizadas (Marcador já detectado, estado mudou/posição atualizada) ---
        foreach (var trackedImage in eventArgs.updated)
        {
            Debug.Log($"[ImageTracker] Marcador ATUALIZADO: {trackedImage.referenceImage.name}. Estado: {trackedImage.trackingState}");
            HandleTrackedImageAddedOrUpdated(trackedImage); // Reutiliza a lógica para consistência
        }

        // --- Imagens Removidas (Marcador fora da câmera/perdido) ---
        foreach (var trackedImage in eventArgs.removed)
        {
            Debug.Log($"[ImageTracker] Marcador REMOVIDO: {trackedImage.referenceImage.name}. Estado: {trackedImage.trackingState}");
            //HandleTrackedImageRemoved(trackedImage);
        }
    }

    // NOVO: Método auxiliar para lidar com adição/atualização de imagens
    private void HandleTrackedImageAddedOrUpdated(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        Debug.Log($"[ImageTracker] Processando imagem: {imageName} com estado {trackedImage.trackingState}");
        // Tentar obter o mapeamento para esta imagem
        if (mappingDictionary.TryGetValue(imageName, out ARModelMapping currentMapping))
        {
            GameObject instantiatedModel = ARObjects.Find(obj => obj != null && obj.name == currentMapping.prefabToInstantiate.name);
            Debug.Log($"[ImageTracker] Mapeamento encontrado para '{imageName}': Prefab = {currentMapping.prefabToInstantiate.name}, ObjectID = {currentMapping.objectInfo?.objectID}");
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (instantiatedModel == null)
                {
                    // Instanciar se não existe
                    instantiatedModel = Instantiate(currentMapping.prefabToInstantiate, trackedImage.transform);
                    instantiatedModel.name = currentMapping.prefabToInstantiate.name; // Garante que o nome seja o do prefab original
                    ARObjects.Add(instantiatedModel);
                    instanceIdMap[instantiatedModel] = currentMapping.objectInfo?.objectID; // Armazena a ID do objeto
                    instantiatedModel.SetActive(true); // Ativa o objeto instanciado
                    Debug.Log($"[ImageTracker] Prefab '{instantiatedModel.name}' INSTANCIADO e ID '{currentMapping.objectInfo?.objectID}' associada.");
                }
                else if (!instantiatedModel.activeSelf)
                {
                    // Reativar se existe mas está desativado
                    instantiatedModel.SetActive(true);
                    Debug.Log($"[ImageTracker] Prefab '{instantiatedModel.name}' REATIVADO.");
                }
                Debug.Log($"Instanciado em pos: {instantiatedModel.transform.position}, local: {instantiatedModel.transform.localPosition}, escala: {instantiatedModel.transform.localScale}, ativo: {instantiatedModel.activeSelf}");
                // Atualiza posição e rotação para o marcador (faça isso sempre que estiver rastreando)
                //instantiatedModel.transform.position = trackedImage.transform.position;
                //instantiatedModel.transform.rotation = trackedImage.transform.rotation;
                isObjectInstantiated = true; // Define que pelo menos um objeto está instanciado/ativo
                SetSelectedObject(instantiatedModel); // Opcional: Define o objeto rastreado como o 'selectedObject'
            }
            else // TrackingState.Limited ou TrackingState.None (marcador fora da câmera ou rastreamento ruim)
            {
                if (instantiatedModel != null && instantiatedModel.activeSelf)
                {
                    // Se o rastreamento é limitado/perdido, desativar o objeto
                    //instantiatedModel.SetActive(false);
                    Debug.Log($"[ImageTracker] Prefab '{instantiatedModel.name}' DESATIVADO (rastreamento perdido).");
                    //isObjectInstantiated = false; // Se todos os objetos estiverem inativos, isso deve ser false.
                    // Você pode precisar de uma lógica mais sofisticada se tiver vários objetos.
                    // Ex: verificar se ARObjects.Any(obj => obj.activeSelf)
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ImageTracker] Nenhum mapeamento encontrado para a imagem de referência: '{imageName}'. Certifique-se de que está na lista 'AR Model Mappings'.");
        }
    }

    // NOVO: Método auxiliar para lidar com imagens removidas
    private void HandleTrackedImageRemoved(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        if (mappingDictionary.TryGetValue(imageName, out ARModelMapping currentMapping))
        {
            GameObject instantiatedModel = ARObjects.Find(obj => obj != null && obj.name == currentMapping.prefabToInstantiate.name);
            if (instantiatedModel != null && instantiatedModel.activeSelf)
            {
                instantiatedModel.SetActive(false);
                Debug.Log($"[ImageTracker] Prefab '{instantiatedModel.name}' DESATIVADO (marcador removido).");
                // Verificar se ainda há outros objetos ativos antes de definir isObjectInstantiated para false
                isObjectInstantiated = ARObjects.Exists(obj => obj != null && obj.activeSelf);
            }
        }
    }

    // Seu método GetInstantiatedObject() pode precisar de ajustes se você tiver vários objetos ativos.
    // Atualmente, ele retorna o primeiro objeto ativo que encontrar.
    public GameObject GetInstantiatedObject()
    {
        foreach (var kvp in instanceIdMap)
        {
            if (kvp.Key != null && kvp.Key.activeSelf)
                return kvp.Key;
        }
        return null;

    }

    // NOVO: Método para obter a ID do ObjectInfoData de um GameObject instanciado
    public string GetObjectIdForInstantiatedObject(GameObject instantiatedGameObject)
    {
        if (instantiatedGameObject != null && instanceIdMap.ContainsKey(instantiatedGameObject))
        {
            return instanceIdMap[instantiatedGameObject];
        }
        return null; // Retorna null se a ID não for encontrada
    }

    // Define o objeto atualmente selecionado
    public void SetSelectedObject(GameObject obj)
    {
        selectedObject = obj;
    }

    // Retorna o objeto atualmente selecionado
    public GameObject GetSelectedObject()
    {
        return selectedObject;
    }

    // ... (restante dos seus métodos como InstantiateObject, OnNewScan, SetSelectedObject, GetSelectedObject) ...

    public void OnNewScan()
    {
        Debug.Log("[ImageTracker] Novo scan solicitado. Desativando todos os objetos AR.");
        foreach (var obj in ARObjects)
        {
            if (obj != null && obj.activeSelf) // Só desativa se estiver ativo
            {
                obj.SetActive(false);
                Debug.Log($"[ImageTracker] Prefab desativado no new scan: {obj.name}");
            }
        }
        isObjectInstantiated = false;
        newScan = true; // Esta flag pode ser reavaliada se você usa os estados de Functions.cs
    }

    

}