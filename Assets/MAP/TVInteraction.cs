using UnityEngine;

public class TVInteraction : PlaceInteraction
{
    private Renderer tvRenderer;
    private Material tvMaterial; // TV 색상을 담당하는 Material

    void Start()
    {
        tvRenderer = GetComponent<Renderer>();
        if (tvRenderer == null)
        {
            Debug.LogError("TVInteraction: Renderer 컴포넌트를 찾을 수 없습니다.");
        }
        else
        {
            // materials 배열에서 두 번째 Material을 선택 (인덱스는 0부터 시작)
            if (tvRenderer.materials.Length > 1)
            {
                tvMaterial = tvRenderer.materials[1];
                Debug.Log("TVInteraction: TV Material이 성공적으로 초기화되었습니다.");
            }
            else
            {
                Debug.LogError("TVInteraction: 두 번째 Material을 찾을 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// tv_state 키의 변경에 따라 TV의 색상을 변경합니다.
    /// </summary>
    /// <param name="key">키 이름</param>
    /// <param name="value">새로운 값</param>
    public override void OnStateChanged(string key, object value)
    {
        if (key.Equals("tv_state", System.StringComparison.OrdinalIgnoreCase))
        {
            string state = value.ToString().ToLower();
            switch (state)
            {
                case "on":
                    ChangeColor(Color.green); // TV 켜짐: 녹색
                    break;
                case "off":
                    ChangeColor(Color.red); // TV 꺼짐: 빨간색
                    break;
                default:
                    Debug.LogWarning($"TVInteraction: 알 수 없는 tv_state '{state}'");
                    break;
            }
        }
    }

    /// <summary>
    /// TV의 색상을 변경합니다.
    /// </summary>
    /// <param name="color">새로운 색상</param>
    private void ChangeColor(Color color)
    {
        if (tvMaterial != null)
        {
            tvMaterial.color = color;
            Debug.Log($"TVInteraction: TV 색상을 {color}으로 변경했습니다.");
        }
        else
        {
            Debug.LogError("TVInteraction: TV Material이 초기화되지 않았습니다.");
        }
    }
}

