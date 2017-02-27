using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : Singleton<GameLogic>
{
    public enum FIELDSTATE
    {
        WAIT,
        MAIN,
        NONE,
    }
    public FIELDSTATE fieldState;

    public enum INSTANCESTATE
    {
        WAIT,
        MAIN,
        NONE,
    }
    public INSTANCESTATE instanceState;

    public Camera IntroCamera;
    private Vector3 IntroCameraMovement;

    private Dictionary<string, OtherPlayersUnityChan> otherPlayersDictionary = new Dictionary<string, OtherPlayersUnityChan>();
    private GameObject otherCharacterPrefab;

    public int Level = 1;

    private bool IsLogWindowEnabled = true;
    public string logText = "";

    public string chatText = "";
    public string chatInputText = "";

    public void Touch() { }    // for singleton initialization

    private void Start()
    {
        // global setting
        Application.runInBackground = true;

        // load prefab
        otherCharacterPrefab = Resources.Load<GameObject>("otherPlayersUnityChan");

        // init. variable
        otherPlayersDictionary.Clear();
        IntroCamera = gameObject.AddComponent<Camera>();

        // init. state
        SetFieldState(FIELDSTATE.WAIT);
        UnityChanControlScriptWithRgidBody.wait = true;

        NetworkManager.Instance.Connect();
        NetworkManager.Instance.ConnectChat();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Field")
        {
            switch (fieldState)
            {
                case FIELDSTATE.WAIT:
                    IntroCamera.transform.Translate(IntroCameraMovement * Time.deltaTime);
                    if ((IntroCameraMovement.y < 0 && IntroCamera.transform.localPosition.y <= 40) ||
                        (IntroCameraMovement.y > 0 && IntroCamera.transform.localPosition.y >= 60))
                    {
                        IntroCameraMovement *= -1;
                    }
                    break;
            }
        }
    }

    public void Log(string log)
    {
        if (!log.EndsWith("\n"))
            log += "\n";
        logText = log + logText;
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name == "Instance")
        {
            if (instanceState == INSTANCESTATE.MAIN)
            {
                GUILayout.BeginArea(new Rect(Screen.width / 3, Screen.height / 3, Screen.width / 3, Screen.height / 3));
                if (GUILayout.Button("Kill the Golem!"))
                {
                    SetInstanceState(INSTANCESTATE.WAIT);
                    GolemAnimationController.Instance.PlayAnimation("die");
                }
                GUILayout.EndArea();
            }
        }

        // name, level
        GUILayout.BeginArea(new Rect(0, 0, Screen.width, 24), ModalWindow.Instance.guiAreaBackgroundStyle);
        GUILayout.Label("LV: " + Level + "  ID: " + NetworkManager.Instance.myId, GUILayout.Width(Screen.width), GUILayout.Height(20));
        GUILayout.EndArea();

        // log window
        if (IsLogWindowEnabled)
        {
            GUILayout.BeginArea(new Rect(2, 28, Screen.width / 3, Screen.height / 3), ModalWindow.Instance.guiAreaBackgroundStyle);
            GUI.Label(new Rect(2, 0, Screen.width / 3, Screen.height / 3 - 20), logText);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(2, Screen.height / 3 + 30, Screen.width / 3, 32), ModalWindow.Instance.guiAreaBackgroundStyle);
            if (GUILayout.Button("hide log", GUILayout.Width(Screen.width / 3 - 10), GUILayout.Height(24)))
            {
                IsLogWindowEnabled = false;
            }
            GUILayout.EndArea();
        }
        else
        {
            GUILayout.BeginArea(new Rect(2, 28, 110, 32), ModalWindow.Instance.guiAreaBackgroundStyle);
            if (GUILayout.Button("show log", GUILayout.Width(100), GUILayout.Height(24)))
            {
                IsLogWindowEnabled = true;
            }
            GUILayout.EndArea();
        }

        // chatting window
        GUILayout.BeginArea(new Rect(2, Screen.height - Screen.height / 3 - 2, Screen.width / 3, Screen.height / 3), ModalWindow.Instance.guiAreaBackgroundStyle);
        GUI.SetNextControlName("chatting textfield");
        chatInputText = GUILayout.TextField(chatInputText, GUILayout.Width(Screen.width / 3 - 8), GUILayout.Height(20));
        GUI.Label(new Rect(2, 20, Screen.width / 3, Screen.height / 3 - 20), chatText);
        GUILayout.EndArea();
        if (Event.current.isKey)
        {
            if (Event.current.keyCode == KeyCode.Return)
            {
                if (GUI.GetNameOfFocusedControl().Equals("chatting textfield"))
                {
                    if (chatInputText.Length > 0)
                    {
                        NetworkManager.Instance.SendChat(chatInputText);
                        chatInputText = "";
                    }
                }
                else
                {
                    GUI.FocusControl("chatting textfield");
                }
            }
        }
    }

    public void SetFieldState(FIELDSTATE state)
    {
        fieldState = state;
        switch(fieldState)
        {
            case FIELDSTATE.WAIT:
                IntroCamera.transform.localPosition = new Vector3(9, 60, 41);
                IntroCamera.transform.Rotate(30, 30, 0);
                IntroCameraMovement = new Vector3(0, -4, 0);
                IntroCamera.enabled = true;
                GameObject.Find("Main Camera").GetComponent<Camera>().enabled = false;
                break;
            case FIELDSTATE.MAIN:
                IntroCamera.enabled = false;
                GameObject.Find("Main Camera").GetComponent<Camera>().enabled = true;
                break;
        }
    }

    public void SetInstanceState(INSTANCESTATE state)
    {
        instanceState = state;
        switch(instanceState)
        {
            case INSTANCESTATE.WAIT:
                IntroCamera.transform.localPosition = new Vector3(1, 6, 0);
                IntroCamera.transform.localRotation = new Quaternion(0, 0, 0, 0);
                IntroCameraMovement = new Vector3(0, -4, 0);
                IntroCamera.enabled = true;
                GameObject.Find("Main Camera").GetComponent<Camera>().enabled = false;
                break;
            case INSTANCESTATE.MAIN:
                IntroCamera.enabled = false;
                GameObject.Find("Main Camera").GetComponent<Camera>().enabled = true;
                break;
        }
    }

    public void NewCharacter(Dictionary<string, object> message)
    {
        if (!message.ContainsKey("Name"))
        {
            Debug.LogWarning("[NewCharacter] message doesn't contain name." + message.ToString());
            return;
        }

        GameObject newCharGameObject = Instantiate(otherCharacterPrefab);
        OtherPlayersUnityChan newChar = newCharGameObject.GetComponent<OtherPlayersUnityChan>();
        if (message.ContainsKey("x") && message.ContainsKey("y") && message.ContainsKey("z"))
        {
            float x, y, z;
            if (float.TryParse(message["x"].ToString(), out x) && float.TryParse(message["y"].ToString(), out y) && float.TryParse(message["z"].ToString(), out z))
                newCharGameObject.transform.localPosition = new Vector3(x, y, z);
        }
        Animator anim = newCharGameObject.GetComponent<Animator>();
        if (anim != null)
        {
            float v, h;
            if (message.ContainsKey("v") && float.TryParse(message["v"].ToString(), out v))
                anim.SetFloat("Speed", v);
            if (message.ContainsKey("h") && float.TryParse(message["h"].ToString(), out h))
                anim.SetFloat("Direction", h);
        }
        newChar.Name = (string)message["Name"];
        newChar.idTextMesh.text = newChar.Name.Substring(0, 8);
        otherPlayersDictionary[newChar.Name] = newChar;
        Debug.Log("[NewCharacter] " + newChar.Name + "created");
    }

    public void RemoveCharacter(Dictionary<string, object> message)
    {
        if (!message.ContainsKey("Name"))
        {
            Debug.LogWarning("[RemoveCharacter] message doesn't contain name." + message);
            return;
        }
        if (!otherPlayersDictionary.ContainsKey((string)message["Name"]))
        {
            Debug.LogWarning("[RemoveCharacter] player dictionary doesn't contain '" + (string)message["Name"] + "'");
            return;
        }
        Destroy(otherPlayersDictionary[(string)message["Name"]].gameObject);
        otherPlayersDictionary.Remove((string)message["Name"]);
        Debug.Log("[RemoveCharacter] " + (string)message["Name"] + " remved.");
    }

    public void UpdateOtherCharacter(Dictionary<string, object> message)
    {
        if (!message.ContainsKey("Name"))
        {
            Debug.LogWarning("[UpdateOtherCharacter] message doesn't contain Name.\n" + message);
            return;
        }
        if (!otherPlayersDictionary.ContainsKey((string)message["Name"]))
        {
            Debug.LogWarning("[UpdateOtherCharacter] player dictionary doesn't contain '" + (string)message["Name"] + "'");
            NewCharacter(message);
        }
        otherPlayersDictionary[(string)message["Name"]].SetPosition(message);
    }

    public void ClearOtherPlayersDictionary()
    {
        otherPlayersDictionary.Clear();
    }
}
