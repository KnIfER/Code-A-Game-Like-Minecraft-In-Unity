using UnityEngine;
using System.Collections;

public class ShowFPS_OnGUI : MonoBehaviour
{
    public float fpsMeasuringDelta = -1f;

    private float timePassed;
    private int m_FrameCount = 0;
    private float m_FPS = 0.0f;
    GUIStyle font;

    private void Start()
    {        
        Application.runInBackground = true;
        timePassed = 0.0f;
        font = new GUIStyle();
        font.normal.background = null;    //这是设置背景填充的
        font.fontSize = 35;       //当然，这是字体大小
    }

    private void Update()
    {
        m_FrameCount = m_FrameCount + 1;
        timePassed = timePassed + Time.deltaTime;

        if (timePassed > fpsMeasuringDelta)
        {
            //m_FPS = 1.0f / Time.smoothDeltaTime;
            m_FPS = m_FrameCount / timePassed;

            timePassed = 0.0f;
            m_FrameCount = 0;
        }
    }

    int lastShame = 0;

    private void OnGUI()
    {
        //Debug.Log("OnGui");

        int now = (int)m_FPS;
        if (lastShame >= 0)
        {
            now = lastShame;
            lastShame = -1;
        }
        if (m_FPS < 20)
        {
            lastShame = now;
        }
        font.normal.textColor = new Color(m_FPS>60?0:1.0f, m_FPS>60?1:(m_FPS<20?0:0.5f), 0);   //设置字体颜色的
        //居中显示FPS
        // GUI.Label(new Rect((Screen.width / 2) - 40, 0, 200, 200), "FPS: " + m_FPS, bb);
        // (Screen.width / 2) - 40
        //if(now<100)
        GUI.Label(new Rect(25, 8, 200, 200), string.Format("FPS:{0}", now), font);
    }
}
