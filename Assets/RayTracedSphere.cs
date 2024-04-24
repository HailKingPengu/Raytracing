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
    public float emissionStrength;
}