using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowFPS : MonoBehaviour {

    public Text fpsText;
    public float updateInterval = 0.5f;

    private float fps;
    private float timer = 0.0f;
    private float frames = 0.0f;

	void Update ()
    {
        frames++;
        timer += Time.deltaTime;
        if (timer > updateInterval)
        {
            fps = frames / updateInterval;
            timer -= updateInterval;
            frames = 0f;

            fpsText.text = "FPS:" + fps.ToString("f2");
        }

	}


}
