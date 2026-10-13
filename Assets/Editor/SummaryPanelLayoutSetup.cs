using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 요약 패널 표를 Vertical Layout Group 구조로 바꿔 주는 도구.
// 줄 위치를 손으로 계산하지 않아도 되도록 RowContainer를 만들고 틀을 그 안에 넣는다.
public static class SummaryPanelLayoutSetup
{
    private const string PrefabPath = "Assets/Prefabs/Summary_Canvas.prefab";
    private const string PopupName = "SummaryPopup";
    private const string ContainerName = "RowContainer";

    // 왼쪽부터 열이 놓일 X. 이름표 + 값 4칸 기준이고, 헤더 위치와 같은 값이다.
    private static readonly float[] ColumnX = { 0f, 130f, 260f, 390f, 700f };

    private const float RowHeight = 30f;
    private const float RowGap = 20f;
    private const float ContainerX = 40f;
    private const float ContainerY = -185f;
    private const float ContainerWidth = 820f;
    private const float PopupHeight = 520f;

    [MenuItem("Tools/요약 패널 레이아웃 적용")]
    private static void Apply()
    {
        if (!EditorUtility.DisplayDialog(
                "요약 패널 레이아웃",
                "Summary_Canvas 프리팹을 Vertical Layout Group 구조로 바꿉니다.\n\n" +
                "· RowContainer를 만들고 틀을 그 안으로 옮깁니다\n" +
                "· 틀에 Image/Button을 붙여 줄 클릭을 켭니다\n" +
                "· 더 이상 쓰지 않는 Living 줄과 구분선은 끕니다\n\n" +
                "프리팹 모드는 닫은 상태에서 실행하세요.",
                "적용", "취소"))
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError(PrefabPath + " 를 열지 못했습니다.");
            return;
        }

        try
        {
            if (!EditPrefab(root))
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();
        WireScenePanel();

        Debug.Log("요약 패널 레이아웃을 적용했습니다. 씬을 저장하세요.");
    }

    private static bool EditPrefab(GameObject root)
    {
        Transform popup = Find(root.transform, PopupName);
        if (popup == null)
        {
            Debug.LogError(PopupName + " 을 찾지 못했습니다.");
            return false;
        }

        RectTransform container = BuildContainer(popup);

        SummaryRow template = root.GetComponentInChildren<SummaryRow>(true);
        if (template == null)
        {
            // 아직 틀이 없으면 현관 줄 텍스트를 모아서 만든다.
            template = BuildTemplate(popup, container);
            if (template == null)
            {
                return false;
            }
        }

        MoveTemplateInto(template, container);
        LayOutColumns(template);
        EnsureRowButton(template);

        // 구역 구분이 없어졌으니 머리말도 바꾼다.
        SetText(popup, "ZoneHeader", " 옵션");
        SetText(popup, "OptionHeader", " 옵션");
        SetText(popup, "LightHeader", "커튼");

        // 줄이 늘어나는 만큼 팝업을 키워 둬야 아래 버튼과 겹치지 않는다.
        RectTransform popupRect = popup as RectTransform;
        if (popupRect != null && popupRect.sizeDelta.y < PopupHeight)
        {
            popupRect.sizeDelta = new Vector2(popupRect.sizeDelta.x, PopupHeight);
        }

        // 현재 줄은 이제 코드가 만든다. 남겨 두면 옛 글자가 그대로 보인다.
        Deactivate(popup, "LivingZoneText", "LivingFloorText", "LivingWallText",
                   "LivingFurnitureText", "LivingCurtainText", "LivingLightText",
                   "RowLine1", "RowLine2");

        // 이름만 비슷한 빈 껍데기가 남아 있으면 지운다.
        foreach (Transform child in popup)
        {
            if (child.GetComponent<SummaryRow>() == null &&
                child.childCount == 0 &&
                child.name.StartsWith("Saved"))
            {
                Object.DestroyImmediate(child.gameObject);
                break;
            }
        }

        template.gameObject.SetActive(false);
        return true;
    }

    // 현관 줄로 쓰이던 텍스트 5개를 묶어서 줄 틀로 만든다.
    private static SummaryRow BuildTemplate(Transform popup, RectTransform container)
    {
        string[] sourceNames =
        {
            "EntranceZoneText",
            "EntranceFloorText",
            "EntranceWallText",
            "EntranceFurnitureText",
            "EntranceLightText",
        };

        List<TMPro.TMP_Text> parts = new List<TMPro.TMP_Text>();

        foreach (string sourceName in sourceNames)
        {
            Transform found = Find(popup, sourceName);
            TMPro.TMP_Text text = found != null ? found.GetComponent<TMPro.TMP_Text>() : null;

            if (text == null)
            {
                Debug.LogError(sourceName + " 을 찾지 못해 줄 틀을 만들지 못했습니다.");
                return null;
            }

            parts.Add(text);
        }

        GameObject created = new GameObject("RowTemplate", typeof(RectTransform));
        created.transform.SetParent(container, false);

        foreach (TMPro.TMP_Text part in parts)
        {
            part.transform.SetParent(created.transform, false);
        }

        SummaryRow row = created.AddComponent<SummaryRow>();

        SerializedObject serialized = new SerializedObject(row);
        serialized.FindProperty("label").objectReferenceValue = parts[0];

        SerializedProperty values = serialized.FindProperty("values");
        values.arraySize = parts.Count - 1;
        for (int i = 1; i < parts.Count; i++)
        {
            values.GetArrayElementAtIndex(i - 1).objectReferenceValue = parts[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        return row;
    }

    private static RectTransform BuildContainer(Transform popup)
    {
        Transform existing = Find(popup, ContainerName);
        RectTransform container;

        if (existing != null)
        {
            container = existing as RectTransform;
        }
        else
        {
            GameObject created = new GameObject(ContainerName, typeof(RectTransform));
            container = created.GetComponent<RectTransform>();
            container.SetParent(popup, false);
        }

        container.anchorMin = new Vector2(0f, 1f);
        container.anchorMax = new Vector2(0f, 1f);
        container.pivot = new Vector2(0f, 1f);
        container.anchoredPosition = new Vector2(ContainerX, ContainerY);
        container.sizeDelta = new Vector2(ContainerWidth, (RowHeight + RowGap) * 4f);

        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = RowGap;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        return container;
    }

    private static void MoveTemplateInto(SummaryRow template, RectTransform container)
    {
        RectTransform rect = template.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        if (rect.parent != container)
        {
            rect.SetParent(container, false);
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(ContainerWidth, RowHeight);

        // 높이를 레이아웃이 아니라 이 값으로 잡게 해 둔다.
        LayoutElement element = rect.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = rect.gameObject.AddComponent<LayoutElement>();
        }

        element.preferredHeight = RowHeight;
        element.minHeight = RowHeight;
    }

    // 이름표와 값 칸을 헤더와 같은 X에 세운다. 이름이 아니라 SummaryRow에 꽂힌 순서를 따른다.
    private static void LayOutColumns(SummaryRow template)
    {
        SerializedObject serialized = new SerializedObject(template);
        List<Object> targets = new List<Object> { serialized.FindProperty("label").objectReferenceValue };

        SerializedProperty values = serialized.FindProperty("values");
        for (int i = 0; i < values.arraySize; i++)
        {
            targets.Add(values.GetArrayElementAtIndex(i).objectReferenceValue);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Component component = targets[i] as Component;
            if (component == null)
            {
                continue;
            }

            RectTransform rect = component.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }

            float x = i < ColumnX.Length ? ColumnX[i] : ColumnX[ColumnX.Length - 1] + 130f * (i - ColumnX.Length + 1);

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
        }
    }

    // 줄을 눌러 조합을 되돌리려면 줄 자신에 Button이 있어야 한다.
    private static void EnsureRowButton(SummaryRow template)
    {
        Image image = template.GetComponent<Image>();
        if (image == null)
        {
            image = template.gameObject.AddComponent<Image>();
        }

        // 투명해도 클릭은 받는다. 살짝 보이게 둬서 줄 범위를 알 수 있게 한다.
        image.color = new Color(1f, 1f, 1f, 0.04f);

        Button button = template.GetComponent<Button>();
        if (button == null)
        {
            button = template.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;

        SerializedObject serialized = new SerializedObject(template);
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetText(Transform parent, string objectName, string value)
    {
        Transform target = Find(parent, objectName);
        TMPro.TMP_Text text = target != null ? target.GetComponent<TMPro.TMP_Text>() : null;

        if (text != null)
        {
            text.text = value;
        }
    }

    private static void Deactivate(Transform parent, params string[] names)
    {
        foreach (string name in names)
        {
            Transform target = Find(parent, name);
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }
    }

    // 씬에 있는 SummaryPanel의 Row Template 칸을 채운다.
    private static void WireScenePanel()
    {
        SummaryPanel panel = Object.FindAnyObjectByType<SummaryPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("씬에서 SummaryPanel을 찾지 못했습니다. Row Template은 직접 꽂아 주세요.");
            return;
        }

        SummaryRow row = panel.GetComponentInChildren<SummaryRow>(true);
        if (row == null)
        {
            Debug.LogWarning(panel.name + ": 줄 틀을 찾지 못했습니다.", panel);
            return;
        }

        SerializedObject serialized = new SerializedObject(panel);
        serialized.FindProperty("rowTemplate").objectReferenceValue = row;
        serialized.FindProperty("maxPresets").intValue = 3;
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
    }

    private static Transform Find(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
