using UnityEngine;

// 바라보고 상호작용 키를 누르면 반응하는 오브젝트의 공통 틀.
public abstract class Interactable : MonoBehaviour
{
    // 조준했을 때 화면에 뜨는 문구. 상태에 따라 바뀐다(문 열기 / 문 닫기).
    public abstract string Prompt { get; }

    public abstract void Interact();
}
