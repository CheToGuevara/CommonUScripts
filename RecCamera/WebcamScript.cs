using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading;

public class WebcamScript : MonoBehaviour {

    public int maxFrames =1000; // maximum number of frames you want to record in one video
    public int frameRate = 2; // number of frames to capture per second
    public string persistentDataPath = "./Data";
    private bool recordig = false;


    private WebCamTexture _webCamTex;
    private RenderTexture tempRenderTexture;
    private Texture2D tempTexture2D;


    // The Encoder Thread
    private Thread encoderThread;

    // Timing Data
    private float captureFrameTime;
    private float lastFrameTime;
    private int frameNumber;
    private int savingFrameNumber;

    // Encoder Thread Shared Resources
    private Queue<byte[]> frameQueue;
    private int screenWidth;
    private int screenHeight;
    private bool threadIsProcessing;
    private bool terminateThreadWhenDone;

    private void Start()
    {
        //StartCamera();
    }

    public void PlayRec()
    {
        print("Capturing to: " + persistentDataPath + "/");

        if (!System.IO.Directory.Exists(persistentDataPath))
        {
            System.IO.Directory.CreateDirectory(persistentDataPath);
        }

        recordig = true;
    }

    public void StopRec()
    {

        recordig = false;
    }

    // Use this for initialization
    public void StartCamera() {

       

        _webCamTex = new WebCamTexture();

        RawImage rawImage = gameObject.GetComponent<RawImage>();
        
        if (rawImage)
        {
            
            rawImage.texture = _webCamTex;
        }
        /*Renderer _renderer = GetComponent<Renderer>();
		_renderer.material.mainTexture = _webcamtex;*/
        _webCamTex.Play();

        screenWidth = 640;
        screenHeight = 360;

        tempRenderTexture = new RenderTexture(screenWidth, screenHeight, 0);
        tempTexture2D = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
        frameQueue = new Queue<byte[]>();

        frameNumber = 0;
        savingFrameNumber = 0;

        captureFrameTime = 1.0f / (float)frameRate;
        Debug.Log(captureFrameTime);
        lastFrameTime = Time.time;

        // Kill the encoder thread if running from a previous execution
        if (encoderThread != null && (threadIsProcessing || encoderThread.IsAlive))
        {
            threadIsProcessing = false;
            encoderThread.Join();
        }

        threadIsProcessing = true;
        encoderThread = new Thread(EncodeAndSave);
        encoderThread.Start();
    }

    private void Update()
    {
        if (recordig)
            RenderLoop();
    }

    void DestroyCamera()
    {


        if (_webCamTex)
            _webCamTex.Stop();
    }

    private void OnDestroy()
    {
        DestroyCamera();
    }

    void RenderLoop()
    {
        if (frameNumber <= maxFrames)
        {

            // Check if render target size has changed, if so, terminate


            // Calculate number of video frames to produce from this game frame
            // Generate 'padding' frames if desired framerate is higher than actual framerate
            float thisFrameTime = Time.time;
            int framesToCapture = ((int)(thisFrameTime / captureFrameTime)) - ((int)(lastFrameTime / captureFrameTime));

            // Capture the frame
            if (framesToCapture > 0)
            {
                Graphics.Blit(_webCamTex, tempRenderTexture);

                RenderTexture.active = tempRenderTexture;
                tempTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                RenderTexture.active = null;
            }

            // Add the required number of copies to the queue
            for (int i = 0; i < framesToCapture && frameNumber <= maxFrames; ++i)
            {
                frameQueue.Enqueue(tempTexture2D.GetRawTextureData());

                frameNumber++;

                if (frameNumber % frameRate == 0)
                {
                    print("Frame " + frameNumber);
                }
            }

            lastFrameTime = thisFrameTime;

        }
        else //keep making screenshots until it reaches the max frame amount
        {
            // Inform thread to terminate when finished processing frames
            terminateThreadWhenDone = true;

            // Disable script
            this.enabled = false;
        }


    }



    private void EncodeAndSave()
    {
        print("SCREENRECORDER IO THREAD STARTED");

        while (threadIsProcessing)
        {
            if (!recordig)
            {
                Thread.Sleep(500);
                continue;
            }
            if (frameQueue.Count > 0)
            {
                // Generate file path
                string path= persistentDataPath + "frame" + savingFrameNumber + ".jpg";
                

                // Dequeue the frame, encode it as a bitmap, and write it to the file
                using (FileStream fileStream = new FileStream(path, FileMode.Create))
                {
                    BitmapEncoder.WriteBitmap(fileStream, screenWidth, screenHeight, frameQueue.Dequeue());
                    fileStream.Close();
                }

                // Done
                savingFrameNumber++;
                print("Saved " + savingFrameNumber + " frames. " + frameQueue.Count + " frames remaining.");
            }
            else
            {
                if (terminateThreadWhenDone)
                {
                    break;
                }

                Thread.Sleep(1);
            }
        }

        terminateThreadWhenDone = false;
        threadIsProcessing = false;

        print("SCREENRECORDER IO THREAD FINISHED");
    }


}


class BitmapEncoder
{
    public static void WriteBitmap(Stream stream, int width, int height, byte[] imageData)
    {
        using (BinaryWriter bw = new BinaryWriter(stream))
        {

            // define the bitmap file header
            bw.Write((UInt16)0x4D42);                               // bfType;
            bw.Write((UInt32)(14 + 40 + (width * height * 4)));     // bfSize;
            bw.Write((UInt16)0);                                    // bfReserved1;
            bw.Write((UInt16)0);                                    // bfReserved2;
            bw.Write((UInt32)14 + 40);                              // bfOffBits;

            // define the bitmap information header
            bw.Write((UInt32)40);                               // biSize;
            bw.Write((Int32)width);                                 // biWidth;
            bw.Write((Int32)height);                                // biHeight;
            bw.Write((UInt16)1);                                    // biPlanes;
            bw.Write((UInt16)32);                                   // biBitCount;
            bw.Write((UInt32)0);                                    // biCompression;
            bw.Write((UInt32)(width * height * 4));                 // biSizeImage;
            bw.Write((Int32)0);                                     // biXPelsPerMeter;
            bw.Write((Int32)0);                                     // biYPelsPerMeter;
            bw.Write((UInt32)0);                                    // biClrUsed;
            bw.Write((UInt32)0);                                    // biClrImportant;

            // switch the image data from RGB to BGR
            for (int imageIdx = 0; imageIdx < imageData.Length; imageIdx += 3)
            {
                bw.Write(imageData[imageIdx + 2]);
                bw.Write(imageData[imageIdx + 1]);
                bw.Write(imageData[imageIdx + 0]);
                bw.Write((byte)255);
            }

        }
    }

}
