using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 문/조명 상호작용을 씬에 붙여 주는 에디터 도구.
// 이 에셋은 충돌 메시가 Collision_* 로 분리돼 있어서 문에 콜라이더가 없다.
// 레이캐스트로 맞히려면 콜라이더를 따로 붙여야 한다.
public static class InteractionSetup
{
    [MenuItem("Tools/상호작용 설정/선택한 오브젝트를 문으로 만들기")]
    private static void MakeDoors()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            Debug.LogError("하이어라키에서 문 오브젝트를 먼저 선택하세요.");
            return;
        }

        int made = 0;

        foreach (GameObject target in selection)
        {
            Renderer doorRenderer = target.GetComponent<Renderer>();
            if (doorRenderer == null)
            {
                Debug.LogWarning(target.name + ": Renderer가 없어 건너뜁니다.", target);
                continue;
            }

            ClearBlockingStatic(target);
            EnsureCollider(target);

            DoorInteractable door = target.GetComponent<DoorInteractable>();
            if (door == null)
            {
                door = Undo.AddComponent<DoorInteractable>(target);
            }

            ApplyHinge(door, target.transform, doorRenderer.bounds);
            made++;
        }

        if (made == 0)
        {
            return;
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"문 {made}개를 설정했습니다. 반대로 열리면 Open Angle 부호를, 경첩이 반대쪽이면 Hinge Local Point 부호를 뒤집으세요.");
    }

    [MenuItem("Tools/상호작용 설정/선택한 오브젝트를 조명 스위치로 만들기")]
    private static void MakeLightSwitches()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            Debug.LogError("하이어라키에서 스위치로 쓸 오브젝트를 먼저 선택하세요.");
            return;
        }

        int made = 0;

        foreach (GameObject target in selection)
        {
            if (target.GetComponentInChildren<Light>(true) == null)
            {
                Debug.LogWarning(
                    target.name + ": 자식에 Light가 없습니다. 컴포넌트를 붙인 뒤 Lights 칸을 직접 채워 주세요.",
                    target);
            }

            EnsureCollider(target);

            if (target.GetComponent<LightSwitchInteractable>() == null)
            {
                Undo.AddComponent<LightSwitchInteractable>(target);
            }

            made++;
        }

        if (made == 0)
        {
            return;
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"조명 스위치 {made}개를 설정했습니다.");
    }

    [MenuItem("Tools/상호작용 설정/플레이어에 상호작용 붙이기")]
    private static void AddInteractorToPlayer()
    {
        CameraMovement player = Object.FindAnyObjectByType<CameraMovement>(FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogError("씬에서 CameraMovement를 찾지 못했습니다.");
            return;
        }

        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null)
        {
            interactor = Undo.AddComponent<PlayerInteractor>(player.gameObject);
        }

        // 카메라를 직접 꽂아 둔다. 이 씬은 카메라가 플레이어의 형제라 자동 탐색에 기대면 위험하다.
        Camera view = player.GetComponentInChildren<Camera>(true);
        if (view == null && player.transform.parent != null)
        {
            view = player.transform.parent.GetComponentInChildren<Camera>(true);
        }

        SerializedObject serialized = new SerializedObject(interactor);
        serialized.FindProperty("viewCamera").objectReferenceValue = view;
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log(
            player.name + "에 PlayerInteractor를 붙였습니다. 카메라: " + (view != null ? view.name : "못 찾음(직접 넣어 주세요)"),
            player);
    }

    [MenuItem("Tools/상호작용 설정/안내 문구 UI 만들기")]
    private static void CreatePromptUI()
    {
        InteractionPrompt existing = Object.FindAnyObjectByType<InteractionPrompt>(FindObjectsInactive.Include);
        if (existing != null)
        {
            FixFont(existing);
            Debug.Log("씬에 이미 InteractionPrompt가 있습니다.", existing);
            Selection.activeObject = existing;
            return;
        }

        Canvas canvas = PickCanvas();
        if (canvas == null)
        {
            Debug.LogError("문구를 올릴 캔버스를 찾지 못했습니다. Info_Canvas를 먼저 씬에 올려 주세요.");
            return;
        }

        GameObject go = new GameObject("InteractionPrompt", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "안내 문구 UI 만들기");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(400f, 48f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = "[E] 문 열기";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;

        // TMP 기본 폰트(LiberationSans)에는 한글 글리프가 없어서 네모로 나온다.
        TMP_FontAsset korean = FindKoreanFont(canvas);
        if (korean != null)
        {
            label.font = korean;
        }
        else
        {
            Debug.LogWarning("캔버스에서 한글이 되는 폰트를 찾지 못했습니다. Font Asset을 직접 넣어 주세요.");
        }

        go.AddComponent<InteractionPrompt>();

        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeObject = go;
        Debug.Log(canvas.name + " 아래에 안내 문구 UI를 만들었습니다. 위치와 글꼴은 취향대로 바꾸세요.", go);
    }

    [MenuItem("Tools/상호작용 설정/상호작용 점검")]
    private static void Inspect()
    {
        PlayerInteractor interactor = Object.FindAnyObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        if (interactor == null)
        {
            Debug.LogError("PlayerInteractor: 씬에 없습니다. [플레이어에 상호작용 붙이기]를 실행하세요.");
        }
        else
        {
            Debug.Log("PlayerInteractor: " + interactor.name + " 확인됨", interactor);
        }

        InteractionPrompt prompt = Object.FindAnyObjectByType<InteractionPrompt>(FindObjectsInactive.Include);
        if (prompt == null)
        {
            Debug.LogWarning("InteractionPrompt: 씬에 없습니다. 안내 문구 없이도 동작하지만 뭘 누를 수 있는지 알기 어렵습니다.");
        }
        else
        {
            Debug.Log("InteractionPrompt: " + prompt.name + " 확인됨", prompt);
        }

        Interactable[] targets = Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include);
        if (targets.Length == 0)
        {
            Debug.LogWarning("씬에 Interactable이 없습니다.");
            return;
        }

        foreach (Interactable target in targets)
        {
            List<string> problems = new List<string>();

            bool hasSolidCollider = false;
            foreach (Collider targetCollider in target.GetComponentsInChildren<Collider>())
            {
                if (!targetCollider.isTrigger)
                {
                    hasSolidCollider = true;
                    break;
                }
            }

            if (!hasSolidCollider)
            {
                problems.Add("콜라이더 없음(레이캐스트에 안 맞음)");
            }

            if (target is DoorInteractable &&
                (GameObjectUtility.GetStaticEditorFlags(target.gameObject) & StaticEditorFlags.BatchingStatic) != 0)
            {
                problems.Add("Batching Static 켜짐(움직여도 화면이 안 바뀜)");
            }

            if (target is LightSwitchInteractable && target.GetComponentInChildren<Light>(true) == null)
            {
                problems.Add("Light 없음");
            }

            if (problems.Count == 0)
            {
                Debug.Log($"{target.name}: 확인됨 ({target.Prompt})", target);
            }
            else
            {
                Debug.LogWarning($"{target.name}: " + string.Join(", ", problems), target);
            }
        }
    }

    private static void EnsureCollider(GameObject target)
    {
        foreach (Collider existing in target.GetComponents<Collider>())
        {
            if (!existing.isTrigger)
            {
                return;
            }
        }

        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            MeshCollider mesh = Undo.AddComponent<MeshCollider>(target);
            mesh.sharedMesh = filter.sharedMesh;
            return;
        }

        Undo.AddComponent<BoxCollider>(target);
    }

    // 피벗이 이미 문 가장자리면 그대로 쓰고, 한가운데면 가장자리를 경첩으로 추천한다.
    private static void ApplyHinge(DoorInteractable door, Transform doorTransform, Bounds bounds)
    {
        bool wideOnX = bounds.size.x >= bounds.size.z;
        float halfWidth = (wideOnX ? bounds.size.x : bounds.size.z) * 0.5f;

        float pivotOffset = wideOnX
            ? doorTransform.position.x - bounds.center.x
            : doorTransform.position.z - bounds.center.z;

        SerializedObject serialized = new SerializedObject(door);
        SerializedProperty hinge = serialized.FindProperty("hingeLocalPoint");

        // 피벗이 가장자리에서 10cm 안쪽이면 경첩으로 본다.
        if (Mathf.Abs(Mathf.Abs(pivotOffset) - halfWidth) <= 0.1f)
        {
            hinge.vector3Value = Vector3.zero;
            serialized.ApplyModifiedProperties();
            Debug.Log(door.name + ": 피벗이 문 가장자리에 있어 그대로 경첩으로 씁니다.", door);
            return;
        }

        Vector3 edge = bounds.center;
        if (wideOnX)
        {
            edge.x -= halfWidth;
        }
        else
        {
            edge.z -= halfWidth;
        }

        edge.y = doorTransform.position.y;

        hinge.vector3Value = doorTransform.InverseTransformPoint(edge);
        serialized.ApplyModifiedProperties();

        Debug.LogWarning(
            door.name + ": 피벗이 문 한가운데라 한쪽 가장자리를 경첩으로 잡았습니다. " +
            "열리는 쪽이 반대면 Hinge Local Point의 부호를 뒤집으세요.",
            door);
    }

    // 시작 화면 캔버스는 체험 시작과 함께 꺼진다. 그 밑에 두면 문구가 영영 안 보인다.
    private static Canvas PickCanvas()
    {
        ZoneInfoPanel panel = Object.FindAnyObjectByType<ZoneInfoPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            Canvas owner = panel.GetComponentInParent<Canvas>(true);
            if (owner != null)
            {
                return owner;
            }
        }

        foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (candidate.GetComponentInChildren<StartCanvas>(true) != null)
            {
                continue;
            }

            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay || candidate.renderMode == RenderMode.ScreenSpaceCamera)
            {
                return candidate;
            }
        }

        return null;
    }

    // 캔버스가 이미 쓰고 있는 폰트를 따라간다. 한글이 들어간 UI면 한글 폰트를 쓰고 있을 것이다.
    private static TMP_FontAsset FindKoreanFont(Canvas canvas)
    {
        if (canvas == null)
        {
            return null;
        }

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font != null && text.font.HasCharacter('가'))
            {
                return text.font;
            }
        }

        return null;
    }

    private static void FixFont(InteractionPrompt prompt)
    {
        TMP_Text label = prompt.GetComponentInChildren<TMP_Text>(true);
        if (label == null || (label.font != null && label.font.HasCharacter('가')))
        {
            return;
        }

        Canvas owner = prompt.GetComponentInParent<Canvas>(true);
        TMP_FontAsset korean = FindKoreanFont(owner != null ? owner : PickCanvas());

        if (korean == null)
        {
            Debug.LogWarning(prompt.name + ": 한글이 되는 폰트를 찾지 못했습니다. Font Asset을 직접 넣어 주세요.", prompt);
            return;
        }

        Undo.RecordObject(label, "한글 폰트로 교체");
        label.font = korean;
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log(prompt.name + ": 한글이 네모로 나오던 폰트를 " + korean.name + "으로 교체했습니다.", label);
    }

    // Batching Static이 켜져 있으면 로드할 때 메시가 합쳐지면서 월드 좌표가 구워진다.
    // 그러면 Transform을 돌려도 화면은 그대로다. 움직일 오브젝트에서는 꺼야 한다.
    private static void ClearBlockingStatic(GameObject target)
    {
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
        StaticEditorFlags blocking = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic;

        if ((flags & blocking) == 0)
        {
            return;
        }

        Undo.RecordObject(target, "Static 해제");
        GameObjectUtility.SetStaticEditorFlags(target, flags & ~blocking);

        Debug.Log(
            target.name + ": Batching Static을 껐습니다. 켜진 채로는 문이 움직여도 화면이 바뀌지 않습니다.",
            target);
    }
}
