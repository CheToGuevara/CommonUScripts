using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GifRender : MonoBehaviour
{

    public Image ImgToRender = null;
    public Sprite[] frames;
    public float framesPerSecond = 10;

    void Update()
    {

        int index = Mathf.RoundToInt(Time.time * framesPerSecond);
        index = index % frames.Length;
        ImgToRender.sprite = frames[index]; 

    // Update is called once per frame
    
        
    }
}
