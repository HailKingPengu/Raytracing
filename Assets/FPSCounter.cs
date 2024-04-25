using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField]
    TMP_Text text;

    [SerializeField]
    bool captureFPS;
    int frames;
    float time;

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.C))
            captureFPS = !captureFPS;

        if (captureFPS)
        {
            frames++;
            time += Time.deltaTime;
            text.text = "FPS: " + (frames / time);
        }
        else
        {
            frames = 0;
            time = 0;
            text.text = "FPS: - ";
        }
    }
}
