using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public TMP_InputField userInputField;
    public TextMeshProUGUI chatHistoryText;
    public UnityEngine.AI.NavMeshAgent npcAgent;
    public GOAPExample goapExample;

    private string serverUrl = "";
    private bool isServerUrlSet = false;

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
        chatHistoryText.text = "Enter Server URL";
    }

    void OnInputFieldSubmit(string input)
    {
        if (!isServerUrlSet)
        {
            serverUrl = input.TrimEnd('/');
            isServerUrlSet = true;
            chatHistoryText.text = "Start chatting";
            userInputField.text = "";
        }
        else
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
