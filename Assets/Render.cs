using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class Render : MonoBehaviour
{

    [SerializeField] bool useShaderInScene;
    [SerializeField] Shader shader;
    Material material;

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
}
