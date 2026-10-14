using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 바닥 메시를 골라 구역 트리거 볼륨을 만들어 주는 에디터 도구.
// 바닥 렌더러의 바운드를 그대로 쓰므로 박스 크기를 손으로 맞출 필요가 없다.
public static class ZoneSetup
{
    private const float VolumeHeight = 2.6f;

    // 바닥면보다 살짝 아래에서 볼륨을 시작한다. CharacterController는 Skin Width만큼 떠서 움직인다.
    private const float FloorOverlap = 0.1f;

    private const string ZonesRootName = "Zones";
    private const string ZoneDataFolder = "Assets/ZoneData";
    private const string PrefabFolder = "Assets/Prefabs";

    // 바닥 오브젝트 이름 → 구역 이름 추측값. 틀리면 만들어진 ZoneData에서 고치면 된다.
    private static readonly string[][] NameTable =
    {
        new[] { "Bedroom", "방" },
        new[] { "Studio", "서재" },
        new[] { "Living", "거실" },
        new[] { "Kitchen", "주방" },
        new[] { "Bathroom", "욕실" },
        new[] { "Ceramic", "현관" },
        new[] { "Relax", "휴게실" },
        new[] { "Courtyard", "중정" },
        new[] { "Exterior", "테라스" },
    };

    [MenuItem("Tools/구역 설정/선택한 바닥으로 구역 만들기")]
    private static void CreateFromSelection()
    {
        ZoneInfoPanel panel = FindPanel();
        if (panel == null)
        {
            Debug.LogError("씬에 ZoneInfoPanel이 없습니다. [Tools/구역 설정/정보 패널 씬에 올리기]를 먼저 실행하세요.");
            return;
        }

        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            Debug.LogError("하이어라키에서 바닥 오브젝트(Floor_...)를 먼저 선택하세요.");
            return;
        }

        Transform zonesRoot = GetZonesRoot();
        int made = 0;

        foreach (GameObject floor in selection)
        {
            Renderer floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer == null)
            {
                Debug.LogWarning(floor.name + ": Renderer가 없어 건너뜁니다. 바닥 메시를 선택하세요.", floor);
                continue;
            }

            BuildZone(floor, floorRenderer.bounds, zonesRoot, panel);
            made++;
        }

        if (made == 0)
        {
            return;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"구역 {made}개를 설정했습니다. 씬을 저장하세요(Ctrl+S).");
    }

    [MenuItem("Tools/구역 설정/정보 패널 씬에 올리기")]
    private static void AddPanelToScene()
    {
        if (FindPanel() != null)
        {
            Debug.Log("씬에 이미 ZoneInfoPanel이 있습니다.");
            return;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null || prefab.GetComponentInChildren<ZoneInfoPanel>(true) == null)
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(instance, "정보 패널 올리기");
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log(path + "를 씬에 올렸습니다. 씬을 저장하세요(Ctrl+S).", instance);
            return;
        }

        Debug.LogError(PrefabFolder + " 안에서 ZoneInfoPanel이 들어 있는 프리팹을 찾지 못했습니다.");
    }

    [MenuItem("Tools/구역 설정/씬 점검")]
    private static void Inspect()
    {
        ZoneInfoPanel[] panels = Object.FindObjectsByType<ZoneInfoPanel>(FindObjectsInactive.Include);

        if (panels.Length == 0)
        {
            Debug.LogError("ZoneInfoPanel: 씬에 없습니다.");
        }
        else if (panels.Length > 1)
        {
            Debug.LogWarning($"ZoneInfoPanel이 {panels.Length}개 있습니다. ZoneTrigger는 그중 하나만 잡으니 하나만 남기세요.");
        }

        foreach (ZoneInfoPanel found in panels)
        {
            if (found.GetComponent<Canvas>() != null)
            {
                Debug.LogWarning(
                    found.name + ": 캔버스 루트에 ZoneInfoPanel이 붙어 있습니다. " +
                    "패널을 숨길 때 캔버스 전체가 사라지니, 패널 오브젝트로 옮겨 주세요.",
                    found);
            }
            else
            {
                Debug.Log("ZoneInfoPanel: " + found.name + " 확인됨", found);
            }
        }

        ZoneTrigger[] triggers = Object.FindObjectsByType<ZoneTrigger>(FindObjectsInactive.Include);
        if (triggers.Length == 0)
        {
            Debug.LogWarning("씬에 ZoneTrigger가 없습니다.");
            return;
        }

        foreach (ZoneTrigger trigger in triggers)
        {
            List<string> problems = new List<string>();

            ZoneData data = ReadZoneData(trigger);
            if (data == null)
            {
                problems.Add("Zone Data 비었음");
            }

            bool hasTrigger = false;
            foreach (Collider zoneCollider in trigger.GetComponents<Collider>())
            {
                if (!zoneCollider.isTrigger)
                {
                    continue;
                }

                hasTrigger = true;

                Vector3 size = zoneCollider.bounds.size;
                if (size.x <= 0.01f || size.y <= 0.01f || size.z <= 0.01f)
                {
                    problems.Add($"볼륨 두께가 0에 가까움 {size}");
                }
            }

            if (!hasTrigger)
            {
                problems.Add("Is Trigger 콜라이더 없음");
            }

            string label = data != null ? trigger.name + " (" + data.ZoneName + ")" : trigger.name;

            if (problems.Count == 0)
            {
                Debug.Log(label + ": 확인됨", trigger);
            }
            else
            {
                Debug.LogWarning(label + ": " + string.Join(", ", problems), trigger);
            }
        }
    }

    private static void BuildZone(GameObject floor, Bounds bounds, Transform zonesRoot, ZoneInfoPanel panel)
    {
        string volumeName = floor.name + "_Zone";
        Transform existing = zonesRoot.Find(volumeName);
        GameObject volume;

        if (existing != null)
        {
            volume = existing.gameObject;
            Undo.RecordObject(volume.transform, "구역 볼륨 갱신");
        }
        else
        {
            volume = new GameObject(volumeName);
            Undo.RegisterCreatedObjectUndo(volume, "구역 볼륨 만들기");
            volume.transform.SetParent(zonesRoot, false);
        }

        // 회전·스케일 없는 오브젝트에 월드 바운드를 그대로 옮긴다. 바닥의 회전을 따라갈 필요가 없다.
        volume.transform.SetPositionAndRotation(
            new Vector3(
                bounds.center.x,
                bounds.max.y - FloorOverlap + VolumeHeight * 0.5f,
                bounds.center.z),
            Quaternion.identity);
        volume.transform.localScale = Vector3.one;

        BoxCollider box = volume.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = Undo.AddComponent<BoxCollider>(volume);
        }
        else
        {
            Undo.RecordObject(box, "구역 볼륨 갱신");
        }

        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = new Vector3(bounds.size.x, VolumeHeight, bounds.size.z);

        ZoneTrigger trigger = volume.GetComponent<ZoneTrigger>();
        ZoneData data = ReadZoneData(floor.GetComponent<ZoneTrigger>());

        if (data == null)
        {
            data = ReadZoneData(trigger);
        }

        if (data == null)
        {
            data = FindOrCreateZoneData(floor.name, bounds);
        }

        if (trigger == null)
        {
            trigger = Undo.AddComponent<ZoneTrigger>(volume);
        }

        string guess = GuessZoneName(floor.name);
        if (guess != null && data.ZoneName != guess)
        {
            Debug.LogWarning(
                $"{volumeName}: 이미 연결돼 있던 ZoneData가 '{data.ZoneName}'입니다. " +
                $"바닥 이름으로는 '{guess}'인데 맞는지 확인하세요.",
                data);
        }

        SerializedObject serialized = new SerializedObject(trigger);
        serialized.FindProperty("zoneData").objectReferenceValue = data;
        serialized.FindProperty("zoneInfoPanel").objectReferenceValue = panel;
        serialized.ApplyModifiedProperties();

        RemoveOldTrigger(floor);

        Debug.Log($"{volumeName}: {data.ZoneName} / 가로 {box.size.x:0.0}m x 세로 {box.size.z:0.0}m", volume);
    }

    // 바닥 메시에 직접 붙여 둔 옛 트리거를 정리한다.
    private static void RemoveOldTrigger(GameObject floor)
    {
        ZoneTrigger old = floor.GetComponent<ZoneTrigger>();
        if (old == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(old);

        foreach (Collider floorCollider in floor.GetComponents<Collider>())
        {
            // 솔리드 콜라이더는 손대지 않는다. 바닥이 뚫리면 안 된다.
            if (floorCollider.isTrigger)
            {
                Undo.DestroyObjectImmediate(floorCollider);
            }
        }

        Debug.Log(floor.name + ": 바닥에 직접 붙어 있던 트리거를 정리했습니다.", floor);
    }

    private static ZoneData ReadZoneData(ZoneTrigger trigger)
    {
        if (trigger == null)
        {
            return null;
        }

        SerializedObject serialized = new SerializedObject(trigger);
        return serialized.FindProperty("zoneData").objectReferenceValue as ZoneData;
    }

    private static ZoneData FindOrCreateZoneData(string floorName, Bounds bounds)
    {
        string zoneName = GuessZoneName(floorName) ?? floorName;

        foreach (string guid in AssetDatabase.FindAssets("t:ZoneData"))
        {
            ZoneData found = AssetDatabase.LoadAssetAtPath<ZoneData>(AssetDatabase.GUIDToAssetPath(guid));
            if (found != null && found.ZoneName == zoneName)
            {
                return found;
            }
        }

        if (!AssetDatabase.IsValidFolder(ZoneDataFolder))
        {
            AssetDatabase.CreateFolder("Assets", "ZoneData");
        }

        ZoneData data = ScriptableObject.CreateInstance<ZoneData>();
        string path = AssetDatabase.GenerateUniqueAssetPath(ZoneDataFolder + "/" + zoneName + ".asset");
        AssetDatabase.CreateAsset(data, path);

        // 필드가 private이라 ZoneTrigger를 채울 때와 같은 방식으로 넣는다.
        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("zoneName").stringValue = zoneName;
        serialized.FindProperty("area").floatValue = Mathf.Round(bounds.size.x * bounds.size.z * 10f) / 10f;
        serialized.FindProperty("description").stringValue = string.Empty;
        serialized.ApplyModifiedProperties();

        Debug.Log($"ZoneData 생성: {path} (전용면적 {data.Area}m² 자동 계산). 설명은 직접 채워 주세요.", data);
        return data;
    }

    private static string GuessZoneName(string floorName)
    {
        foreach (string[] row in NameTable)
        {
            if (floorName.IndexOf(row[0], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return row[1];
            }
        }

        return null;
    }

    private static ZoneInfoPanel FindPanel()
    {
        return Object.FindAnyObjectByType<ZoneInfoPanel>(FindObjectsInactive.Include);
    }

    private static Transform GetZonesRoot()
    {
        GameObject root = GameObject.Find(ZonesRootName);
        if (root == null)
        {
            root = new GameObject(ZonesRootName);
            Undo.RegisterCreatedObjectUndo(root, "Zones 만들기");
        }

        return root.transform;
    }
}
