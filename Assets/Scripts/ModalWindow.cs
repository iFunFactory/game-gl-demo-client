using UnityEngine;
using UnityEngine.UI;


// 확인 팝업으로 사용할 modal window
public class ModalWindow : Singleton<ModalWindow>
{
    public delegate void OnClickButton();
    OnClickButton onClickButtonCallback = null;

    public GUIStyle guiAreaBackgroundStyle;
    private const int backgroundTextureSize = 4;
    private Texture2D guiAreaBackgroundTexture;
    private Color guiAreaBackgoundFillColor;

    string title = "";
    string content = "";
    bool opened = false;

    public void Touch() { }

    void Start()
    {
        // create a background area style
        guiAreaBackgroundTexture = new Texture2D(backgroundTextureSize, backgroundTextureSize);
        guiAreaBackgoundFillColor = new Color(0.0f, 0.0f, 0.0f, 0.3f);
        for (int i = 0; i < backgroundTextureSize * backgroundTextureSize; i++)
        {
            guiAreaBackgroundTexture.SetPixel(i % backgroundTextureSize, i / backgroundTextureSize, guiAreaBackgoundFillColor);
        }
        guiAreaBackgroundTexture.Apply();
        guiAreaBackgroundStyle = new GUIStyle(GUIStyle.none);
        guiAreaBackgroundStyle.normal.background = guiAreaBackgroundTexture;
        guiAreaBackgroundStyle.normal.textColor = Color.black;
    }

    public void Open(string title, string content, OnClickButton onClickButtonCallback = null)
    {
        // set title & content
        this.title = title;
        this.content = content;
        // set button callback
        this.onClickButtonCallback = onClickButtonCallback;
        // open modal window
        opened = true;
    }

    private void OnGUI()
    {
        if (opened)
        {
            GUILayout.BeginArea(new Rect(Screen.width / 4, Screen.height / 4, Screen.width / 2, Screen.height / 2), guiAreaBackgroundStyle);
            GUILayout.Label(title);
            GUILayout.Label(content);
            if(GUILayout.Button("OK"))
            {
                if (onClickButtonCallback != null)
                    onClickButtonCallback();
                Close();
            }
            GUILayout.EndArea();
        }
    }

    public void Close()
    {
        opened = false;
    }
}
