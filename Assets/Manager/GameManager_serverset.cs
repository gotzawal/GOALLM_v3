using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class GameManagerServerSet : MonoBehaviour
{
    public TMP_InputField userInputField;
    public TextMeshProUGUI chatHistoryText;
    public UnityEngine.AI.NavMeshAgent npcAgent;
    public GOAPExample goapExample;

    // 서버 URL을 미리 지정
    private string serverUrl = "https://28c2-2a09-bac1-3f00-518-00-22-114.ngrok-free.app"; // 여기에 실제 서버 URL을 입력
    private bool isServerUrlSet = true; // 서버 URL 입력을 건너뛰기 위해 true로 설정

    [System.Serializable]
    public class ServerRequest
    {
        public NPCStatus npc_status;
        public string userInput;

        public ServerRequest(NPCStatus status, string input)
        {
            npc_status = status;
            userInput = input;
        }
    }

    [System.Serializable]
    private class ServerResponse
    {
        public string TalkGoal;
        public string MoveGoal;
        public string ItemGoal;
        public string Gesture;
        public string Think;
    }

    void Start()
    {
        userInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        chatHistoryText.text = "Start chatting"; // 서버 URL 입력 대신 바로 채팅 시작 문구 출력
    }

    void OnInputFieldSubmit(string input)
    {
        if (isServerUrlSet) // 서버 URL을 이미 설정한 상태
        {
            if (!IsInvoking("CommunicateWithServer"))
            {
                StartCoroutine(CommunicateWithServer(input));
            }
        }
    }

    IEnumerator CommunicateWithServer(string userInput)
    {
        // 사용자 입력을 즉시 표시
        UpdateChatHistoryWithUserInput(userInput);

        // 현재 NPC 상태를 가져와서 ServerRequest 생성
        ServerRequest request = new ServerRequest(goapExample.CurrentNPCStatus, userInput);

        Debug.Log("NPC Status: " + goapExample.CurrentNPCStatus);

        string jsonRequest = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(serverUrl + "/api/game", "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
                UpdateChatHistoryWithNPCTalk("Error.");
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(jsonResponse);
                Debug.Log(response.TalkGoal);
                UpdateChatHistoryWithNPCTalk(response.TalkGoal);

                if (goapExample != null)
                {
                    Debug.Log(response.Gesture);
                    Debug.Log(response.MoveGoal);
                    Debug.Log(response.ItemGoal);
                    goapExample.SetGoals(response.Gesture, response.MoveGoal, response.ItemGoal);
                }
                else
                {
                    Debug.LogError("GOAPExample reference is not set in GameManager.");
                }

                userInputField.text = ""; // 입력 필드 초기화
            }
        }
    }

    void UpdateChatHistoryWithUserInput(string userInput)
    {
        // 사용자 입력을 즉시 표시
        chatHistoryText.text += "\nUser: " + userInput;
    }

    void UpdateChatHistoryWithNPCTalk(string talkGoal)
    {
        // 서버 응답이 온 후 NPC 대사를 표시
        chatHistoryText.text += "\nNPC: " + talkGoal;
    }
}
