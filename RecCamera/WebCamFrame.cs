using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WebCamFrame : MonoBehaviour
{

    WebCamTexture _webcamtex;
    public Camera camToMatch;

    void Awake()
    {
       
        _webcamtex = new WebCamTexture();
        Renderer _renderer = GetComponent<Renderer>();
        _renderer.material.mainTexture = _webcamtex;
        _webcamtex.Play();
    }

    // Use this for initialization
    void Start()
    {
        // The camera must be Orthographic for this to work.
        if (camToMatch == null || !camToMatch.orthographic)
        {
            return;
        }

        transform.localScale = Vector3.one;
        Renderer rend = GetComponent<Renderer>();
        Vector3 baseSize = rend.bounds.size;
        Vector3 camSize = baseSize;
        camSize.y = camToMatch.orthographicSize * 2;
        camSize.x = camSize.y * camToMatch.aspect;

        // This makes use of the Vector3Extensions.ComponentDivide extension method
        Vector3 scale = camSize.ComponentDivide(baseSize);

        transform.localScale = scale;
    }

    void OnDestroy()
    {
        _webcamtex.Stop();
    }
}
