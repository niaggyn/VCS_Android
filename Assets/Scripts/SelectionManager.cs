using System;
using System.Collections.Generic;
using UnityEngine;


using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    private GameObject selectedObject;       // Objeto inteiro selecionado
    private GameObject selectedLayer;        // Camada selecionada
    private bool layerSelectionActive = false; // Indica se o modo de seleção de camada está ativo

    private List<GameObject> previouslyHighlighted = new List<GameObject>();
    private Outline currentOutline;
    private Material originalMaterial; // Store the original material
    public Material highlightMaterial; // Material with the outline shader
    public Material transparentMaterial; // Material with the outline shader

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }



    public void SelectLayer(GameObject layer)
    {
        ClearLayerHighlight();
        // Oculta outras camadas (somente se estiver no modo camada)
        if (selectedObject == null)
        {
            Debug.LogWarning("Nenhum objeto foi selecionado para escolher uma camada.");
            return;
        }
        selectedLayer = layer;


        foreach (Transform child in selectedObject.transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (child.gameObject == layer)
                {
                    renderer.material = transparentMaterial; // Mantém destaque
                }
                else if (validLayer(child))
                {
                    renderer.material = transparentMaterial; // Aplicar transparência
                }
            }
        }

        Debug.Log("Camada selecionada: " + layer.name);
    }

    public void EnableLayerSelection()
    {
        layerSelectionActive = true;
        // Ativa todas as camadas visíveis novamente para o usuário escolher
        SetAllLayersActive(true);
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

    public bool HasSelection()
    {
        return selectedObject != null;
    }

    public bool IsLayerSelectionActive()
    {
        return layerSelectionActive;
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

    private void HighlightObject(GameObject obj, Color color)
    {
        var outline = obj.GetComponent<Outline>();
        if (outline == null)
            outline = obj.AddComponent<Outline>();

        outline.effectColor = color;
        previouslyHighlighted.Add(obj);
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
    public void SetAllLayersActive(bool active)
    {
        if (selectedObject != null)
        {
            foreach (Transform child in selectedObject.transform)
            {
                child.gameObject.SetActive(validLayer(child));

                /*
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        SetMaterialTransparency(mat, 1.0f); // Totalmente opaco
                    }
                }
                */
            }
        }

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

            /*
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (Material material in renderer.materials)
                {
                    if (child.name == layerName)
                    {
                        // Destaca a camada selecionada (restaura opacidade total)
                        SetMaterialTransparency(material, 1.0f); // Totalmente opaco
                        selectedLayer = child.gameObject;
                        Debug.Log("Camada ativada: " + child.name);
                    }
                    else
                    {
                        // Torna as outras camadas transparentes
                        SetMaterialTransparency(material, 0.3f); // Transparência ajustada
                    }
                }
            }
            */
        }
    }

    private bool validLayer(Transform child)
    {
        return child.tag == "paredes" || child.tag == "telhados" || child.tag == "pisos"
            || child.tag == "janelas" || child.tag == "portas" || child.tag == "forros" || child.tag == "colunas"
        ;
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


}
