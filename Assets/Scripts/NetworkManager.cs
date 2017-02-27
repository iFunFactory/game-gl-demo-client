using Fun;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using funapi.service.multicast_message;
using UnityEngine.SceneManagement;

public class NetworkManager : Singleton<NetworkManager>
{
    // Options
    public bool sessionReliability = false;

    public bool sequenceValidation = false;
    public int sendingCount = 10;

    public string myId { get; private set; }
    public string playerSessionId { get; private set; }

    private enum STATE
    {
        START,      // session is started (not initialized, not connected, not logined)
        INITED,     // session initialized
        REDIRECTING,// session is redirecting now
        READY,      // session is ready
        CLOSED,     // session closed
        ERROR,      // error occurred
    }
    private STATE state;
    private FunapiSession session = null;

    private FunapiSession chatSession = null;
    private FunapiChatClient chatClient = null;

    private const string serverAddrTxtFilename = "serverInfo.txt";
    private string loginServerAddr = "13.124.23.90";
    private ushort loginServerPort = 8012;
    private string chatServerAddr = "13.124.23.90";
    private ushort chatServerPort = 8052;

    public void Touch() { }    // for singleton initialization

    private void Awake()
    {
        // uid를 구해서 ID로 쓴다
#if UNITY_EDITOR
        myId = SystemInfo.deviceUniqueIdentifier + "_Editor";
#else
        myId = SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceType;
#endif
        string serverIniFilePath = Path.Combine(Application.dataPath, serverAddrTxtFilename);
        if (File.Exists(serverIniFilePath))
        {
            string log;
            using (StreamReader sr = File.OpenText(serverIniFilePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    line = line.Trim();
                    string[] datas = line.Split(':');
                    if(datas.Length!=2)
                    {
                        log = "'serverInfo.txt' parsing failed.  ";
                        foreach (string data in datas)
                            log += data + " ";
                        Debug.LogWarning(log);
                        continue;
                    }
                    else
                    {
                        switch(datas[0])
                        {
                            case "loginServerAddr":
                                loginServerAddr = datas[1];
                                break;
                            case "loginServerPort":
                                if (!ushort.TryParse(datas[1], out loginServerPort))
                                {
                                    log = "'serverInfo.txt' parsing failed.  ";
                                    foreach (string data in datas)
                                        log += data + " ";
                                    Debug.LogWarning(log);
                                }
                                break;
                            case "chatServerAddr":
                                chatServerAddr = datas[1];
                                break;
                            case "chatServerPort":
                                if (!ushort.TryParse(datas[1], out chatServerPort))
                                {
                                    log = "'serverInfo.txt' parsing failed.  ";
                                    foreach (string data in datas)
                                        log += data + " ";
                                    Debug.LogWarning(log);
                                }
                                break;
                            default:
                                log = "'serverInfo.txt' parsing failed.  ";
                                foreach (string data in datas)
                                    log += data + " ";
                                Debug.LogWarning(log);
                                break;
                        }
                    }
                }
            }
        }
    }

    private void Start()
    {
    }

    // 네트워크 초기화
    public void Connect()
    {
        if (session != null && session.Connected)
            return;

        state = STATE.START;

        if (session == null)
        {
            session = FunapiSession.Create(loginServerAddr, sessionReliability);
            session.SessionEventCallback += OnSessionEvent;
            session.TransportEventCallback += OnTransportEvent;
            session.ReceivedMessageCallback += OnReceive;
        }

        GameLogic.Instance.Log("Connecting to login server..");

        TcpTransportOption tcp_option = new TcpTransportOption();
        tcp_option.Encryption = EncryptionType.kDefaultEncryption;
        tcp_option.AutoReconnect = false;
        tcp_option.SetPing(5, 30, false);
        session.Connect(TransportProtocol.kTcp, FunEncoding.kJson, loginServerPort, tcp_option);
    }

    public void ConnectChat()
    {
        if (chatSession != null && chatSession.Connected)
            return;
        chatSession = FunapiSession.Create(chatServerAddr, false);
        chatSession.SessionEventCallback += OnChatSessionCallback;
        chatSession.Connect(TransportProtocol.kTcp, FunEncoding.kJson, chatServerPort);
    }

    public bool IsReady
    {
        get { return state == STATE.READY; }
    }

    public void Stop()
    {
        if (session != null)
        {
            session.Stop();
            session = null;
        }
    }

    public void Send(string messageType, Dictionary<string, object> body = null,
                      TransportProtocol protocol = TransportProtocol.kDefault)
    {
        if (body == null)
            body = new Dictionary<string, object>();
        session.SendMessage(messageType, body, protocol);
        if (!messageType.Equals("relay"))
            GameLogic.Instance.Log("'" + messageType + "' message sent.");
    }

    public void SendChat(string message)
    {
        if (chatClient != null)
            chatClient.SendText("global", message);
    }

    private void OnChatSessionCallback(SessionEventType type, string session_id)
    {
        switch(type)
        {
            case SessionEventType.kOpened:
                chatClient = new FunapiChatClient(chatSession, FunEncoding.kJson);

                chatClient.sender = myId;

                // 채널 목록을 받았을 때 호출되는 콜백입니다.
                chatClient.ChannelListCallback += delegate (object channel_list) {
                };

                // Player가 채널에 입장하면 호출되는 콜백입니다.
                chatClient.JoinedCallback += delegate (string channel_id, string sender) {
                    GameLogic.Instance.chatText = "'" + sender + "' comes.\n" + GameLogic.Instance.chatText;
                };

                // Player가 채널에서 퇴장하면 호출되는 콜백입니다.
                chatClient.LeftCallback += delegate (string channel_id, string sender) {
                    GameLogic.Instance.chatText = "'" + sender + "' leaves.\n" + GameLogic.Instance.chatText;
                    if (sender.Equals(myId))
                    {
                        chatClient.JoinChannel("global", ChattingMessageCallback);
                    }
                };

                // 에러가 발생했을 때 알림을 받는 콜백입니다.
                // 에러 종류는 enum FunMulticastMessage.ErrorCode 타입을 참고해주세요.
                chatClient.ErrorCallback += delegate (string channel_id, FunMulticastMessage.ErrorCode code) {
                    GameLogic.Instance.chatText = "[ERROR] " + code + "\n" + GameLogic.Instance.chatText;
                };

                chatClient.JoinChannel("global", ChattingMessageCallback);
                break;
        }
    }

    private void ChattingMessageCallback(string channel_id, string sender, string text)
    {
        GameLogic.Instance.chatText = "'" + sender.Substring(0, 8) + "': " + text + "\n" + GameLogic.Instance.chatText;
    }

    // session 이벤트 처리
    private void OnSessionEvent(SessionEventType type, string session_id)
    {
        switch (type)
        {
            case SessionEventType.kOpened:
                state = STATE.INITED;
                GameLogic.Instance.Log("Session opened, let's login.");
                // 세션이 생성되면, 바로 로그인 한다.
                Dictionary<string, object> body = new Dictionary<string, object>();
                body["uid"] = myId;
                UnityChanControlScriptWithRgidBody.myCharacter.AddPositionInfoToDictionary(body);
                Send("login", body);
                break;
            case SessionEventType.kClosed:
            case SessionEventType.kRedirectFailed:
                state = STATE.CLOSED;
                ModalWindow.Instance.Open("Network Error", "Event type is '" + type + "'", AppUtil.Quit);
                break;
            case SessionEventType.kRedirectSucceeded:
                break;
            case SessionEventType.kRedirectStarted:
                state = STATE.REDIRECTING;
                break;
        }
    }

    // transport 이벤트 처리
    private void OnTransportEvent(TransportProtocol protocol, TransportEventType type)
    {
        switch (type)
        {
            case TransportEventType.kDisconnected:
                // 연결이 끊기면 재연결
                session.Connect(protocol);
                break;
            case TransportEventType.kConnectionFailed:
            case TransportEventType.kConnectionTimedOut:
                // 연결에 실패함
                ModalWindow.Instance.Open("Connection failure!", "Cannot connect to server, Please restart game.\n" + type.ToString(), AppUtil.Quit);
                break;
        }
    }

    // 메세지 핸들러
    private void OnReceive(string msg_type, object body)
    {
        Dictionary<string, object> message = body as Dictionary<string, object>;

        switch (msg_type)
        {
            case "field":
                {
                    int fieldIndex = 1;
                    if (int.TryParse(message["field_index"].ToString(), out fieldIndex))
                    {
                        switch(fieldIndex)
                        {
                            default:
                            case 1:
                                SceneManager.LoadScene("Scenes/Field");
                                break;
                            case 2:
                                SceneManager.LoadScene("Scenes/Field2");
                                break;
                        }
                    }
                }
                break;
            case "login":
                // 로그인 실패
                state = STATE.ERROR;
                ModalWindow.Instance.Open("Login failure", message["msg"].ToString(), AppUtil.Quit);
                break;
            case "welcome":
                // 서버 이동시 도착하는 웰컴 메세지, 타입에 따라 맞는 Scene을 로드한다
                if (!message.ContainsKey("server_type"))
                {
                    state = STATE.ERROR;
                    ModalWindow.Instance.Open("Error", "Server redirecting message doesn't contain server type.", AppUtil.Quit);
                    return;
                }
                // 서버에 맞는 Scene을 로드
                switch(message["server_type"] as string)
                {
                    case "field":
                        GameLogic.Instance.Log("Field server connected.");
                        UnityChanControlScriptWithRgidBody.wait = false;
                        UnityChanControlScriptWithRgidBody.myCharacter.SetPosition(message);
                        if (message.ContainsKey("Level"))
                            int.TryParse(message["Level"].ToString(), out GameLogic.Instance.Level);
                        state = STATE.READY;
                        GameLogic.Instance.SetFieldState(GameLogic.FIELDSTATE.MAIN);
                        break;
                    case "instance":
                        GameLogic.Instance.Log("Instance server connected.");
                        GameLogic.Instance.SetInstanceState(GameLogic.INSTANCESTATE.MAIN);
                        break;
                }
                break;
            case "new":
                if (SceneManager.GetActiveScene().name.Contains("Field"))
                {
                    GameLogic.Instance.Log("New character comes.");
                    GameLogic.Instance.NewCharacter(message);
                }
                break;
            case "remove":
                if (SceneManager.GetActiveScene().name.Contains("Field"))
                {
                    GameLogic.Instance.Log("A character leaves.");
                    GameLogic.Instance.RemoveCharacter(message);
                }
                break;
            case "relay":
                if (SceneManager.GetActiveScene().name.Contains("Field"))
                    GameLogic.Instance.UpdateOtherCharacter(message);
                break;
            case "error":
                state = STATE.ERROR;
                if(message.ContainsKey("msg"))
                    ModalWindow.Instance.Open("Error", message["msg"] as string, AppUtil.Quit);
                else
                    ModalWindow.Instance.Open("Error", "Unknown error! Please report to us." + message.ToString(), AppUtil.Quit);
                break;
            default:
                Debug.LogWarning("Unknown message type: " + msg_type);
                break;
        }
    }
}