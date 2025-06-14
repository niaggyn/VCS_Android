using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using ZXing;
using ZXing.QrCode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading.Tasks;
using Unity.Collections;
using Cysharp.Threading.Tasks;


public class Functions : MonoBehaviour
{

    public ARSessionController arSessionController; // Reference to the ARSessionController script
    public ARCameraManager arCameraManager; // Reference to the ARCameraManager script
    public Button selectButton; // Reference to the button component
    public Button infoButton; // Reference to the button component
    public Button elevateUpButton; // Reference to the button component
    public Button elevateDownButton; // Reference to the button component
    public Button homeReturnButton; // Reference to the button component
    public float elevationStep = 200f; // Step size for elevation adjustment
    private bool isDragging = false;
    private bool isReadingQRCode = false; // Flag to prevent multiple reads
    private bool canDragToRotate = false;
    public bool isSelected = false; // Flag to track if the button is selected
    private string qrCodeTextCache = null; // Cache for the QR code text to avoid re-reading
    private bool isWaitingForSecondClick = false; // Flag to track if waiting for a second click
    //private bool secondClick = false; // Flag to track if the second click is registered
    public GameObject selectObjectLayerButtonIcon; // Reference to the RawImage component
    public GameObject selectObjectButtonIcon; // Reference to the RawImage component

    public GameObject rotationObjectButtonIcon; // Reference to the RawImage component
    public GameObject infoObjectButtonIcon; // Reference to the RawImage component
    public GameObject infoBoxBackground; // Reference to the RawImage component
    public GameObject elevationButtonIcon; // Reference to the RawImage component
    public GameObject newScanButtonIcon; // Reference to the RawImage component
    public GameObject dropdownMenu; // Reference to the dropdown menu
    public GameObject elevationActions; // Reference to the RawImage component
    public GameObject scannerAnimation; // Reference to the scanner animation
    public GameObject resertObjectButtonIcon; // Reference to the RawImage component
    public GameObject textInfoTechnical; // Reference to the TextMeshPro component
    public TMP_Text infoText; // Reference to the TextMeshPro component
    public TMP_Text tecnicalTextOfObject; // Reference to the TextMeshPro component
    public ImageTracker trackedImageHandler;
    public GameObject UIHome; // Reference to the UIHOME GameObject
    public SelectionManager selectionManager; // Reference to the SelectionManager script
    private List<string> validTags = new List<string> { "paredes", "telhados", "pisos", "colunas",
        "portas", "janelas", "forros", "escadas", "mobilia", "paisagismo", "eletrico", "estrutura" }; // Adicione as tags válidas aqui

    private Vector3 originalPosition; // Variável para armazenar a posição original do Objeto
    private Quaternion originalRotation; // Variável para armazenar a rotação original do Objeto
    private Vector3 originalScale;
    private UIPressHandler elevateUpHandler;
    private UIPressHandler elevateDownHandler;
    private bool isOriginalPositionStored = false; // Flag para verificar se a posição original já foi armazenada

    // Variável para armazenar o texto de informações técnicas
    private string tecnicalInfoBackupText = "Nome: Projeto Residencial Familiar /nÁrea: 49.640m² /nElevação: 0.15m /nQuantidade de Cômodos: 5" +
                                            "Escala: 50 /nPerímetro: 28.2m /nDimensões: 7.3m x 6,8m /nPortas: 5 /nJanelas: 5";

    //exlusivo do Android
    private float lastBackPressTime = 0f;
    private const float doubleBackDelay = 1.2f; // Tempo máximo entre os dois toques (em segundos)
    private bool waitingForSecondBack = false;


    // Enum para os estados do aplicativo
    public enum AppState
    {
        Starting,           // Estado inicial do aplicativo, antes de qualquer interação
        Scanning,           // Nenhuma imagem rastreada, procurando um marcador
        ObjectFound,        // Objeto instanciado, mas não selecionado
        ObjectSelected,     // Objeto principal selecionado
        LayerSelectionMode, // Modo de seleção de camada ativado
        ObjectRotating,     // Objeto principal está sendo rotacionado
        InfoDisplaying      // Exibindo informações (QR Code ou outras)
    }
    public AppState currentAppState;

    public void SetAppState(AppState newState)
    {
        if (currentAppState == newState) return; // Evita processamento redundante
        currentAppState = newState;
        UpdateUIBasedOnState(); // Chama a função que atualiza a UI
        Debug.Log("App State changed to: " + newState);
    }

    protected void UpdateUIBasedOnState()
    {
        // Primeiro, desative TUDO para garantir um estado limpo
        selectObjectLayerButtonIcon.SetActive(false);
        selectObjectButtonIcon.SetActive(false); // Este é o ícone do botão "selecionar objeto"
        rotationObjectButtonIcon.SetActive(false);
        infoObjectButtonIcon.SetActive(false);
        elevationButtonIcon.SetActive(false);
        newScanButtonIcon.SetActive(false);
        resertObjectButtonIcon.SetActive(false);
        textInfoTechnical.SetActive(false);
        elevationActions.SetActive(false); // Desativa o painel de ações de elevação
        elevationButtonIcon.SetActive(false); // Botão "Elevação" (se necessário)
        homeReturnButton.gameObject.SetActive(false); // Desativa o botão "Home" para reiniciar o app
        // Os botões reais (com componente Button) também precisam ser ativados/desativados.
        // Se selectButton e infoButton são os componentes Button, e os GameObject.Icons são apenas visuais:
        if (selectButton != null) selectButton.gameObject.SetActive(false);
        if (infoButton != null) infoButton.gameObject.SetActive(false);

        scannerAnimation.SetActive(false); // Geralmente inativo, a menos que esteja scaneando
        ResetAndHideDropdownMenu(); // Garante que o dropdown também seja tratado

        // Agora, ative apenas o que é relevante para o estado atual
        switch (currentAppState)
        {
            case AppState.Starting:
                // Este estado não deve ser usado diretamente, mas pode ser útil para inicialização.
                Debug.Log("Estado Start: Inicializando o aplicativo.");
                infoText.text = "Bem-vindo ao Visualizador de Construção Simulada!";
                feedBackInfo(infoBoxBackground, Color.white);
                infoText.color = Color.black; // Define a cor do texto de info para o estado Start
                homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                break;
            case AppState.Scanning:
                Debug.Log("Estado Scanning: aguardando marcador.");
                infoText.text = "Para iniciar foque a camera em um MARCADOR";
                scannerAnimation.SetActive(true);
                feedBackInfo(infoBoxBackground, Color.black);
                infoText.color = Color.white; // Define a cor do texto de info para o estado Scanning
                homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                break;

            case AppState.ObjectFound:
                // GameObject obj = trackedImageHandler.GetInstantiatedObject(); // Puxe o objeto aqui se precisar do nome
                //infoText.text = "Objeto encontrado: " + (obj != null ? obj.name : "N/A");
                infoText.text = "Objeto encontrado!"; // Mensagem genérica para este estado, o nome pode vir de outro lugar.
                feedBackInfo(infoBoxBackground, Color.green);
                homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                infoText.color = Color.black; // Define a cor do texto de info
                newScanButtonIcon.SetActive(true); // Botão para novo scan
                if (selectButton != null) selectButton.gameObject.SetActive(true); // Botão "Selecionar Objeto Inteiro"
                break;

            case AppState.ObjectSelected:
                GameObject selectedObj = SelectionManager.Instance.GetSelectedObject();
                if (selectedObj != null)
                {
                    infoText.text = "Objeto selecionado: " + selectedObj.name;
                    feedBackInfo(infoBoxBackground, Color.blue);
                    infoText.color = Color.white; // Define a cor do texto de info
                    newScanButtonIcon.SetActive(true);
                    selectObjectLayerButtonIcon.SetActive(true); // Botão "Selecionar Camada"
                    rotationObjectButtonIcon.SetActive(true); // Botão "Rotacionar"
                    infoObjectButtonIcon.SetActive(true); // Botão "Informações" (ícone)
                    homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                    if (infoButton != null) infoButton.gameObject.SetActive(true); // Botão real de info
                    resertObjectButtonIcon.SetActive(true); // Botão "Resetar Objeto"
                    elevationButtonIcon.SetActive(true); // Botão "Elevação" (se necessário)
                }
                else
                {
                    // Se o estado é ObjectSelected, mas não há objeto selecionado, algo deu errado.
                    // Volte para ObjectFound ou Scanning, ou log um erro.
                    Debug.LogWarning("Estado ObjectSelected, mas nenhum objeto selecionado. Voltando para ObjectFound.");
                    SetAppState(AppState.ObjectFound);
                }
                break;

            case AppState.LayerSelectionMode:
                infoText.text = "Selecione uma camada do Objeto.";
                // Manter botões de interação do objeto selecionado visíveis
                newScanButtonIcon.SetActive(true);
                selectObjectLayerButtonIcon.SetActive(true);
                rotationObjectButtonIcon.SetActive(true);
                infoObjectButtonIcon.SetActive(true);
                homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                if (infoButton != null) infoButton.gameObject.SetActive(true);
                resertObjectButtonIcon.SetActive(true);
                elevationButtonIcon.SetActive(true); // Botão "Elevação" (se necessário)
                dropdownMenu.SetActive(true); // Ativa o menu suspenso
                // Preencher o dropdown (pode ser feito aqui ou na ClickSelectLayer)
                PopulateDropdownLayers(SelectionManager.Instance.GetSelectedObject());
                break;

            case AppState.ObjectRotating:
                homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                infoText.text = "Arraste para rotacionar o Objeto!";
                newScanButtonIcon.SetActive(true);
                rotationObjectButtonIcon.SetActive(true); // Manter ativo para indicar o modo
                resertObjectButtonIcon.SetActive(true);
                // isDragging = true; // Não defina isDragging aqui, isso é feito pelo método rotateButton()
                break;

            case AppState.InfoDisplaying:
                homeReturnButton.gameObject.SetActive(true); // Botão "Home" para reiniciar o app
                infoText.text = "Exibindo informações técnicas.";
                newScanButtonIcon.SetActive(true);
                infoObjectButtonIcon.SetActive(true); // Manter ativo
                if (infoButton != null) infoButton.gameObject.SetActive(true);
                textInfoTechnical.SetActive(true); // Ativa o painel de texto de informações
                break;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetAppState(AppState.Starting); // Define o estado inicial do aplicativo como Scanning
        // currentAppState é um enum, seu valor padrão é 0 (Scanning).
        // A chamada SetAppState(AppState.Scanning) já está no OnEnable, o que é um bom lugar.
        // Se você quiser garantir que o estado inicial seja definido assim que o script é ativado,
        // OnEnable é preferível ao Start, especialmente se o script pode ser ativado/desativado.
        // Se Start() for chamado, ele será chamado uma única vez no ciclo de vida do script.
        // currentAppState = AppState.Scanning; // Não defina diretamente, chame o SetAppState para garantir que a UI seja atualizada.
        // SetAppState(AppState.Scanning); // Já está em OnEnable.
    }
    void OnEnable()
    {
        // Certifique-se de que o estado inicial é definido aqui se não for definido em Start.
        // Se Start é onde você quer a inicialização principal, mova SetAppState(AppState.Scanning) para lá.
        // Se o script pode ser desativado/ativado e você quer que ele reinicie corretamente, OnEnable é melhor.
        SetAppState(AppState.Scanning); // Garante que a UI é configurada ao iniciar/re-ativar o componente.

        Debug.Log("App State initialized to: " + currentAppState);
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(ClickSelect);
        }
        if (infoButton != null)
        {
            infoButton.onClick.AddListener(infoShow);
        }

        // Obtenha as referências para os UIPressHandler dos botões de elevação
        if (elevateUpButton != null)
        {
            elevateUpHandler = elevateUpButton.GetComponent<UIPressHandler>();
            if (elevateUpHandler == null) Debug.LogError("ElevateUpButton não possui UIPressHandler!");
            Debug.Log("ElevateUpHandler configurado: " + elevateUpHandler.name);
        }
        if (elevateDownButton != null)
        {
            elevateDownHandler = elevateDownButton.GetComponent<UIPressHandler>();
            if (elevateDownHandler == null) Debug.LogError("ElevateDownButton não possui UIPressHandler!");
            Debug.Log("ElevateDownHandler configurado: " + elevateDownHandler.name);
        }
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived; // Adiciona o evento de frame recebido da câmera
            Debug.Log("ARCameraManager configurado para receber frames.");
        }
        else
        {
            Debug.LogError("ARCameraManager não está configurado. Certifique-se de que o componente ARCameraManager está atribuído.");
        }
        // É importante adicionar listeners para OUTROS botões (rotation, layer select, new scan, reset)
        // se eles não forem configurados no Inspector. Se forem via Inspector, não precisa aqui.
        // Exemplo:
        // if (selectObjectLayerButtonIcon.GetComponent<Button>() != null) {
        //     selectObjectLayerButtonIcon.GetComponent<Button>().onClick.AddListener(ClickSelectLayer);
        // }
    }

    void OnDisable()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(ClickSelect);
        }
        if (infoButton != null)
        {
            infoButton.onClick.RemoveListener(infoShow);
        }
        // Remover listeners para outros botões também
    }

    // Update is called once per frame
    void Update()
    {
        // --- Lógica de Detecção e Atualização de Estado do Aplicativo ---
        // (Mantenha esta seção como está, ela está boa)
        if (!checkTrack())
        {
            SetAppState(AppState.Scanning);
        }
        else
        {
            if (trackedImageHandler.isObjectInstantiated && SelectionManager.Instance.HasSelection())
            {
                if (currentAppState != AppState.ObjectRotating && currentAppState != AppState.LayerSelectionMode && currentAppState != AppState.InfoDisplaying)
                {
                    SetAppState(AppState.ObjectSelected);
                }
            }
            else if (trackedImageHandler.isObjectInstantiated && !SelectionManager.Instance.HasSelection())
            {
                if (currentAppState == AppState.Scanning || currentAppState == AppState.ObjectSelected)
                {
                    SetAppState(AppState.ObjectFound);
                }
            }
            //if (trackedImageHandler.isObjectInstantiated && string.IsNullOrEmpty(qrCodeTextCache))
            //{
            //    StartCoroutine(ReadQRCodeAndCache());
            //}
        }

        // --- Processamento de Input ---

        // 1. Verificação de clique inicial (GetMouseButtonDown)
        //    Este é o momento para decidir se um clique INICIAL (o momento que o botão do mouse é pressionado)
        //    foi em uma UI ou no mundo 3D.
        if (Input.GetMouseButtonDown(0))
        {
            bool isPointerOverUIOnDown = false;
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
                isPointerOverUIOnDown = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#else
        isPointerOverUIOnDown = EventSystem.current.IsPointerOverGameObject();
#endif
            Debug.Log(EventSystem.current.IsPointerOverGameObject());
            if (isPointerOverUIOnDown)
            {
                // Se o clique inicial (down) foi na UI, resetamos flags de interação 3D.
                // NÃO RETORNAMOS AQUI, para que a lógica de arrasto ainda possa ser ativada,
                // ou para que um drag iniciado na UI possa ser ignorado pelo arrasto 3D,
                // mas o GetMouseButtonUp() ainda possa ser processado.
                isWaitingForSecondClick = false;
                // canDragToRotate = false; // Não resete aqui, o botão de rotação irá definir isso.
                Debug.Log("Input.GetMouseButtonDown(0) sobre UI.");
            }
            else
            {
                // Se o clique inicial (down) foi no mundo 3D
                Debug.Log("Input.GetMouseButtonDown(0) sobre 3D.");
                // Processa o segundo clique para seleção de objetos/camadas
                if (isWaitingForSecondClick) // Se a flag foi ativada por ClickSelect() ou ClickSelectLayer()
                {
                    // A lógica de raycast para seleção do objeto 3D
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 100))
                    {
                        GameObject hitObj = hit.transform.gameObject;
                        Debug.Log($"Raycast atingiu: {hitObj.name} com a tag: {hitObj.tag}");

                        if (currentAppState == AppState.LayerSelectionMode)
                        {
                            if (validTags.Contains(hitObj.tag))
                            {
                                SelectionManager.Instance.SelectLayer(hitObj);
                                infoText.text = "Camada selecionada: " + hitObj.name;
                                SetAppState(AppState.ObjectSelected);
                            }
                            else
                            {
                                infoText.text = "Clique inválido. Selecione uma camada válida.";
                            }
                        }
                        else // currentAppState == AppState.ObjectFound (para selecionar objeto inteiro)
                        {
                            if (hitObj.tag == "construction" || hitObj.tag == "construction2")
                            {
                                SelectionManager.Instance.SelectWholeObject(hitObj);
                                StoreOriginalPosition(hitObj);
                                infoText.text = "Objeto selecionado: " + hitObj.name;
                                Debug.Log("Objeto selecionado: " + hit.transform.name);
                                SetAppState(AppState.ObjectSelected);
                            }
                            else if (validTags.Contains(hitObj.tag))
                            {
                                infoText.text = "Este é um sub-Objeto. Ative a seleção de camada para selecioná-lo.";
                            }
                            else
                            {
                                infoText.text = "Clique inválido. Tente novamente.";
                            }
                        }
                    }
                    else
                    {
                        infoText.text = "Nenhum Objeto encontrado. Clique novamente para selecionar.";
                    }
                    isWaitingForSecondClick = false; // Resetar após a tentativa de clique 3D
                }
            }
        }

        // 2. Processamento do arrasto contínuo (GetMouseButton)
        //    Esta lógica deve ser executada se o botão do mouse estiver pressionado,
        //    INDEPENDENTEMENTE de onde o clique inicial (GetMouseButtonDown) ocorreu,
        //    mas o arrasto só deve afetar o objeto 3D se o ponteiro NÃO estiver sobre a UI AGORA.
        if (Input.GetMouseButton(0) && currentAppState == AppState.ObjectRotating && canDragToRotate)
        {
            bool isPointerOverUIForDrag = false;
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
                isPointerOverUIForDrag = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#else
        isPointerOverUIForDrag = EventSystem.current.IsPointerOverGameObject();
#endif

            if (!isPointerOverUIForDrag) // Só permite rotacionar se o ponteiro NÃO estiver sobre a UI
            {
                if (SelectionManager.Instance.HasSelection())
                {
                    Debug.Log("Arrastando para rotacionar o Objeto selecionado (mundo 3D).");
                    float rotationSpeed = 100f;
                    float mouseX = Input.GetAxis("Mouse X");
                    GameObject selected = SelectionManager.Instance.GetSelectedObject();
                    if (selected != null)
                    {
                        selected.transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
                    }
                }
            }
            else
            {
                if (SelectionManager.Instance.HasSelection())
                {
                    // Se o ponteiro está sobre a UI, não rotaciona o objeto 3D.
                    isDragging = false; // Reseta isDragging para evitar confusão
                    canDragToRotate = false; // Desativa o arrasto para rotação
                    Debug.Log("Arrasto sobre UI, rotação 3D bloqueada.");
                }
                else
                {
                    // Se não há seleção, não faz nada.
                    isDragging = false; // Reseta isDragging para evitar confusão
                    canDragToRotate = false; // Desativa o arrasto para rotação
                }
                // O arrasto está sobre a UI. Não rotacionar o objeto 3D.
                // Debug.Log("Arrasto sobre UI, rotação 3D bloqueada.");
            }
        }

        // 3. Processamento do botão do mouse solto (GetMouseButtonUp)
        //    Deve ser processado sempre, para garantir que as flags de arrasto sejam resetadas.
        if (Input.GetMouseButtonUp(0))
        {

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (var result in results)
            {
                Debug.Log("UI Clicked: " + result.gameObject.name);
            }

            if (isDragging) // Se estava arrastando, finalize o arrasto
            {
                isDragging = false;
                canDragToRotate = false; // Desativa o arrasto para rotação
                Debug.Log("Arrasto (geral) finalizado.");
                // Após finalizar o arrasto, se o estado era de rotação, volte para ObjectSelected
                if (currentAppState == AppState.ObjectRotating && SelectionManager.Instance.HasSelection())
                {
                    SetAppState(AppState.ObjectSelected);
                }
            }
            // Também pode ser que isWaitingForSecondClick esteja ativo e o usuário soltou sem clicar em nada 3D.
            // É seguro resetar aqui também, se nenhuma seleção 3D foi feita.
            // isWaitingForSecondClick = false; // Já é resetado no bloco GetMouseButtonDown(0)
        }

        // --- Execução da Elevação (Enquanto os botões são pressionados) ---
        // A elevação só deve ser possível se o OBJETO ESTIVER SELECIONADO E O PAINEL DE AÇÕES DE ELEVAÇÃO ESTIVER ATIVO.
        if (SelectionManager.Instance.HasSelection() && elevationActions.activeInHierarchy)
        {
            // NOTA: Os botões de elevação SÃO elementos UI.
            // O isPointerOverUIThisFrame só é para a lógica de RAYCASTING 3D.
            // A lógica abaixo NÃO é afetada pelo isPointerOverUIThisFrame.

            if (elevateUpHandler != null && elevateUpHandler.IsBeingPressed)
            {
                Debug.Log("Botão de Elevar Para Cima pressionado.");
                // O botão de "Elevar Para Cima" está sendo segurado.
                ElevateObject(Vector3.up);
            }
            else if (elevateDownHandler != null && elevateDownHandler.IsBeingPressed)
            {
                Debug.Log("Botão de Elevar Para Baixo pressionado.");
                // O botão de "Elevar Para Baixo" está sendo segurado.
                ElevateObject(Vector3.down);
            }
        }


        // Se não está no estado de rotação, garante que isDragging e canDragToRotate sejam false
        if (currentAppState != AppState.ObjectRotating)
        {
            isDragging = false;
            canDragToRotate = false;
        }

    //Acao de fechar o app
#if UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            float currentTime = Time.time;
            if (waitingForSecondBack && (currentTime - lastBackPressTime) < doubleBackDelay)
            {
                Debug.Log("Saindo do aplicativo por duplo toque no botão voltar.");
                Application.Quit();
            }
            else
            {
                waitingForSecondBack = true;
                lastBackPressTime = currentTime;
                infoText.text = "Pressione voltar novamente para sair.";
                Debug.Log("Pressione voltar novamente para sair.");
            }
        }

    // Reseta o estado se o usuário não pressionar o segundo voltar a tempo
    if (waitingForSecondBack && (Time.time - lastBackPressTime) > doubleBackDelay)
    {
        waitingForSecondBack = false;
    }
    #endif  

    }
    private IEnumerator EnableClickAfterDelay()
    {
        yield return new WaitForSeconds(0.2f); // pequeno delay evita clique duplicado
        isWaitingForSecondClick = true;
        Debug.Log("Delay para segundo clique 3D concluído. isWaitingForSecondClick = true.");
    }

    private IEnumerator EnableRotationDragAfterDelay()
    {
        // Este delay é para ignorar o clique inicial no ícone do botão "Rotacionar".
        // Após o delay, permite que o arrasto para rotação comece.
        yield return new WaitForSeconds(0.2f);
        canDragToRotate = true;
        isDragging = true; // Define isDragging aqui para indicar que o arrasto está ativo para rotação
        Debug.Log("Delay para arrasto de rotação concluído. canDragToRotate = true, isDragging = true.");

    }

    public void ClickSelect()
    {
        Debug.Log("Botão de selecionar Objeto ativado!");
        infoText.text = "Clique em um Objeto para selecioná-lo.";
        isWaitingForSecondClick = false; // Reseta a flag de espera para o segundo clique
        StartCoroutine(EnableClickAfterDelay());
        Debug.Log("Aguardando segundo clique para selecionar o Objeto.");
    }

    public void ClickSelectLayer()
    {
        if (SelectionManager.Instance.HasSelection())
        {
            var selectedObject = SelectionManager.Instance.GetSelectedObject();
            if (selectedObject != null && HasLayerChildren(selectedObject))
            {
                SetAppState(AppState.LayerSelectionMode); // Agora este é o único lugar que define o estado
                Debug.Log("Modo de seleção de camada ativado. Clique em uma camada do Objeto.");
                SelectionManager.Instance.EnableLayerSelection(); // Ativa o modo de seleção de camada no SelectionManager
                isWaitingForSecondClick = true;
                StartCoroutine(EnableClickAfterDelay());
                // PopulateDropdownLayers() é chamado dentro de UpdateUIBasedOnState.LayerSelectionMode
            }
            else
            {
                Debug.LogWarning("O Objeto selecionado não possui camadas válidas.");
                infoText.text = "O Objeto selecionado não possui camadas válidas.";
                // Se não pode entrar no modo, volte para o estado anterior (ObjectSelected)
                SetAppState(AppState.ObjectSelected);
            }
        }
        else
        {
            Debug.LogWarning("Nenhum Objeto foi selecionado. Selecione um Objeto primeiro.");
            infoText.text = "Nenhum Objeto foi selecionado. Clique em um Objeto primeiro.";
            // Permaneça no estado ObjectFound ou Scanning, dependendo se há um objeto geral
        }
    }

    public void homeButton()
    {
        Debug.Log("Home button clicked! O cenário deve ser resetado.");
        clearVirtualizedObject();
        arSessionController.StartAR();
        SetAppState(AppState.Scanning); // Define o estado final

    }

    public void newScan()
    {
        Debug.Log("Iniciando novo scan. Prefabs serão desativados!");
        clearVirtualizedObject();
        SetAppState(AppState.Scanning); // Define o estado final
    }

    public void clearVirtualizedObject()
    {
        // Desativa todos os Objetos desativados
        Debug.Log("Prefab deactivated! Ativando rotina do rastreador"); // Log to the console
        trackedImageHandler.OnNewScan(); // Call the method to clear the virtualized object
    }

    public bool checkTrack()
    {
        if (trackedImageHandler != null && trackedImageHandler.isObjectInstantiated)
        {
            scannerAnimation.SetActive(false); // Ativa a animação do scanner
            return true; // Return true if the prefab is instantiated
        }
        else
        {
            return false; // Return false if the prefab is not instantiated
        }
    }


    public void rotateButton()
    {
        if (SelectionManager.Instance.HasSelection())
        {
            SetAppState(AppState.ObjectRotating); // Define o estado para rotação. Isso ativará a UI de rotação.
            Debug.Log("Botão 'Rotacionar' ativado! Objeto atual: " + SelectionManager.Instance.GetSelectedObject().name);
            isDragging = false; // Importante: Inicia o arrasto aqui
            canDragToRotate = false; // Permite que o arrasto rotacione o objeto]
            StartCoroutine(EnableRotationDragAfterDelay()); // Inicia a espera para o segundo clique
        }
        else
        {
            infoText.text = "Nenhum Objeto selecionado para rotacionar!";
            // Se não há objeto para rotacionar, o estado deve permanecer como ObjectFound ou ObjectSelected
            // Não mude o estado para ObjectRotating se não houver seleção.
            isDragging = false; // Garante que não está arrastando
            canDragToRotate = false; // Garante que não pode arrastar para rotacionar
        }
    }

    public void infoShow()
    {
        SetAppState(AppState.InfoDisplaying); // Define o estado para exibir informações

        // Exibe o texto do cache imediatamente
        tecnicalTextOfObject.text = qrCodeTextCache ?? "Informações do QR Code não disponíveis ou ainda sendo lidas.";
        textInfoTechnical.SetActive(true);

        // Se o QR Code ainda não foi lido e não há uma leitura em andamento, inicie uma.
        if (string.IsNullOrEmpty(qrCodeTextCache) && !isReadingQRCode)
        {
            Debug.Log("Botão Info clicado. QR Code não cacheado e não lendo. Tentando leitura.");
            // A leitura real é iniciada em OnCameraFrameReceived quando um frame da câmera AR chega.
            // Aqui, podemos forçar uma tentativa se quisermos, mas a lógica em OnCameraFrameReceived já controla isso.
            // O mais importante é que a UI já está mostrando "aguardando".
        }
        else if (!string.IsNullOrEmpty(qrCodeTextCache))
        {
            Debug.Log("Botão Info clicado. Exibindo QR Code do cache.");
        }
        else if (isReadingQRCode)
        {
            Debug.Log("Botão Info clicado. Leitura de QR Code já em andamento.");
            infoText.text = "Lendo informações do QR Code, aguarde...";
        }
    }

    public void elevationButton()
    {
        if (SelectionManager.Instance.HasSelection())
        {
            elevationActions.SetActive(true); // Ativa o painel de UI com os botões UP/DOWN
            infoText.text = "Use os botões para mover o objeto verticalmente.";
        }
        else
        {
            infoText.text = "Nenhum Objeto selecionado para elevar!";
            elevationActions.SetActive(false); // Garante que o painel está desativado se não houver seleção.
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
        elevationButtonIcon.GetComponent<RawImage>().color = color;
        newScanButtonIcon.GetComponent<RawImage>().color = color;
    }



    public void ElevateObject(Vector3 direction)
    {
        GameObject selected = SelectionManager.Instance.GetSelectedObject();
        if (selected != null)
        {
            float elevationMovement = elevationStep * Time.deltaTime; // Movimento por frame
            selected.transform.position += direction * elevationMovement;
            Debug.Log($"Objeto movido. Nova posição Y: {selected.transform.position.y:F2}");
            infoText.text = $"Movendo: {selected.name} Y: {selected.transform.position.y:F2}";
        }
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

    private IEnumerator ReadQRCodeAndCache()
    {
        isReadingQRCode = true;
        qrCodeTextCache = null;

#if UNITY_EDITOR
        qrCodeTextCache = "Leitura de QR Code não disponível no modo simulado/Editor.";
        isReadingQRCode = false;
        yield break;
#else
    if (WebCamTexture.devices.Length == 0)
    {
        qrCodeTextCache = "Nenhuma câmera detectada no dispositivo.";
        isReadingQRCode = false;
        yield break;
    }

    WebCamTexture webcamTexture = null;
    Exception lastException = null;
    string qrText = null;

    try
    {
        webcamTexture = new WebCamTexture(320, 240); // Reduza a resolução!
        webcamTexture.Play();
        Debug.Log("WebCamTexture iniciada para leitura de QR Code.");

        float timeout = 3f;
        float elapsed = 0f;
        while (webcamTexture.width <= 16 && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (webcamTexture.width <= 16)
        {
            qrCodeTextCache = "Falha ao inicializar a câmera (largura inválida).";
            Debug.LogError(qrCodeTextCache);
            yield break;
        }

        yield return new WaitForSeconds(1f);

        IBarcodeReader barcodeReader = new BarcodeReader();
        var snap = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);

        float timer = 0f;
        while (timer < 3f && qrText == null) // Reduza o tempo total!
        {
            try
            {
                snap.SetPixels32(webcamTexture.GetPixels32());
                snap.Apply();

                var result = barcodeReader.Decode(snap.GetPixels32(), snap.width, snap.height);
                if (result != null)
                {
                    qrText = result.Text;
                    Debug.Log("QR Code lido com sucesso: " + qrText);
                    break;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.LogWarning("Erro durante a decodificação do QR Code: " + ex.Message);
            }

            timer += 1f;
            yield return new WaitForSeconds(1f); // Menos tentativas, menos travamento
        }
        Destroy(snap);
    }
    finally
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
            Debug.Log("WebCamTexture parada.");
        }
        isReadingQRCode = false;
    }

    if (lastException != null)
    {
        qrCodeTextCache = "Erro ao tentar ler QR Code: " + lastException.Message;
    }
    else if (!string.IsNullOrEmpty(qrText))
    {
        qrCodeTextCache = qrText;
    }
    else
    {
        qrCodeTextCache = "QR Code não detectado após tentativas!";
    }
    Debug.Log("QR Code leitura finalizada. Cache: " + qrCodeTextCache);
#endif
    }


    // Helper coroutine to wait for a Task to complete
    private IEnumerator AwaitTaskInCoroutine<T>(Task<T> task, float timeoutSeconds, Action<T> onComplete, Action<Exception> onError, Action onTimeout)
    {
        float elapsed = 0f;
        while (!task.IsCompleted && elapsed < timeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!task.IsCompleted)
        {
            onTimeout?.Invoke();
        }
        else if (task.IsFaulted) // Task completed with an exception
        {
            onError?.Invoke(task.Exception);
        }
        else // Task completed successfully
        {
            onComplete?.Invoke(task.Result);
        }
    }

    private async UniTask ProcessImageForQRCode(XRCpuImage image)
    {
        string decodedText = null;
        Exception backgroundException = null;
        XRCpuImage.AsyncConversion request = default;
        bool requestInitialized = false;
        //bool imageDisposed = false;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(320, 240),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        try
        {
            request = image.ConvertAsync(conversionParams);
            requestInitialized = true;

            // Await the completion of the conversion
            while (!request.status.IsDone())
            {
                await UniTask.Yield();
            }

            if (request.status == XRCpuImage.AsyncConversionStatus.Ready) // Corrected status check
            {
                NativeArray<byte> rawImageData = request.GetData<byte>();


                // === AQUI ESTÁ A CORREÇÃO CRÍTICA ===
                // Copie os dados brutos para um array gerenciado (byte[] ou Color32[])
                // Este `byte[]` será passado para o Task.Run.
                byte[] pixelBytesForTask = rawImageData.ToArray(); // Copia NativeArray para um byte[] gerenciado
                                                                   // .ToArray() é uma extensão de Unity.Collections
                                                                   // Alternativamente, pode ser um loop manual se preferir.



                int width = conversionParams.outputDimensions.x;
                int height = conversionParams.outputDimensions.y;

                // Dispose image and request here, as data is now in rawImageData
                /*if (image.valid)
                {
                    image.Dispose();
                    imageDisposed = true;
                }
                if (requestInitialized)
                {
                    request.Dispose();
                    requestInitialized = false;
                }
                */

                // Copy to Color32[] for ZXing and run in a background Task
                Task<Result> decodeTask = Task.Run(() =>
                {
                    try
                    {
                        Color32[] pixels = new Color32[width * height];
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            int byteIndex = i * 4;
                            pixels[i] = new Color32(
                                rawImageData[byteIndex],
                                rawImageData[byteIndex + 1],
                                rawImageData[byteIndex + 2],
                                rawImageData[byteIndex + 3]
                            );
                        }
                        IBarcodeReader barcodeReader = new BarcodeReader();
                        return barcodeReader.Decode(pixels, width, height);
                    }
                    catch (Exception ex)
                    {
                        backgroundException = ex;
                        return null;
                    }
                });

                // Await the completion of the decoding task
                // Use .WithTimeout() from UniTask for easy timeout handling
                try
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                    var completedTask = await Task.WhenAny(decodeTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        qrCodeTextCache = "Timeout na decodificação do QR Code (tarefa assíncrona).";
                        Debug.LogError(qrCodeTextCache);
                        return; // Exit here if timeout
                    }
                }
                catch (TimeoutException)
                {
                    qrCodeTextCache = "Timeout na decodificação do QR Code (tarefa assíncrona).";
                    Debug.LogError(qrCodeTextCache);
                    // No need to check IsCompleted if timeout occurred, as it threw an exception
                    return; // Exit here if timeout
                }


                if (backgroundException != null)
                {
                    qrCodeTextCache = "Erro na decodificação assíncrona do QR Code: " + backgroundException.Message;
                    Debug.LogError(qrCodeTextCache);
                }
                else if (decodeTask.Result != null)
                {
                    decodedText = decodeTask.Result.Text;
                    qrCodeTextCache = decodedText;
                    Debug.Log("QR Code lido com sucesso (assíncrono): " + qrCodeTextCache);
                }
                else
                {
                    qrCodeTextCache = "QR Code não detectado no frame ou resultado nulo.";
                    Debug.Log("QR Code não detectado neste frame.");
                }
            }
            else // Conversion to RGBA32 failed
            {
                qrCodeTextCache = "Falha ao converter imagem da CPU para QR Code. Status: " + request.status;
                Debug.LogError("Falha na conversão da imagem da CPU: " + request.status);
            }
        }
        catch (Exception ex) // Catch any exceptions from the async flow
        {
            qrCodeTextCache = "Erro inesperado durante o processamento do QR Code: " + ex.Message;
            Debug.LogError("Erro inesperado no ProcessImageForQRCode: " + ex.Message);
        }
        finally
        {
            // Garante que a imagem da CPU seja descartada
            if (image.valid) // Se a imagem não foi descartada anteriormente
            {
                image.Dispose();
            }
            // Garante que a requisição seja descartada
            if (requestInitialized && request.status.IsDone())
            {
                request.Dispose();
            }

            isReadingQRCode = false;
            Debug.Log("Processamento de imagem para QR Code finalizado. isReadingQRCode = false.");
            // Make sure DisplayCachedObjectInfo() is called here or in your `infoShow()`
            // DisplayCachedObjectInfo(); // Call this to update the UI
        }
    }


    private void PopulateDropdownLayers(GameObject selectedObject)
    {
        if (dropdownMenu == null) return;
        TMP_Dropdown dropdown = dropdownMenu.GetComponentInChildren<TMP_Dropdown>();
        if (dropdown == null) return;

        dropdown.ClearOptions();
        dropdown.onValueChanged.RemoveAllListeners(); // Limpa os listeners antigos

        List<string> layerNames = new List<string>();
        layerNames.Add("Todas as camadas");

        if (selectedObject != null)
        {
            foreach (Transform child in selectedObject.transform)
            {
                if (child != null && validTags.Contains(child.tag))
                {
                    layerNames.Add(child.name);
                }
            }
        }
        dropdown.AddOptions(layerNames);

        // Adiciona um listener para detectar a seleção no dropdown
        dropdown.onValueChanged.AddListener((index) =>
        {
            if (index == 0)
            {
                SelectionManager.Instance.SetAllLayersActive(true);
                infoText.text = "Todas as camadas ativadas.";
            }
            else
            {
                Debug.Log("Camada selecionada no dropdown: " + layerNames[index]);
                SelectionManager.Instance.SelectLayerByName(layerNames[index]);
                infoText.text = "Camada selecionada: " + layerNames[index];
            }
            // Após a seleção da camada pelo dropdown, você pode querer sair do modo de seleção de camada
            // e voltar para ObjectSelected. Depende da sua UX.
            // SetAppState(AppState.ObjectSelected);
        });
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!isReadingQRCode && trackedImageHandler.isObjectInstantiated && string.IsNullOrEmpty(qrCodeTextCache))
        {
            if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                isReadingQRCode = true;
                Debug.Log("AR Camera Frame recebido. Iniciando processamento de imagem para QR Code.");
                // No need for StartCoroutine with UniTask
                ProcessImageForQRCode(image).Forget(); // .Forget() is used when you don't need to await the UniTask from here
            }
        }
    }
}
