using UnityEngine;
using System.Collections.Generic; // Para usar List, se você quiser listas de dados

// Isso permite criar assets deste ScriptableObject no menu "Create" do Unity
[CreateAssetMenu(fileName = "NewObjectInfoModel2", menuName = "AR Project/Object Data Information")]
public class ObjectInfoData : ScriptableObject
{
    [Tooltip("ID única que será lida do QR Code ou associada ao objeto 3D.")]
    public string objectID; // Ex: "PROJETO_RESIDENCIAL_001", "ESTRUTURA_METALICA_A"

    [Tooltip("Nome amigável do objeto.")]
    public string objectName;

    [Tooltip("Texto com todas as informações técnicas formatadas para exibição na UI.")]
    [TextArea(5, 15)] // Permite múltiplas linhas no Inspector
    public string technicalInfoFormattedText;

    [Tooltip("Caminho relativo para o arquivo PDF dentro da pasta StreamingAssets (ex: 'docs/manual_planta1.pdf').")]
    public string pdfPathInStreamingAssets; // Novo campo

    // public float area;
    // public Vector3 dimensions;
    // public int numberOfRooms;
    // public List<string> materialsUsed;
}