using UnityEngine;

public class ObjectInfo
{
    public string Name;
    public Vector3 Dimensions, Scale;  // Largura, altura, profundidade
    public float Volume;
    public float Area;

    public ObjectInfo(string name, Vector3 dimensions, float volume, float area, Vector3 scale)
    {
        Name = name;
        Dimensions = dimensions;
        Volume = volume;
        Area = area;
        Scale = scale;
    }
}
