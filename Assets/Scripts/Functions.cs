using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Functions : MonoBehaviour
{

    public ARSessionController arSessionController; // Reference to the ARSessionController script

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
    public GameObject scannerAnimation; // Reference to the scanner animation
    public GameObject resertObjectButtonIcon; // Reference to the RawImage component
    public TMP_Text infoText; // Reference to the TextMeshPro component
    public ImageTracker trackedImageHandler;
    public SelectionManager selectionManager; // Reference to the SelectionManager script
    private List<string> validTags = new List<string> { "paredes", "telhados", "pisos", "colunas", "portas", "janelas", "forros" }; // Adicione as tags válidas aqui

    private Vector3 originalPosition; // Variável para armazenar a posição original do Objeto
    private Quaternion originalRotation; // Variável para armazenar a rotação original do Objeto
    private Vector3 originalScale;
    private bool isOriginalPositionStored = false; // Flag para verificar se a posição original já foi armazenada


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(ClickSelect);
            infoButton.onClick.AddListener(infoShow);
        }
        infoText.text = "para iniciar foque a camera em uma marca";
        DeactivateAllButtonsExceptHome();
    }

    // Update is called once per frame
    void Update()
    {
        // Situação: Objeto não foi iniciado
        if (!checkTrack())
        {
            infoText.text = "Para iniciar foque a camera em um MARCADOR";
            DeactivateAllButtonsExceptHome();
        }
        else
        {
            // Quando o Objeto é instanciado e nenhum Objeto está selecionado
            if (trackedImageHandler.isObjectInstantiated && !SelectionManager.Instance.HasSelection())
            {
                ActivateScanAndSelectButtons();
                // Mensagem ao instanciar Objeto
                GameObject obj = selectionManager.GetInstantiatedObject();
                if (obj != null)
                    infoText.text = "Objeto encontrado: " + obj.tag;
                else
                    infoText.text = "Objeto encontrado";
            }
            // Quando o Objeto está selecionado
            else if (trackedImageHandler.isObjectInstantiated && SelectionManager.Instance.HasSelection())
            {
                ActivateObjectInteractionButtons();
                GameObject obj = SelectionManager.Instance.GetSelectedObject();
                if (obj != null)
                    infoText.text = "Objeto selecionado: " + obj.name;
                else
                    infoText.text = "Objeto selecionado";
            }
        }

        if (isWaitingForSecondClick && secondClick && Input.GetMouseButtonDown(0))
        {
            Debug.Log("Segundo clique detectado. Verificando Objeto...");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Cria um raio da câmera para a posição do mouse
            RaycastHit hit; // Variável para armazenar informações do hit

            if (Physics.Raycast(ray, out hit, 100))
            {
                GameObject hitObj = hit.transform.gameObject;
                if (validTags.Contains(hitObj.tag))
                {
                    SelectionManager.Instance.SelectLayer(hitObj);
                    infoText.text = "Camada selecionada: " + hitObj.name;
                }
                else if (hitObj.tag == "construction")
                {
                    SelectionManager.Instance.SelectWholeObject(hitObj);
                    infoText.text = "Objeto selecionado: " + hitObj.name;
                    isWaitingForSecondClick = false;
                }
                else
                {
                    infoText.text = "Clique inválido. Tente novamente.";
                }
                // ...existing code...
                if (SelectionManager.Instance.IsLayerSelectionActive())
                {
                    SelectionManager.Instance.SelectLayer(hit.transform.gameObject);
                    infoText.text = "Camada selecionada: " + hit.transform.name;
                    dropdownMenuButton(trackedImageHandler);
                }
                else
                {
                    SelectionManager.Instance.SelectWholeObject(hit.transform.gameObject);
                    StoreOriginalPosition(hitObj);
                    infoText.text = "Objeto selecionado: " + hit.transform.name;
                    Debug.Log("Objeto selecionado: " + hit.transform.name);
                }
            }
            else
            {
                infoText.text = "Nenhum Objeto encontrado. Clique novamente para selecionar.";
            }
            isWaitingForSecondClick = false;
            secondClick = false;
        }

        // Verifica se o Objeto está sendo arrastado
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


                    // Rotaciona o Objeto no próprio eixo local
                    selected.transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
                    //selected.transform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime, Space.Self); // Rotação no eixo X

                    resertObjectButtonIcon.gameObject.SetActive(true); // Ativa o botão de resetar Objeto

                }
            }
            else if (Input.GetMouseButtonUp(0)) // Quando o botão do mouse é solto
            {

                Debug.Log("Arrasto finalizado.");
            }

        }


        // Verifica se o Objeto foi instanciado
        if (trackedImageHandler.isObjectInstantiated && !SelectionManager.Instance.HasSelection())
        {
            // Ativa o efeito do botão de selecionar Objeto inteiro
            selectObjectButtonIcon.SetActive(true); // Ativa o efeito do botão de selecionar Objeto inteiro
            activeButtonsFunctions(newScanButtonIcon); // Ativa o efeito do botão de zoom.
            scannerAnimation.SetActive(false); // Ativa a animação do scanner
            feedBackInfo(infoBoxBackground, Color.green); // Altera a cor do botão de selecionar Objeto inteiro
            infoText.color = Color.black; // Altera a cor do texto de informações

        }
        else if (trackedImageHandler.isObjectInstantiated && SelectionManager.Instance.HasSelection())
        {
            // Ativa o efeito do botão de selecionar Objeto inteiro
            //selectObjectButtonIcon.SetActive(true); // Ativa o efeito do botão de selecionar Objeto inteiro
            feedBackInfo(infoBoxBackground, Color.blue); // Altera a cor do botão de selecionar Objeto inteiro
            infoText.color = Color.white; // Altera a cor do texto de informações
        }
        if (!trackedImageHandler.isObjectInstantiated && !SelectionManager.Instance.HasSelection())
        {
            // Desativa o efeito do botão de selecionar Objeto inteiro
            feedBackInfo(infoBoxBackground, Color.black); // Altera a cor do botão de selecionar Objeto inteiro
            infoText.color = Color.white; // Altera a cor do texto de informações
        }

    }

    public void ClickSelect()
    {
        Debug.Log("Botão de selecionar Objeto ativado!");
        infoText.text = "Clique em um Objeto para selecioná-lo.";
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
                Debug.Log("Modo de seleção de camada ativado. Clique em uma camada do Objeto.");
                infoText.text = "Selecione uma camada do Objeto.";
                SelectionManager.Instance.EnableLayerSelection(); // Ativa o modo de seleção de camada
                isWaitingForSecondClick = true;
                isAnyButtonClicked = false; // Define que nenhum botão foi clicado
                StartCoroutine(EnableClickAfterDelay()); // Inicia a coroutine para habilitar o clique após um pequeno delay
                dropdownMenuButton(trackedImageHandler); // Chama o método para abrir o menu suspenso
            }
            else
            {
                Debug.LogWarning("O Objeto selecionado não possui camadas válidas.");
                infoText.text = "O Objeto selecionado não possui camadas válidas.";
            }
        }
        else
        {
            Debug.LogWarning("Nenhum Objeto foi selecionado. Selecione um Objeto primeiro.");
            infoText.text = "Nenhum Objeto foi selecionado. Clique em um Objeto primeiro.";
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
        arSessionController.StartAR(); // Stop the AR session
        disableALlButtonEffect(); // Call the method to disable all button effects
        infoText.text = "para iniciar foque a camera em uma marca"; // Reset the info text 
    }

    public void newScan()
    {
        Debug.Log("Prefab serao desativados!"); // Log to the console
        if (trackedImageHandler.isObjectInstantiated)
        {
            clearVirtualizedObject(); // Call the method to clear the virtualized object
            DeactivateAllButtonsExceptHome(); // Desativa todos os botões menos o home
            ResetAndHideDropdownMenu(); // Reseta e desativa o dropdown menu
            infoText.text = "Para iniciar foque a camera em um MARCADOR";
            Debug.Log("New scan initiated!");
        }
        else if (trackedImageHandler.isObjectInstantiated && SelectionManager.Instance.HasSelection())
        {
            clearVirtualizedObject(); // Call the method to clear the virtualized object
            DeactivateAllButtonsExceptHome(); // Desativa todos os botões menos o home
            ResetAndHideDropdownMenu(); // Reseta e desativa o dropdown menu
            infoText.text = "Para iniciar foque a camera em um MARCADOR";
            Debug.Log("New scan initiated!");
        }
        else
        {
            Debug.LogWarning("Nenhum Objeto instanciado. Não é possível iniciar um novo scan.");
            infoText.text = "Nenhum Objeto instanciado. Não é possível iniciar um novo scan.";
        }
    }

    public void clearVirtualizedObject()
    {
        // Desativa todos os Objetos desativados
        Debug.Log("Prefab deactivated! Ativando rotina do rastreador"); // Log to the console
        trackedImageHandler.OnNewScan(); // Call the method to clear the virtualized object
        dropdownMenu.SetActive(false); // Desativa o menu suspenso
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
                infoText.text = "Arraste para rotacionar o Objeto!";
                isDragging = true;
                secondClick = true; // Define que o segundo clique foi registrado
                Debug.Log("Rotacionando Objeto: " + selectedObject.name);
            }
            else
            {
                Debug.LogWarning("Nenhum Objeto válido foi selecionado para rotacionar!");
                infoText.text = "Nenhum Objeto válido foi selecionado para rotacionar!";
            }
        }
        else
        {
            infoText.text = "Nenhum Objeto selecionado para rotacionar!";
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
                infoText.text = "Nenhum Objeto válido foi selecionado para exibir informações!";
            }
        }
        else
        {
            infoText.text = "Nenhum Objeto selecionado para exibir informações!";
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



    public void feedBackInfo(GameObject gameObject, Color newCor)
    {
        //Color color = gameObject.GetComponent<Image>().color;


        Color cor = newCor; // Define a cor verde

        gameObject.GetComponent<Image>().color = cor; // Aplica a nova cor

        //Debug.Log("Cor do botão alterada para: " + cor); // Log para depuração
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
                infoText.text = "Selecione uma camada do Objeto.";

                // Verifica se o Objeto selecionado é válido
                GameObject selectedObject = SelectionManager.Instance.GetSelectedObject();
                if (selectedObject != null)
                {
                    Debug.Log("Exibindo camadas do Objeto: " + selectedObject.name);

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
                            // Verifica se o Objeto filho tem uma tag válida
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
                                infoText.text = "Camada selecionada: " + layerNames[index];
                            }

                        });
                    }
                }
                else
                {
                    Debug.LogWarning("Nenhum Objeto selecionado. Clique em um Objeto primeiro.");
                    infoText.text = "Nenhum Objeto selecionado. Clique em um Objeto primeiro.";
                }
            }
            else
            {
                Debug.LogWarning("Dropdown menu não encontrado!");
            }
        }
        else
        {
            Debug.LogWarning("Nenhum Objeto selecionado. Clique em um Objeto primeiro.");
            infoText.text = "Nenhum Objeto selecionado. Clique em um Objeto primeiro.";
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

    public void ResetObject()
    {
        Debug.Log("Resetando Objeto selecionado para a posição original!");
        GameObject selected = SelectionManager.Instance.GetSelectedObject();
        if (selected != null)
        {
            if (isOriginalPositionStored)
            {
                selected.transform.position = originalPosition;
                selected.transform.rotation = originalRotation;
                selected.transform.localScale = originalScale;
                Debug.Log("Objeto resetado para a posição original: " + originalPosition + ", Rotação: " + originalRotation + ", Escala: " + originalScale);
            }
            else
            {
                Debug.LogWarning("A posição original do Objeto não foi armazenada.");
            }
        }
        else
        {
            Debug.LogWarning("Nenhum Objeto está selecionado para resetar.");
        }
    }

    public void StoreOriginalPosition(GameObject selected)
    {
        if (!isOriginalPositionStored && selectionManager.HasSelection())
        {
            originalPosition = selected.transform.position;
            originalRotation = selected.transform.rotation;
            originalScale = selected.transform.localScale;
            isOriginalPositionStored = true; // Marca que a posição original foi armazenada
            Debug.Log("Posição original armazenada: " + originalPosition + ", Rotação: " + originalRotation + ", Escala: " + originalScale);
        }
    }

    public void disableALlButtonEffect()
    {
        // Desativa todos os efeitos dos botões
        Color color = Color.white; // Define a cor padrão (branca)
        selectObjectLayerButtonIcon.GetComponent<RawImage>().color = color;
        selectObjectButtonIcon.GetComponent<RawImage>().color = color;
        rotationObjectButtonIcon.GetComponent<RawImage>().color = color;
        infoObjectButtonIcon.GetComponent<RawImage>().color = color;
        zoomObjectButtonIcon.GetComponent<RawImage>().color = color;
        newScanButtonIcon.GetComponent<RawImage>().color = color;
    }

    private void activeButtonsFunctions(GameObject gameObject)
    {
        if (trackedImageHandler.isObjectInstantiated)
        {
            foreach (Transform child in gameObject.transform)
            {
                // Ativa os botões de interação
                gameObject.SetActive(true); // Ativa o efeito do botão de selecionar camada
                activedButtonEffect(gameObject); // Ativa o efeito do botão de selecionar camada

            }
            Debug.Log("Botões de interação ativados!"); // Log para depuração
        }
        else
        {
            Debug.LogWarning("Nenhum Objeto instanciado. Botões de interação não ativados.");
        }

    }

    private void inactiveButtonsFunctions()
    {
        // Desativa os botões de interação
        selectObjectLayerButtonIcon.SetActive(false); // Desativa o efeito do botão de selecionar camada
        rotationObjectButtonIcon.SetActive(false); // Desativa o efeito do botão de rotacionar Objeto.
        infoObjectButtonIcon.SetActive(false); // Desativa o efeito do botão de informações do Objeto.
        zoomObjectButtonIcon.SetActive(false); // Desativa o efeito do botão de zoom.
        Debug.Log("Botões de interação desativados!"); // Log para depuração
    }

    // === NOVOS MÉTODOS AUXILIARES ===

    private void DeactivateAllButtonsExceptHome()
    {
        // Desativa todos os botões de interação, exceto o botão home (assumindo que o botão home não está nesta lista)
        selectObjectLayerButtonIcon.SetActive(false);
        selectObjectButtonIcon.SetActive(false);
        rotationObjectButtonIcon.SetActive(false);
        infoObjectButtonIcon.SetActive(false);
        zoomObjectButtonIcon.SetActive(false);
        newScanButtonIcon.SetActive(false);
        resertObjectButtonIcon.SetActive(false);
        if (!trackedImageHandler.isObjectInstantiated) selectButton.gameObject.SetActive(false);
        if (infoButton != null) infoButton.gameObject.SetActive(false);
        Debug.Log("Todos os botões desativados, exceto o home!"); // Log para depuração
        // O botão home deve ser mantido ativo (não listado aqui)
    }

    private void ActivateScanAndSelectButtons()
    {
        // Ativa apenas os botões de novo scan e selecionar Objeto
        newScanButtonIcon.SetActive(true);
        if (selectButton != null) selectButton.gameObject.SetActive(true);
        // Desativa os outros
        selectObjectLayerButtonIcon.SetActive(false);
        rotationObjectButtonIcon.SetActive(false);
        infoObjectButtonIcon.SetActive(false);
        zoomObjectButtonIcon.SetActive(false);
        if (infoButton != null) infoButton.gameObject.SetActive(false);
        Debug.Log("Botões de novo scan e selecionar Objeto ativados!"); // Log para depuração
    }

    private void ActivateObjectInteractionButtons()
    {
        // Ativa os botões de interação do Objeto após seleção
        selectObjectLayerButtonIcon.SetActive(true);
        rotationObjectButtonIcon.SetActive(true);
        infoObjectButtonIcon.SetActive(true);
        // Os outros permanecem conforme necessário
        newScanButtonIcon.SetActive(true);
        if (selectButton != null) selectButton.gameObject.SetActive(true);
        if (infoButton != null) infoButton.gameObject.SetActive(true);
        Debug.Log("Botões de interação do Objeto ativados!"); // Log para depuração
    }

    private void ResetAndHideDropdownMenu()
    {
        if (dropdownMenu != null)
        {
            TMP_Dropdown dropdown = dropdownMenu.GetComponentInChildren<TMP_Dropdown>();
            if (dropdown != null)
            {
                dropdown.ClearOptions();
            }
            dropdownMenu.SetActive(false);
        }
        Debug.Log("Dropdown menu resetado e oculto!"); // Log para depuração
    }
}








