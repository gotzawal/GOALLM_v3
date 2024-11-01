using UnityEngine;

public class LookAt : MonoBehaviour
{
    [Header("Transforms")]
    public Transform head; // 캐릭터의 머리 Transform
    public Transform lookAtTarget; // 카메라나 타겟 Transform

    [Header("Settings")]
    public float rotationSpeed = 5f; // 회전 속도
    public float maxHeadTurnAngle = 60f; // 수평 회전 최대 각도
    public float maxHeadTiltAngle = 30f; // 수직 기울기 최대 각도

    // 현재 머리의 회전 각도
    private float currentYaw = 0f; // 수평 회전 (좌우)
    private float currentPitch = 0f; // 수직 회전 (위아래)

    void LateUpdate()
    {
        if (head == null || lookAtTarget == null)
            return;

        // 타겟까지의 방향 벡터 계산 (타겟 방향을 구하기 위해 head의 위치에서 lookAtTarget의 위치를 뺌)
        Vector3 directionToTarget = lookAtTarget.position - head.position;

        // 머리의 로컬 좌표계에서의 방향 계산 (머리의 로컬 축이 Z 축을 향하지 않을 때를 고려)
        Vector3 localDirection = head.InverseTransformDirection(directionToTarget);

        // 수평 및 수직 각도 계산
        float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg; // 좌우(Yaw) 각도
        float targetPitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg; // 위아래(Pitch) 각도

        // 각도 제한 적용
        targetYaw = Mathf.Clamp(targetYaw, -maxHeadTurnAngle, maxHeadTurnAngle);
        targetPitch = Mathf.Clamp(targetPitch, -maxHeadTiltAngle, maxHeadTiltAngle);

        // 현재 각도에서 목표 각도로 부드럽게 보간
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * rotationSpeed);
        currentPitch = Mathf.LerpAngle(currentPitch, targetPitch, Time.deltaTime * rotationSpeed);

        // 머리 회전 적용
        head.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }
}
