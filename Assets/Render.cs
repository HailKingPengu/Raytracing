using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Sphere
{
    public Vector3 position;
    public float radius;
    public RayMaterial material;
}

public class Render : MonoBehaviour
{

    [SerializeField] int maxBounceCount;
    [SerializeField] int numRaysPerPixel;
    int currentFrame;

    [SerializeField] bool isRendering;
    int isRenderingI;
    int renderedFrames;

    [SerializeField] Color skyColourHorizon;
    [SerializeField] Color skyColourZenith;

    [SerializeField] float antiAliasingStrength;

    [SerializeField] bool useShader;
    [SerializeField] Shader shader;
    Material material;
    RenderTexture previousFrame;

    List<GameObject> objects;

    ComputeBuffer sphereBuffer;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        if (previousFrame == null)
        {
            previousFrame = new RenderTexture(source.width, source.height, 0);
            previousFrame.enableRandomWrite = true;
        }

        //for seed
        currentFrame++;

        bool isSceneCam = Camera.current.name == "SceneCamera";

        if (useShader)
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

            material.SetInt("MaxBounceCount", maxBounceCount);
            material.SetInt("NumRaysPerPixel", numRaysPerPixel);
            material.SetInt("CurrentFrame", currentFrame);

            material.SetColor("SkyColourHorizon", skyColourHorizon);
            material.SetColor("SkyColourZenith", skyColourZenith);

            material.SetFloat("BlurStrength", antiAliasingStrength);
            material.SetVector("CamRight", Camera.current.transform.right);
            material.SetVector("CamUp", Camera.current.transform.up);

            material.SetInt("IsRendering", isRenderingI);
            material.SetInt("RenderedFrames", renderedFrames);

            //RenderTexture prevFrameCopy = RenderTexture.GetTemporary(source.width, source.height, 0);
            //Graphics.Blit(previousFrame, prevFrameCopy);
            material.SetTexture("PrevFrame", previousFrame);

            if (material == null || (material.shader != shader && shader != null))
            {
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Texture");
                }

                material = new Material(shader);
            }

            CreateSpheres();

            Graphics.Blit(null, destination, material);
            Graphics.Blit(destination, previousFrame);

            //RenderTexture.ReleaseTemporary(prevFrameCopy);
        }
        else
        {
            Graphics.Blit(source, destination);
        }

        if (isRendering)
        {
            renderedFrames++;
            isRenderingI = 1;
        }
        else
        {
            renderedFrames = 0;
            isRenderingI = 0;
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
                material = sphereObjects[i].material
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            isRendering = !isRendering;
    }

    public void SetNumRays(string input)
    {
        numRaysPerPixel = int.Parse(input);
    }

    public void SetMaxBounces(string input)
    {
        maxBounceCount = int.Parse(input);
    }
}
