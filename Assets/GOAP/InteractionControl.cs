using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteractionControl : MonoBehaviour
{
    [Header("References")]
    public GOAPExample goapExample; // Inspector에서 할당
    public Transform handTransform; // Inspector에서 할당

    [Header("Items")]
    public List<ItemObject> items; // Inspector에서 할당

    [Header("Settings")]
    [Tooltip("아이템이 Place에 자동 할당되기 위한 최대 거리 (단위: 미터)")]
    public float autoAssignDistanceThreshold = 5f;

    // 빠른 아이템 조회를 위한 딕셔너리
    private Dictionary<string, ItemObject> itemDict;

    // NPCState 참조
    private NPCState npcState;

    void Start()
    {
        // GOAPExample 참조 유효성 검사
        if (goapExample == null)
        {
            goapExample = FindObjectOfType<GOAPExample>();
            if (goapExample == null)
            {
                Debug.LogError("GOAPExample 스크립트를 씬에서 찾을 수 없습니다. InteractionControl에서 할당해주세요.");
                return;
            }
            else
            {
                Debug.Log("GOAPExample 참조를 FindObjectOfType를 통해 찾았습니다.");
            }
        }
        else
        {
            Debug.Log("GOAPExample 참조가 Inspector를 통해 할당되었습니다.");
        }

        // handTransform 참조 유효성 검사
        if (handTransform == null)
        {
            Debug.LogError("InteractionControl에서 Hand Transform이 할당되지 않았습니다. Inspector에서 할당해주세요.");
            return;
        }

        // goapExample으로부터 npcState 초기화
        npcState = goapExample.NpcState;
        if (npcState == null)
        {
            Debug.LogError("GOAPExample의 NpcState가 null입니다. GOAPExample이 NpcState를 올바르게 초기화하는지 확인해주세요.");
            return;
        }
        else
        {
            Debug.Log("InteractionControl: GOAPExample에서 npcState를 성공적으로 가져왔습니다.");
        }

        // 아이템 딕셔너리 초기화
        InitializeItemDictionary();

        // 아이템을 초기 장소에 할당
        AssignItemsToPlaces();
    }

    /// <summary>
    /// 빠른 아이템 조회를 위한 딕셔너리를 초기화합니다.
    /// </summary>
    private void InitializeItemDictionary()
    {
        itemDict = new Dictionary<string, ItemObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.itemName))
            {
                Debug.LogWarning("InteractionControl의 아이템 중 itemName이 비어 있습니다. 유효한 이름을 할당해주세요.");
                continue;
            }

            if (item.itemGameObject == null)
            {
                Debug.LogWarning($"아이템 '{item.itemName}'에 GameObject가 할당되지 않았습니다. Inspector에서 할당해주세요.");
                continue;
            }

            if (!itemDict.ContainsKey(item.itemName))
            {
                itemDict.Add(item.itemName, item);
            }
            else
            {
                Debug.LogWarning($"중복된 itemName이 감지되었습니다: '{item.itemName}'. 각 아이템은 고유한 이름을 가져야 합니다.");
            }
        }

        Debug.Log("InteractionControl: 아이템 딕셔너리가 성공적으로 초기화되었습니다.");
    }

    /// <summary>
    /// 아이템을 초기 장소에 할당합니다. Inspector에서 currentPlace가 할당되지 않은 경우, 특정 거리 내에 있는 Place에 자동 할당합니다.
    /// </summary>
    private void AssignItemsToPlaces()
    {
        foreach (var item in itemDict.Values)
        {
            if (item.currentPlace != null)
            {
                // currentPlace가 GOAPExample.Places에 존재하는지 확인
                if (!goapExample.Places.ContainsKey(item.currentPlace.Name))
                {
                    Debug.LogWarning($"아이템 '{item.itemName}'의 Place '{item.currentPlace.Name}'가 GOAPExample.Places에 존재하지 않습니다.");
                    // 자동 할당 시도
                    AssignToClosestPlaceWithinThreshold(item);
                    continue;
                }

                Place place = goapExample.Places[item.currentPlace.Name];
                if (!place.Inventory.Contains(item.itemName))
                {
                    place.Inventory.Add(item.itemName);
                    Debug.Log($"InteractionControl: 아이템 '{item.itemName}'을 Place '{place.Name}'에 할당했습니다.");
                }

                // 아이템의 GameObject 위치를 Place 위치로 설정하지 않고, 현재 위치 유지
                // item.itemGameObject.transform.position = place.GameObject.transform.position;
                // item.itemGameObject.transform.SetParent(null); // 부모 관계 해제
            }
            else
            {
                // Inspector에서 currentPlace가 할당되지 않은 경우, 특정 거리 내에 있는 Place에 자동 할당
                AssignToClosestPlaceWithinThreshold(item);
            }
        }
    }

    /// <summary>
    /// 아이템을 특정 거리 내에 있는 가장 가까운 Place에 자동 할당합니다.
    /// </summary>
    /// <param name="item">할당할 아이템</param>
    private void AssignToClosestPlaceWithinThreshold(ItemObject item)
    {
        Place closestPlace = FindClosestPlace(item.itemGameObject.transform.position, autoAssignDistanceThreshold);
        if (closestPlace != null)
        {
            closestPlace.Inventory.Add(item.itemName);
            item.currentPlace = closestPlace;
            // 아이템의 GameObject 위치를 이동시키지 않고 현재 위치 유지
            // 단, Place의 위치와 아이템의 위치가 근접하게 설정되어야 함
            item.itemGameObject.transform.SetParent(null); // 부모 관계 해제
            Debug.Log($"InteractionControl: 아이템 '{item.itemName}'을 가장 가까운 Place '{closestPlace.Name}'에 자동 할당했습니다.");
        }
        else
        {
            Debug.LogWarning($"아이템 '{item.itemName}'을 할당할 Place를 찾을 수 없습니다. 아이템을 원래 위치에 유지합니다.");
        }
    }

    /// <summary>
    /// 특정 위치에 가장 가까운 Place를 찾습니다. 지정된 거리 이내에 있는지 확인합니다.
    /// </summary>
    /// <param name="position">기준 위치</param>
    /// <param name="distanceThreshold">최대 거리</param>
    /// <returns>가장 가까운 Place 객체, 없으면 null</returns>
    private Place FindClosestPlace(Vector3 position, float distanceThreshold)
    {
        float minDistance = Mathf.Infinity;
        Place closestPlace = null;

        foreach (var place in goapExample.Places.Values)
        {
            if (place.GameObject == null)
            {
                Debug.LogWarning($"Place '{place.Name}'에 GameObject가 할당되지 않았습니다.");
                continue;
            }

            float distance = Vector3.Distance(position, place.GameObject.transform.position);
            Debug.Log($"아이템 위치와 Place '{place.Name}' 간의 거리: {distance}");

            if (distance < minDistance && distance <= distanceThreshold)
            {
                minDistance = distance;
                closestPlace = place;
            }
        }

        if (closestPlace != null)
        {
            Debug.Log($"FindClosestPlace: 가장 가까운 Place는 '{closestPlace.Name}'이며, 거리 {minDistance}입니다.");
        }
        else
        {
            Debug.LogWarning("FindClosestPlace: 가장 가까운 Place를 찾을 수 없습니다.");
        }

        return closestPlace;
    }

    /// <summary>
    /// NPC가 아이템을 집는 동작을 처리합니다.
    /// </summary>
    /// <param name="itemName">집을 아이템의 이름</param>
    /// <param name="npcState">NPC의 상태</param>
    /// <param name="worldState">월드 상태</param>
    public void PickUpItem(string itemName, NPCState npcState, WorldState worldState)
    {
        Debug.Log($"PickUpItem: 아이템 '{itemName}'을 집으려 합니다.");

        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("PickUpItem이 빈 itemName으로 호출되었습니다.");
            return;
        }

        if (!itemDict.TryGetValue(itemName, out ItemObject item))
        {
            Debug.LogError($"PickUpItem: 아이템 '{itemName}'을 InteractionControl에서 찾을 수 없습니다.");
            return;
        }

        if (npcState.Inventory.Contains(itemName))
        {
            Debug.LogWarning($"PickUpItem: NPC가 이미 아이템 '{itemName}'을 인벤토리에 가지고 있습니다.");
            return;
        }

        if (item.currentPlace == null)
        {
            Debug.LogWarning($"PickUpItem: 아이템 '{itemName}'이 어떤 Place에도 할당되지 않았습니다.");
            return;
        }

        // Place의 인벤토리에서 아이템 제거
        Place place = item.currentPlace;
        if (place.Inventory.Contains(itemName))
        {
            place.Inventory.Remove(itemName);
            Debug.Log($"PickUpItem: Place '{place.Name}'에서 아이템 '{itemName}'을 제거했습니다.");
        }
        else
        {
            Debug.LogWarning($"PickUpItem: Place '{place.Name}'에 아이템 '{itemName}'이 존재하지 않습니다.");
        }

        // NPC의 인벤토리에 아이템 추가
        npcState.Inventory.Add(itemName);
        npcState.UpperBody["hold"] = itemName;
        Debug.Log($"PickUpItem: NPC가 아이템 '{itemName}'을 인벤토리에 추가했습니다.");

        // 아이템의 GameObject를 NPC의 손에 부착
        item.itemGameObject.transform.SetParent(handTransform);
        item.itemGameObject.transform.localPosition = Vector3.zero;
        item.itemGameObject.transform.localRotation = Quaternion.identity;
        Debug.Log($"PickUpItem: 아이템 '{itemName}'을 NPC의 손에 부착했습니다.");

        // 아이템의 currentPlace를 null로 설정 (NPC가 소유하게 됨)
        item.currentPlace = null;
        Debug.Log($"PickUpItem: 아이템 '{itemName}'의 currentPlace를 null로 설정했습니다.");
    }

    /// <summary>
    /// NPC가 아이템을 놓는 동작을 처리합니다.
    /// </summary>
    /// <param name="itemName">놓을 아이템의 이름</param>
    /// <param name="npcState">NPC의 상태</param>
    /// <param name="worldState">월드 상태</param>
    public void DropItem(string itemName, NPCState npcState, WorldState worldState)
    {
        Debug.Log($"DropItem: 아이템 '{itemName}'을 놓으려 합니다.");

        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("DropItem이 빈 itemName으로 호출되었습니다.");
            return;
        }

        if (!itemDict.TryGetValue(itemName, out ItemObject item))
        {
            Debug.LogError($"DropItem: 아이템 '{itemName}'을 InteractionControl에서 찾을 수 없습니다.");
            return;
        }

        if (!npcState.Inventory.Contains(itemName))
        {
            Debug.LogWarning($"DropItem: NPC가 인벤토리에 아이템 '{itemName}'을 가지고 있지 않습니다.");
            return;
        }

        // NPC의 인벤토리에서 아이템 제거
        npcState.Inventory.Remove(itemName);
        npcState.UpperBody["hold"] = "none";
        Debug.Log($"DropItem: NPC의 인벤토리에서 아이템 '{itemName}'을 제거했습니다.");

        // 아이템의 GameObject를 손에서 분리
        item.itemGameObject.transform.SetParent(null);
        Debug.Log($"DropItem: 아이템 '{itemName}'을 NPC의 손에서 분리했습니다.");

        // NPC의 현재 위치를 기반으로 Place 결정
        string npcLocation = npcState.LowerBody.ContainsKey("location") ? npcState.LowerBody["location"].ToString() : "unknown";
        if (!worldState.Places.TryGetValue(npcLocation, out Place currentPlace))
        {
            Debug.LogWarning($"DropItem: NPC의 현재 위치 '{npcLocation}'이 WorldState.Places에 존재하지 않습니다.");
            return;
        }

        // Place의 인벤토리에 아이템 추가
        currentPlace.Inventory.Add(itemName);
        item.currentPlace = currentPlace;
        Debug.Log($"DropItem: 아이템 '{itemName}'을 Place '{currentPlace.Name}'의 인벤토리에 추가했습니다.");

        // 아이템의 GameObject 위치를 Place의 위치로 설정하지 않고, 현재 위치 유지
        // item.itemGameObject.transform.position = currentPlace.GameObject.transform.position;
        item.itemGameObject.transform.position = npcLocation == "unknown" ? item.itemGameObject.transform.position : currentPlace.GameObject.transform.position;
        Debug.Log($"DropItem: 아이템 '{itemName}'의 위치를 Place '{currentPlace.Name}'의 위치로 설정했습니다.");
    }

    /// <summary>
    /// NPC가 아이템을 사용하는 동작을 처리합니다.
    /// </summary>
    /// <param name="itemName">사용할 아이템의 이름</param>
    /// <param name="npcState">NPC의 상태</param>
    /// <param name="worldState">월드 상태</param>
    public void UseItem(string itemName, NPCState npcState, WorldState worldState)
    {
        Debug.Log($"UseItem: 아이템 '{itemName}'을 사용하려 합니다.");

        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("UseItem이 빈 itemName으로 호출되었습니다.");
            return;
        }

        if (!itemDict.TryGetValue(itemName, out ItemObject item))
        {
            Debug.LogError($"UseItem: 아이템 '{itemName}'을 InteractionControl에서 찾을 수 없습니다.");
            return;
        }

        if (!npcState.Inventory.Contains(itemName))
        {
            Debug.LogWarning($"UseItem: NPC가 인벤토리에 아이템 '{itemName}'을 가지고 있지 않습니다.");
            return;
        }

        // 아이템의 GameObject에서 IUsableItem 컴포넌트 가져오기
        IUsableItem usableItem = item.itemGameObject.GetComponent<IUsableItem>();
        if (usableItem == null)
        {
            Debug.LogError($"UseItem: 아이템 '{itemName}'에 사용 가능한 스크립트가 첨부되어 있지 않습니다.");
            return;
        }

        // 아이템의 Use 메서드 실행
        usableItem.Use();
        Debug.Log($"UseItem: 아이템 '{itemName}'의 Use() 메서드를 실행했습니다.");

        // 아이템 사용에 따른 효과 처리 (예: NPC의 자원 업데이트 등)
        // 아이템 사용 효과는 별도의 스크립트나 로직에서 처리된다고 가정합니다.

        // 아이템을 사용해도 인벤토리에서 제거하지 않음 (요구사항에 따라 조정 가능)
    }
}
