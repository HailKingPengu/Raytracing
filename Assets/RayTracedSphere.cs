using UnityEngine;

public class RayTracedSphere : MonoBehaviour
{
    public RayMaterial material;
}

[System.Serializable]
public struct RayMaterial
{
    public Color colour;
    public Color emissionColour;
    public Color specularColour;
    public float emissionStrength;
    [Range(0f, 1f)]
    public float smoothness;
    [Range(0f, 1f)]
    public float specular;
}