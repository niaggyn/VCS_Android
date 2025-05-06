using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Functions : MonoBehaviour
{

    public Button selectButton; // Reference to the button component
    public Button infoButton; // Reference to the button component
    private bool isDragging = false;

    public bool isSelected = false; // Flag to track if the button is selected
    private bool isWaitingForSecondClick = false; // Flag to track if waiting for a second click
    private bool secondClick = false; // Flag to track if the second click is registered
    private bool isAnyButtonClicked = true; // Flag to track if any button is clicked
    public GameObject selectObjectLayerButtonIcon; // Reference to the RawImage component
    public GameObject selectObjectButtonIcon; // Reference to the RawImage component
    public GameObject rotationObjectButtonIcon; // Reference to the RawImage component
    public GameObject infoObjectButtonIcon; // Reference to the RawImage component
    public GameObject infoBoxBackground; // Reference to the RawImage component
    public GameObject zoomObjectButtonIcon; // Reference to the RawImage component
    public GameObject newScanButtonIcon; // Reference to the RawImage component
    public GameObject dropdownMenu; // Reference to the dropdown menu
    public TMP_Text infoText; // Reference to the TextMeshPro component
    public ImageTracker trackedImageHandler;
    public SelectionManager selectionManager; // Reference to the SelectionManager script
    private List<string> validTags = new List<string> { "paredes", "telhados", "pisos", "colunas", "portas", "janelas", "forros" }; // Adicione as tags válidas aqui

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(ClickSelect);
            infoButton.onClick.AddListener(infoShow);
        }
        infoText.text = "para iniciar foque a camera em uma marca"; // Reset the info text 
    }

    // Update is called once per frame
    void Update()
    {
        if (checkTrack())
        {

            if (isAnyButtonClicked)
            {
                infoText.text = "Selecione a interacao desejada";
            }
            else if (isWaitingForSecondClick)
            {
                infoText.text = "Clique no objeto para selecioná-lo.";
            }


        }
        if (isWaitingForSecondClick && secondClick && Input.GetMouseButtonDown(0)) // Detecta o segundo clique
        {
            Debug.Log("Segundo clique detectado. Verificando objeto...");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Cria um raio da câmera para a posição do mouse
            RaycastHit hit; // Variável para armazenar informações do hit

            if (Physics.Raycast(ray, out hit, 100)) // Realiza o raycast
            {
                //Debug.Log("Raycast hit! Objeto atingido: " + SelectionManager.Instance.GetSelectedObject() + ", Tag: " + SelectionManager.Instance.GetSelectedObject().tag);
                //Transform selected = hit.transform;
                GameObject hitObj = hit.transform.gameObject;
                //string selectedTag = selected.tag;

                if (validTags.Contains(hitObj.tag))
                {
                    Debug.Log("Camada selecionada: " + hitObj.name);
                    SelectionManager.Instance.SelectLayer(hitObj);
                    infoText.text = "Camada selecionada: " + hitObj.name;
                }
                else if (hitObj.tag == "construction") // Tag do objeto inteiro
                {
                    Debug.Log("Objeto inteiro selecionado: " + hitObj.name);
                    // Selecionar o objeto inteiro
                    SelectionManager.Instance.SelectWholeObject(hitObj);
                    infoText.text = "Objeto selecionado: " + hitObj.name;

                    isWaitingForSecondClick = false; // Desativa a espera para o segundo clique
                }
                else
                {
                    Debug.LogWarning("Objeto inválido. Tente novamente.");
                    infoText.text = "Clique inválido. Tente novamente.";
                }


                if (SelectionManager.Instance.IsLayerSelectionActive())
                {
                    // Seleciona uma camada
                    SelectionManager.Instance.SelectLayer(hit.transform.gameObject);
                    infoText.text = "Camada selecionada: " + hit.transform.name;
                    dropdownMenuButton(trackedImageHandler); // Chama o método para abrir o menu suspenso

                }
                else
                {
                    // Seleciona o objeto inteiro
                    SelectionManager.Instance.SelectWholeObject(hit.transform.gameObject);
                    infoText.text = "Objeto selecionado: " + hit.transform.name;
                    Debug.Log("Objeto selecionado: " + hit.transform.name);
                    activedButtonEffect(selectObjectLayerButtonIcon); // Ativa o efeito do botão de selecionar camada
                    activedButtonEffect(rotationObjectButtonIcon); // Ativa o efeito do botão de rotacionar objeto.
                    activedButtonEffect(infoObjectButtonIcon); // Ativa o efeito do botão de informações do objeto.
                    activedButtonEffect(newScanButtonIcon); // Ativa o efeito do botão de zoom.
                }
            }
            else
            {
                Debug.Log("Nenhum objeto atingido. Tente novamente.");
                infoText.text = "Nenhum objeto encontrado. Clique novamente para selecionar.";
            }

            // Retorna ao estado inicial
            isWaitingForSecondClick = false;
            secondClick = false; // Reseta o segundo clique
        }

        // Verifica se o objeto está sendo arrastado
        if (isDragging && SelectionManager.Instance.HasSelection()) // Verifica se o botão do mouse está pressionado
        {
            // Inicia a coroutine para habilitar o clique após um pequeno delay
            if (Input.GetMouseButton(0)) // Enquanto o botão esquerdo do mouse estiver pressionado
            {
                float rotationSpeed = 100f; // Velocidade de rotação
                float mouseX = Input.GetAxis("Mouse X"); // Obtém o movimento do mouse no eixo X
                float mouseY = Input.GetAxis("Mouse Y"); // Obtém o movimento do mouse no eixo Y

                GameObject selected = SelectionManager.Instance.GetSelectedObject();
                if (selected != null)
                {
                    // Rotaciona o objeto no próprio eixo local
                    selected.transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.Self); // Rotação no eixo Y
                    selected.transform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime, Space.Self); // Rotação no eixo X
                }
            }
            else if (Input.GetMouseButtonUp(0)) // Quando o botão do mouse é solto
            {

                Debug.Log("Arrasto finalizado.");
            }

        }


        // Verifica se o objeto foi instanciado
        if (trackedImageHandler.isObjectInstantiated)
        {
            // Ativa o efeito do botão de selecionar objeto inteiro
            activedButtonEffect(selectObjectButtonIcon); // Ativa o efeito do botão de selecionar objeto inteiro.
            feedBackInfo(infoBoxBackground); // Altera a cor do botão de selecionar objeto inteiro
        }

    }

    public void ClickSelect()
    {
        Debug.Log("Botão de selecionar objeto ativado!");
        infoText.text = "Clique em um objeto para selecioná-lo.";
        isWaitingForSecondClick = true;
        isAnyButtonClicked = false; // Define que nenhum botão foi clicado
        StartCoroutine(EnableClickAfterDelay()); // Inicia a coroutine para habilitar o clique após um pequeno delay
    }

    public void ClickSelectLayer()
    {
        if (SelectionManager.Instance.HasSelection())
        {
            var selectedObject = SelectionManager.Instance.GetSelectedObject();
            foreach (Transform child in selectedObject.transform)
            {
                Debug.Log("Camada: " + child.name + ", Ativa: " + child.gameObject.activeSelf);
            }
            if (selectedObject != null && HasLayerChildren(selectedObject))

            {
                Debug.Log("Modo de seleção de camada ativado. Clique em uma camada do objeto.");
                infoText.text = "Selecione uma camada do objeto.";
                SelectionManager.Instance.EnableLayerSelection(); // Ativa o modo de seleção de camada
                isWaitingForSecondClick = true;
                isAnyButtonClicked = false; // Define que nenhum botão foi clicado
                StartCoroutine(EnableClickAfterDelay()); // Inicia a coroutine para habilitar o clique após um pequeno delay
                dropdownMenuButton(trackedImageHandler); // Chama o método para abrir o menu suspenso
            }
            else
            {
                Debug.LogWarning("O objeto selecionado não possui camadas válidas.");
                infoText.text = "O objeto selecionado não possui camadas válidas.";
            }
        }
        else
        {
            Debug.LogWarning("Nenhum objeto foi selecionado. Selecione um objeto primeiro.");
            infoText.text = "Nenhum objeto foi selecionado. Clique em um objeto primeiro.";
        }
    }

    private IEnumerator EnableClickAfterDelay()
    {
        yield return new WaitForSeconds(0.2f); // pequeno delay evita clique duplicado
        secondClick = true;
    }

    public void homeButton()
    {
        Debug.Log("Home button clicked!, o cenario deve ser resetado"); // Log to the console
        clearVirtualizedObject(); // Call the method to clear the virtualized object
        infoText.text = "para iniciar foque a camera em uma marca"; // Reset the info text 
    }

    public void newScan()
    {
        Debug.Log("Prefab serao desativados!"); // Log to the console
        clearVirtualizedObject(); // Call the method to clear the virtualized object
        Debug.Log("New scan initiated!");
    }

    public void clearVirtualizedObject()
    {
        // Desativa todos os objetos desativados
        Debug.Log("Prefab deactivated! Ativando rotina do rastreador"); // Log to the console
        trackedImageHandler.OnNewScan(); // Call the method to clear the virtualized object
    }

    public bool checkTrack()
    {

        if (trackedImageHandler != null && trackedImageHandler.isObjectInstantiated)
        {
            return true; // Return true if the prefab is instantiated
        }
        else
        {
            return false; // Return false if the prefab is not instantiated
        }
    }



    public void rotateButton()
    {
        Debug.Log("Botão de rotação ativado!"); // Log to the console

        isWaitingForSecondClick = false; // Reseta o estado de espera para o segundo clique
        if (SelectionManager.Instance.HasSelection())
        {
            var selectedObject = SelectionManager.Instance.GetSelectedObject();
            if (selectedObject != null)
            {
                Debug.Log("Modo de rotação ativado!");
                infoText.text = "Arraste para rotacionar o objeto!";
                isDragging = true;
                secondClick = true; // Define que o segundo clique foi registrado
                Debug.Log("Rotacionando objeto: " + selectedObject.name);
            }
            else
            {
                Debug.LogWarning("Nenhum objeto válido foi selecionado para rotacionar!");
                infoText.text = "Nenhum objeto válido foi selecionado para rotacionar!";
            }
        }
        else
        {
            infoText.text = "Nenhum objeto selecionado para rotacionar!";
        }
        isAnyButtonClicked = false; // Define que nenhum botão foi clicado
    }

    public void infoShow()
    {
        Debug.Log("Botão de informações ativado!"); // Log to the console
        isWaitingForSecondClick = false; // Reseta o estado de espera para o segundo clique
        if (SelectionManager.Instance.HasSelection())
        {
            GameObject selectedObject = SelectionManager.Instance.GetSelectedObject();
            if (selectedObject != null)
            {
                ObjectInfo info = SelectionManager.Instance.DisplayObjectInfoReal(selectedObject);

                infoText.text = $"<b>{info.Name}</b>: " +
                                $"Dimensões: {info.Dimensions.x:F2} x {info.Dimensions.y:F2} x {info.Dimensions.z:F2} m, " +
                                $"Área: {info.Area:F2} m², " +
                                $"Volume: {info.Volume:F2} m³, " +
                                $"Escala: {info.Scale.x:F2} x {info.Scale.y:F2} x {info.Scale.z:F2} ";
            }
            else
            {
                Debug.LogWarning("Nenhum objeto válido foi selecionado para exibir informações!");
                infoText.text = "Nenhum objeto válido foi selecionado para exibir informações!";
            }
        }
        else
        {
            infoText.text = "Nenhum objeto selecionado para exibir informações!";
        }
        isAnyButtonClicked = false; // Define que nenhum botão foi clicado
    }

    private void activedButtonEffect(GameObject gameObject)
    {
        RawImage rawImage = gameObject.GetComponent<RawImage>();
        if (rawImage != null)
        {
            Color color = rawImage.color;
            color.a = 1.0f; // Define a transparência
            rawImage.color = color; // Aplica a nova cor
        }
        else
        {
            Debug.LogWarning("RawImage não encontrado no GameObject alvo!");
        }
    }

    public void feedBackInfo(GameObject gameObject)
    {
        Image rawImage = gameObject.GetComponent<Image>();
        if (rawImage == null)
        {
            Debug.LogWarning("RawImage não encontrado no GameObject alvo!");
            return; // Sai do método se o RawImage não for encontrado
        }

        Color newColor = Color.green; // Define a nova cor
        rawImage.color = newColor; // Aplica a nova cor
        Debug.Log("Cor do botão alterada para: " + newColor); // Log para depuração
    }

    public void dropdownMenuButton(ImageTracker imageTracker)
    {
        if (SelectionManager.Instance.HasSelection())
        {
            Debug.Log("Dropdown menu button clicked!");
            isAnyButtonClicked = true; // Define que um botão foi clicado

            // Alterna a visibilidade do menu suspenso
            if (dropdownMenu != null)
            {
                dropdownMenu.SetActive(!dropdownMenu.activeSelf);
                Debug.Log("Dropdown menu toggled!");
                infoText.text = "Selecione uma camada do objeto.";

                // Verifica se o objeto selecionado é válido
                GameObject selectedObject = SelectionManager.Instance.GetSelectedObject();
                if (selectedObject != null)
                {
                    Debug.Log("Exibindo camadas do objeto: " + selectedObject.name);

                    // Aqui você pode, por exemplo, preencher um TMP_Dropdown com os nomes das camadas:
                    TMP_Dropdown dropdown = dropdownMenu.GetComponentInChildren<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        dropdown.ClearOptions();
                        // Adiciona as opções de camada ao dropdown
                        List<string> layerNames = new List<string>();
                        layerNames.Add("Todas as camadas"); // Adiciona a opção "Todas as camadas" ao dropdown
                        foreach (Transform child in selectedObject.transform)
                        {
                            // Verifica se o objeto filho tem uma tag válida
                            if (child != null && validTags.Contains(child.tag))
                            {

                                layerNames.Add(child.name);
                            }

                        }
                        dropdown.AddOptions(layerNames);


                        // Adiciona um listener para detectar a seleção no dropdown
                        dropdown.onValueChanged.RemoveAllListeners(); // Remove listeners antigos
                        dropdown.onValueChanged.AddListener((index) =>
                        {
                            if (index == 0) // Se "Todas as camadas" for selecionada
                            {
                                SelectionManager.Instance.SetAllLayersActive(true); // Ativa todas as camadas
                                infoText.text = "Todas as camadas ativadas.";
                            }
                            else
                            {
                                Debug.Log("Camada selecionada no dropdown: " + layerNames[index]);
                                SelectionManager.Instance.SelectLayerByName(layerNames[index]); // Chama o método no SelectionManager
                            }

                        });
                    }
                }
                else
                {
                    Debug.LogWarning("Nenhum objeto selecionado. Clique em um objeto primeiro.");
                    infoText.text = "Nenhum objeto selecionado. Clique em um objeto primeiro.";
                }
            }
            else
            {
                Debug.LogWarning("Dropdown menu não encontrado!");
            }
        }
        else
        {
            Debug.LogWarning("Nenhum objeto selecionado. Clique em um objeto primeiro.");
            infoText.text = "Nenhum objeto selecionado. Clique em um objeto primeiro.";
        }
    }


    bool HasLayerChildren(GameObject obj)
    {
        if (obj == null) return false;

        foreach (Transform child in obj.transform)
        {
            if (validTags.Contains(child.tag))
                return true;
        }
        return false;
    }

}
