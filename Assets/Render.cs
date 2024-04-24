using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

public struct Sphere
{
    public Vector3 position;
    public float radius;
    public Color colour;
}

public class Render : MonoBehaviour
{

    [SerializeField] bool useShaderInScene;
    [SerializeField] Shader shader;
    Material material;

    List<GameObject> objects;

    ComputeBuffer sphereBuffer;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        bool isSceneCam = Camera.current.name == "SceneCamera";

        if (useShaderInScene)
        {

            if (material == null || (material.shader != shader && shader != null))
            {
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Texture");
                }

                material = new Material(shader);
            }

            UpdateCameraParams(Camera.current);
            CreateSpheres();

            Graphics.Blit(null, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }

    }

    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        material.SetVector("CameraParameters", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
        material.SetMatrix("CamWorldMatrix", cam.transform.localToWorldMatrix);
    }

    void CreateSpheres()
    {
        // Create sphere data from the sphere objects in the scene
        RayTracedSphere[] sphereObjects = FindObjectsOfType<RayTracedSphere>();
        Sphere[] spheres = new Sphere[sphereObjects.Length];

        for (int i = 0; i < sphereObjects.Length; i++)
        {
            spheres[i] = new Sphere()
            {
                position = sphereObjects[i].transform.position,
                radius = sphereObjects[i].transform.localScale.x * 0.5f,
                colour = sphereObjects[i].material
            };
        }

        int length = Math.Max(1, spheres.Count());

        //Debug.Log(spheres[0].position + " " + spheres[0].radius + " " + spheres[0].colour);

        // Create buffer containing all sphere data, and send it to the shader
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere));
        if (sphereBuffer == null || !sphereBuffer.IsValid() || sphereBuffer.count != length || sphereBuffer.stride != stride)
		{
			if (sphereBuffer != null) { sphereBuffer.Release(); }
            sphereBuffer = new ComputeBuffer(length, stride, ComputeBufferType.Structured);
		}

        sphereBuffer.SetData(spheres);
        material.SetBuffer("Spheres", sphereBuffer);
        material.SetInt("NumSpheres", sphereObjects.Length);
    }
}
