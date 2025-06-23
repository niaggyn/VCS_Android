using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    // Eventos para notificar outros scripts sobre mudanças de seleção
    public event Action<GameObject> OnObjectSelected;
    public event Action<GameObject> OnObjectDeselected;


    private GameObject selectedObject;       // Objeto inteiro selecionado
    private GameObject selectedLayer;        // Camada selecionada
    private bool layerSelectionActive = false; // Indica se o modo de seleção de camada está ativo

    private List<GameObject> previouslyHighlighted = new List<GameObject>();
    private List<Outline> activeOutlines = new List<Outline>(); // Rastreia todos os outlines ativos
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    public Color highlightColor = Color.yellow; // Cor padrão do highlight
    public Material transparentMaterial; // Material with the outline shader

    private Outline currentOutline;
    private Material originalMaterial; // Store the original material
    public Material highlightMaterial; // Material with the outline shader

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this) // Garante que só há uma instância e destrói duplicatas
        {
            Destroy(gameObject);
            return; // Importante para evitar que o Awake continue executando em um objeto que será destruído.
        }
    }

    // Este é o método primário para selecionar um objeto principal 3D.
    public void SelectObject(GameObject obj)
    {
        if (selectedObject == obj) return; // Objeto já selecionado

        // 1. Desseleciona o objeto anteriormente selecionado (e limpa visuais)
        DeselectObject(); // Dispara OnObjectDeselected para o objeto antigo

        // 2. Define o novo objeto selecionado
        selectedObject = obj;
        selectedLayer = null; // Zera a seleção de camada ao selecionar um novo objeto principal
        layerSelectionActive = false; // Desativa modo de seleção de camada

        // 3. Aplica destaque visual ao novo objeto principal
        ApplyHighlight(selectedObject); // Aplica o highlight (outline)

        // 4. Notifica os ouvintes
        OnObjectSelected?.Invoke(selectedObject);
        Debug.Log($"[SelectionManager] Objeto principal selecionado: {selectedObject.name}");
    }

    public void DeselectObject()
    {
        if (selectedObject == null) return;

        // 1. Remove destaque visual e restaura materiais
        RemoveAllHighlightsAndRestoreMaterials();

        // 2. Notifica os ouvintes
        OnObjectDeselected?.Invoke(selectedObject);
        Debug.Log($"[SelectionManager] Objeto principal desselecionado: {selectedObject.name}");

        // 3. Limpa a referência
        selectedObject = null;
        selectedLayer = null;
        layerSelectionActive = false;
    }

    public bool HasSelection()
    {
        return selectedObject != null;
    }

    public void EnableLayerSelection()
    {
        if (selectedObject == null)
        {
            Debug.LogWarning("[SelectionManager] Não é possível ativar o modo de seleção de camada: nenhum objeto principal selecionado.");
            return;
        }
        layerSelectionActive = true;
        SetAllLayersActive(true); // Garante que todas as camadas do objeto selecionado estão ativas para visualização
        Debug.Log("[SelectionManager] Modo de seleção de camada ATIVO.");
    }

    public void DisableLayerSelection()
    {
        layerSelectionActive = false;
        ClearLayerHighlight(); // Limpa o destaque da camada selecionada
        SetAllLayersActive(true); // Opcional: Ativa todas as camadas novamente para visibilidade padrão
        // Se a seleção de camada for para "isolar" uma camada, você pode querer que as outras permaneçam transparentes
        // ou desativadas aqui, ou que retornem ao estado do objeto inteiro.
        Debug.Log("[SelectionManager] Modo de seleção de camada DESATIVADO.");
    }

    public bool IsLayerSelectionActive()
    {
        return layerSelectionActive;
    }

    public void SelectLayer(GameObject layer)
    {
        if (selectedObject == null || layer == null || layer.transform.parent != selectedObject.transform)
        {
            Debug.LogWarning("[SelectionManager] Tentativa de selecionar camada inválida ou não pertencente ao objeto selecionado.");
            return;
        }

        ClearLayerHighlight(); // Remove destaque da camada anterior (se houver)
        selectedLayer = layer; // Define a nova camada selecionada

        // Aplica o material transparente para ocultar as outras camadas, mantendo a selecionada
        foreach (Transform child in selectedObject.transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (child.gameObject == layer)
                {
                    // Restaura o material original da camada selecionada para que ela seja visível
                    RestoreOriginalMaterial(renderer);
                    ApplyHighlight(child.gameObject); // Opcional: Adicionar um highlight na camada selecionada
                    Debug.Log($"[SelectionManager] Camada '{layer.name}' selecionada e destacada.");
                }
                else // Se não é a camada selecionada, e é uma camada válida (oculta/transparente)
                {
                    // Supondo que validLayerTags é uma lista de strings públicas para tags válidas
                    if (validLayer(child.transform)) // Use o método validLayer
                    {
                        ApplyTransparency(renderer); // Torna transparente
                        Debug.Log($"[SelectionManager] Camada '{child.name}' tornada transparente.");
                    }
                    else
                    {
                        // Opcional: Manter visíveis se não for uma camada "filtrável"
                    }
                }
            }
        }
        Debug.Log($"[SelectionManager] Camada '{layer.name}' selecionada no objeto '{selectedObject.name}'.");

    }

    public void SetAllLayersActive(bool active)
    {
        if (selectedObject == null) return;

        foreach (Transform child in selectedObject.transform)
        {
            // Apenas afeta GameObjects que representam camadas (você pode querer filtrar por tag aqui)
            if (validLayer(child.transform)) // Usa seu método validLayer
            {
                child.gameObject.SetActive(active);
                // Se ativa == true, restaura material original. Se false, não se preocupe com material.
                if (active)
                {
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        RestoreOriginalMaterial(renderer);
                    }
                }
            }
        }
        Debug.Log($"[SelectionManager] Todas as camadas do objeto '{selectedObject.name}' {(active ? "ativadas" : "desativadas")}.");
    }

    private void ApplyHighlight(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();

        outline.effectColor = highlightColor;
        outline.effectDistance = new Vector2(1, 1); // Ajuste a espessura do outline
        outline.enabled = true; // Garante que o outline está ativo
        activeOutlines.Add(outline); // Mantém uma referência para limpar depois
    }

    private void ApplyTransparency(Renderer renderer)
    {
        // Armazena materiais originais antes de aplicar transparência
        if (!originalMaterials.ContainsKey(renderer))
        {
            originalMaterials[renderer] = renderer.materials;
        }

        // Aplica o material transparente
        renderer.material = transparentMaterial; // Assumindo que transparentMaterial é um material URP/Standard que suporta transparência
        // Se transparentMaterial for um array, ou se você precisa de configurações específicas de transparência,
        // use SetMaterialTransparency como você já tem.
        SetMaterialTransparency(renderer.material, 0.2f); // Exemplo: 20% de opacidade
    }

    private void RestoreOriginalMaterial(Renderer renderer)
    {
        if (originalMaterials.ContainsKey(renderer))
        {
            renderer.materials = originalMaterials[renderer];
            originalMaterials.Remove(renderer);
            Debug.Log($"[SelectionManager] Materiais de '{renderer.gameObject.name}' restaurados.");
        }
        // Garante que o material não é mais transparente se tiver sido alterado diretamente
        SetMaterialTransparency(renderer.material, 1.0f);
    }

    private void RemoveAllHighlightsAndRestoreMaterials()
    {
        // Remove outlines de todos os objetos que foram highlightados
        foreach (Outline outline in activeOutlines)
        {
            if (outline != null && outline.gameObject != null)
            {
                Destroy(outline); // Destroi o componente Outline
            }
        }
        activeOutlines.Clear();

        // Restaura materiais originais para todos os renderers cujos materiais foram alterados
        foreach (var entry in originalMaterials)
        {
            if (entry.Key != null) // entry.Key é o Renderer
            {
                entry.Key.materials = entry.Value;
            }
        }
        originalMaterials.Clear(); // Limpa o dicionário

        RestoreLayerTransparency(); // Seu método existente para camadas (pode ser integrado acima)
        Debug.Log("[SelectionManager] Todos os destaques visuais e materiais restaurados.");
    }

    // Método para destacar o objeto inteiro
    public void SelectWholeObject(GameObject obj)
    {
        ClearSelection();
        selectedObject = obj;
        ApplyOutline(obj); // Aplica o contorno ao objeto inteiro
        selectedLayer = null; // Reseta a camada selecionada
        layerSelectionActive = false; // Desativa o modo de seleção de camada
        Debug.Log("Objeto inteiro selecionado: " + obj.name);
    }

    // Método para destacar uma camada específica
    public GameObject GetSelectedObject()
    {
        return selectedObject;
    }

    public GameObject GetSelectedLayer()
    {
        return selectedLayer;
    }

    public void Deselect()
    {
        if (currentOutline != null)
            Destroy(currentOutline);
        selectedObject = null;
        selectedLayer = null;
        currentOutline = null;
    }

    private GameObject GetRootObject(GameObject obj)
    {
        return obj.transform.root.gameObject;
    }

    private void AddOutline(GameObject target)
    {
        currentOutline = target.GetComponent<Outline>();
        if (currentOutline == null)
            currentOutline = target.AddComponent<Outline>();
        currentOutline.effectColor = Color.yellow;
    }


    public void ClearSelection()
    {
        if (selectedObject != null)
        {
            var outline = selectedObject.GetComponent<Outline>();
            if (outline != null)
                Destroy(outline);
        }

        foreach (GameObject obj in previouslyHighlighted)
        {
            var outline = obj.GetComponent<Outline>();
            if (outline != null)
                Destroy(outline);
        }

        previouslyHighlighted.Clear();
        selectedObject = null;
        selectedLayer = null;
        layerSelectionActive = false;

        RestoreLayerTransparency(); // Restaura os materiais originais
    }


    public void SelectLayerByName(string layerName)
    {
        if (selectedObject == null)
        {
            Debug.LogWarning("Nenhum objeto foi selecionado para escolher uma camada.");
            return;
        }

        foreach (Transform child in selectedObject.transform)
        {
            if (child.name == layerName)
            {
                child.gameObject.SetActive(true);
                selectedLayer = child.gameObject;
                Debug.Log("Camada ativada: " + child.name);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    public GameObject GetInstantiatedObject()
    {
        return selectedObject;
    }


    public ObjectInfo DisplayObjectInfoReal(GameObject obj)
    {
        Vector3 dimensions = GetDimensions(obj);
        Vector3 scale = obj.transform.lossyScale;
        //float volume = GetVolume(obj);
        float area = GetArea(obj);


        Vector3 realDimensions = GetDimensionsRealWorld(obj);
        float realVolume = GetVolumeRealWorld(obj);
        float realArea = GetAreaRealWorld(obj);

        Debug.Log(
            $"INFORMAÇÕES DO OBJETO:\n" +
            $"(Escala atual: {obj.transform.lossyScale})\n\n" +
            $"🔹 Dimensões: {dimensions.x:F2} x {dimensions.y:F2} x {dimensions.z:F2} (unidades reduzidas)\n" +
            $"🔹 Área: {area:F2} (unidades² reduzidas)\n\n" +
            $"📏 Dimensões reais: {realDimensions.x:F2} x {realDimensions.y:F2} x {realDimensions.z:F2} (unidades reais)\n" +
            $"📦 Volume real: {realVolume:F2} (unidades³)\n" +
            $"🧱 Área real: {realArea:F2} (unidades²)"
        );

        return new ObjectInfo(
            obj.name,
            realDimensions,
            realArea,
            scale
        );
    }

    public void ApplyOutline(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            originalMaterial = renderer.material; // Store the original material
        renderer.material = transparentMaterial; // Material com o shader de contorno
        renderer.material = transparentMaterial; // Material com o shader de contorno
    }

    private void ClearLayerHighlight()
    {
        if (selectedLayer != null)
        {
            var outline = selectedLayer.GetComponent<Outline>();
            if (outline != null)
                Destroy(outline);
        }
        selectedLayer = null;
    }

    public bool validLayer(Transform child) // Use este método para validar as tags
    {
        return child.CompareTag("forros") || child.CompareTag("janelas") || child.CompareTag("colunas")
            || child.CompareTag("portas") || child.CompareTag("paredes") || child.CompareTag("pisos")
            || child.CompareTag("telhados") || child.CompareTag("escadas") || child.CompareTag("mobilia")
            || child.CompareTag("paisagismo") || child.CompareTag("eletrico") || child.CompareTag("hidrosanitario")
            || child.CompareTag("estrutural") || child.CompareTag("outros") || child.CompareTag("tubulacao")
            || child.CompareTag("guarda-corpos");
    }

    public void RestoreLayerTransparency()
    {
        if (selectedObject == null) return;

        foreach (Transform child in selectedObject.transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (Material material in renderer.materials)
                {
                    SetMaterialTransparency(material, 1.0f); // Restaura opacidade total
                }
            }
        }
    }

    private void SetMaterialTransparency(Material material, float alpha)
    {
        // Compatível com URP Lit
        if (material.HasProperty("_BaseColor"))
        {
            Color color = material.GetColor("_BaseColor");
            color.a = alpha;
            material.SetColor("_BaseColor", color);

            if (alpha < 1.0f)
            {
                // Transparent
                material.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
                material.SetFloat("_Blend", 0); // Alpha blending
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                // Opaque
                material.SetFloat("_Surface", 0);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_ZWrite", 1);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
        }
        else if (material.HasProperty("_Color"))
        {
            // Fallback para materiais padrão
            Color color = material.color;
            color.a = alpha;
            material.color = color;

            if (alpha < 1.0f)
            {
                material.SetFloat("_Mode", 3); // Transparent
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Mode", 0); // Opaque
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }
        }
        else
        {
            Debug.LogWarning("O material não possui a propriedade '_BaseColor' ou '_Color'.");
        }
    }



    public float GetVolume(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider is BoxCollider boxCollider)
        {
            Vector3 size = boxCollider.size;
            return size.x * size.y * size.z; // Volume de um BoxCollider
        }
        else if (collider is SphereCollider sphereCollider)
        {
            float radius = sphereCollider.radius;
            return (4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3); // Volume de uma esfera
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            float radius = capsuleCollider.radius;
            float height = capsuleCollider.height - 2 * radius;
            return Mathf.PI * Mathf.Pow(radius, 2) * height + (4f / 3f) * Mathf.PI * Mathf.Pow(radius, 3); // Volume de um cilindro com hemisférios
        }
        else
        {
            Debug.LogWarning("Collider não suportado para cálculo de volume.");
            return 0f;
        }
    }
    public Vector3 GetDimensions(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size; // Retorna as dimensões do objeto
        }
        else
        {
            Debug.LogWarning("Renderer não encontrado no objeto.");
            return Vector3.zero;
        }
    }
    public void DisplayObjectInfo(GameObject obj)
    {
        float volume = GetVolume(obj);

        Vector3 dimensions = GetDimensions(obj);

        Debug.Log($"Informações do Objeto: \n" +
                  $"Volume: {volume} unidades cúbicas\n" +
                    $"Área: {GetArea(obj)} unidades quadradas\n" +
                  $"Dimensões: {dimensions.x} x {dimensions.y} x {dimensions.z} unidades");
    }
    public float GetArea(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.x * renderer.bounds.size.y; // Retorna a área do objeto
        }
        else
        {
            Debug.LogWarning("Renderer não encontrado no objeto.");
            return 0f;
        }
    }
    public float GetBoxColliderFaceArea(BoxCollider boxCollider, string face)
    {
        Vector3 size = boxCollider.size;

        switch (face.ToLower())
        {
            case "top":
            case "bottom":
                return size.x * size.z; // Área da face superior ou inferior
            case "front":
            case "back":
                return size.x * size.y; // Área da face frontal ou traseira
            case "left":
            case "right":
                return size.y * size.z; // Área da face lateral
            default:
                Debug.LogWarning("Face inválida especificada.");
                return 0f;
        }
    }

    public Vector3 GetDimensionsRealWorld(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("Nenhum Renderer encontrado no objeto ou em seus filhos.");
            return Vector3.zero;
        }
        Debug.Log($"Encontrados {renderers.Length} Renderers em {obj.name}");


        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 scaledSize = combinedBounds.size;
        Vector3 scale = obj.transform.lossyScale;
        return new Vector3(
            scaledSize.x / scale.x,
            scaledSize.y / scale.y,
            scaledSize.z / scale.z
        );
    }

    public float GetVolumeRealWorld(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider is BoxCollider boxCollider)
        {
            Vector3 size = boxCollider.size;
            Vector3 scale = obj.transform.lossyScale;
            float realX = size.x * scale.x;
            float realY = size.y * scale.y;
            float realZ = size.z * scale.z;
            return realX * realY * realZ;
        }
        // Adapte para SphereCollider e CapsuleCollider se quiser
        Debug.LogWarning("Collider não suportado para cálculo de volume real.");
        return 0f;
    }

    public float GetAreaRealWorld(GameObject obj)
    {
        Vector3 dimensions = GetDimensionsRealWorld(obj);
        return dimensions.x * dimensions.y;

    }




}
