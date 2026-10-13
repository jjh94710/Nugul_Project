using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 가구 교체 버튼에 넣을 썸네일을 굽는 도구.
// FurnitureSlot 의 variants 를 하나씩 미리보기 씬에서 렌더링해 PNG 로 저장하고,
// Activate(index) 를 호출하는 버튼의 PreviewImage 에 그 스프라이트를 연결한다.
public class FurnitureThumbnailBaker : EditorWindow
{
    private const string OutputFolder = "Assets/Image/FurnitureThumbnails";
    private const string PreviewImageName = "PreviewImage";
    private const string ActivateMethod = "Activate";
    private const float FieldOfView = 30f;

    // 배경 처리 방식. 자동 대비는 가구가 밝으면 어두운 배경, 어두우면 밝은 배경을 깐다.
    private enum BackgroundMode
    {
        투명,
        단색,
        자동대비,
    }

    private int resolution = 256;
    private BackgroundMode backgroundMode = BackgroundMode.단색;
    private Color backgroundColor = new Color(0.78f, 0.79f, 0.82f, 1f);
    private bool vignette = true;
    private int outlineThickness = 2;
    private bool autoOutlineColor = true;
    private Color outlineColor = new Color(0.14f, 0.14f, 0.16f, 1f);
    private float lightIntensity = 1.3f;
    private float yaw = 35f;
    private float pitch = 20f;
    private float padding = 1.15f;

    [MenuItem("Tools/가구 썸네일 만들기")]
    private static void Open()
    {
        FurnitureThumbnailBaker window = GetWindow<FurnitureThumbnailBaker>(true, "가구 썸네일", true);
        window.minSize = new Vector2(340f, 260f);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "열려 있는 씬의 FurnitureSlot 을 찾아 variants 를 한 장씩 렌더링합니다.\n" +
            "씬에서 못 찾으면 Assets/Prefabs 의 프리팹에서 찾습니다.",
            MessageType.Info);

        resolution = EditorGUILayout.IntPopup(
            "해상도", resolution, new[] { "128", "256", "512" }, new[] { 128, 256, 512 });

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("배경", EditorStyles.boldLabel);
        backgroundMode = (BackgroundMode)EditorGUILayout.EnumPopup("배경 모드", backgroundMode);

        using (new EditorGUI.DisabledScope(backgroundMode != BackgroundMode.단색))
        {
            backgroundColor = EditorGUILayout.ColorField("배경색", backgroundColor);
        }

        using (new EditorGUI.DisabledScope(backgroundMode == BackgroundMode.투명))
        {
            vignette = EditorGUILayout.Toggle("가장자리 어둡게", vignette);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("외곽선", EditorStyles.boldLabel);
        outlineThickness = EditorGUILayout.IntSlider("두께 (0 = 없음)", outlineThickness, 0, 5);

        using (new EditorGUI.DisabledScope(outlineThickness == 0))
        {
            autoOutlineColor = EditorGUILayout.Toggle("색 자동", autoOutlineColor);

            using (new EditorGUI.DisabledScope(autoOutlineColor))
            {
                outlineColor = EditorGUILayout.ColorField("외곽선 색", outlineColor);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("카메라 / 조명", EditorStyles.boldLabel);
        lightIntensity = EditorGUILayout.Slider("조명 세기", lightIntensity, 0.3f, 3f);
        yaw = EditorGUILayout.Slider("좌우 각도", yaw, -180f, 180f);
        pitch = EditorGUILayout.Slider("위아래 각도", pitch, -10f, 70f);
        padding = EditorGUILayout.Slider("여백", padding, 1f, 1.6f);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. 썸네일 굽기", GUILayout.Height(28f)))
        {
            Bake();
        }

        if (GUILayout.Button("2. 버튼 PreviewImage 에 연결", GUILayout.Height(28f)))
        {
            AssignToButtons();
        }
    }

    // --- 굽기 ---------------------------------------------------------------

    private void Bake()
    {
        List<GameObject> loadedPrefabs = new List<GameObject>();
        List<FurnitureSlot> slots = CollectSlots(loadedPrefabs);

        if (slots.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "가구 썸네일", "FurnitureSlot 을 찾지 못했습니다. 가구가 들어 있는 씬을 열고 다시 실행하세요.", "확인");
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        List<string> written = new List<string>();
        int total = 0;

        foreach (FurnitureSlot slot in slots)
        {
            total += ReadVariants(slot).Length;
        }

        try
        {
            foreach (FurnitureSlot slot in slots)
            {
                GameObject[] variants = ReadVariants(slot);

                for (int i = 0; i < variants.Length; i++)
                {
                    if (variants[i] == null)
                    {
                        Debug.LogWarning($"{slot.name}: variants[{i}] 가 비어 있어 건너뜁니다.", slot);
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "가구 썸네일",
                        $"{slot.name} / {variants[i].name}",
                        total == 0 ? 1f : written.Count / (float)total);

                    Texture2D thumbnail = RenderThumbnail(variants[i]);
                    if (thumbnail == null)
                    {
                        Debug.LogWarning($"{variants[i].name}: Renderer 가 없어 건너뜁니다.", variants[i]);
                        continue;
                    }

                    Compose(thumbnail);

                    string path = $"{OutputFolder}/{ThumbnailName(slot, i, variants[i])}.png";
                    File.WriteAllBytes(path, thumbnail.EncodeToPNG());
                    DestroyImmediate(thumbnail);
                    written.Add(path);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            foreach (GameObject root in loadedPrefabs)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.Refresh();

        foreach (string path in written)
        {
            ApplySpriteSettings(path);
        }

        Debug.Log($"가구 썸네일 {written.Count}장을 {OutputFolder} 에 저장했습니다.");
    }

    private Texture2D RenderThumbnail(GameObject variant)
    {
        GameObject clone = Instantiate(variant);
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        clone.SetActive(true);

        if (!TryGetBounds(clone, out Bounds bounds))
        {
            DestroyImmediate(clone);
            return null;
        }

        // 바운즈 중심을 원점에 맞춰 두면 카메라 배치가 단순해진다.
        clone.transform.position -= bounds.center;

        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        float distance = radius * padding / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        PreviewRenderUtility preview = new PreviewRenderUtility();

        try
        {
            preview.BeginStaticPreview(new Rect(0f, 0f, resolution, resolution));

            Camera camera = preview.camera;
            camera.fieldOfView = FieldOfView;
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            camera.farClipPlane = distance + radius * 4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.rotation = rotation;
            camera.transform.position = rotation * Vector3.back * distance;

            preview.ambientColor = new Color(0.32f, 0.32f, 0.36f, 1f) * Mathf.Lerp(0.7f, 1.4f, lightIntensity / 3f);
            preview.lights[0].intensity = lightIntensity;
            preview.lights[0].transform.rotation = Quaternion.Euler(35f, yaw + 40f, 0f);
            preview.lights[1].intensity = lightIntensity * 0.55f;
            preview.lights[1].transform.rotation = Quaternion.Euler(10f, yaw - 120f, 0f);

            preview.AddSingleGO(clone);

            // EndStaticPreview 는 RGB24 로 복사해서 알파를 버린다.
            // 흰 배경과 검은 배경으로 두 번 찍어 알파를 역산하면 실루엣을 정확히 얻는다.
            Color[] onWhite = Capture(preview, Color.white);
            Color[] onBlack = Capture(preview, Color.black);
            preview.EndPreview();

            return BuildMatte(onWhite, onBlack, resolution);
        }
        finally
        {
            preview.Cleanup();
            DestroyImmediate(clone);
        }
    }

    private Color[] Capture(PreviewRenderUtility preview, Color background)
    {
        RenderTexture target = RenderTexture.GetTemporary(
            resolution, resolution, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = preview.camera.targetTexture;
        Texture2D readback = null;

        try
        {
            preview.camera.backgroundColor = background;
            preview.camera.targetTexture = target;
            preview.Render(true, true);

            RenderTexture.active = target;
            readback = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, false);
            readback.ReadPixels(new Rect(0f, 0f, resolution, resolution), 0, 0);
            readback.Apply();

            return readback.GetPixels();
        }
        finally
        {
            RenderTexture.active = previousActive;
            preview.camera.targetTexture = previousTarget;
            RenderTexture.ReleaseTemporary(target);

            if (readback != null)
            {
                DestroyImmediate(readback);
            }
        }
    }

    // 같은 장면을 흰 배경과 검은 배경으로 찍으면 배경이 비치는 만큼만 차이가 난다.
    // 그 차이가 곧 (1 - 알파) 라서 알파와 원래 색을 되돌릴 수 있다.
    private static Texture2D BuildMatte(Color[] onWhite, Color[] onBlack, int size)
    {
        Color[] pixels = new Color[onWhite.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            float difference = (onWhite[i].r - onBlack[i].r
                              + onWhite[i].g - onBlack[i].g
                              + onWhite[i].b - onBlack[i].b) / 3f;
            float alpha = Mathf.Clamp01(1f - difference);

            if (alpha <= 0.002f)
            {
                pixels[i] = new Color(0f, 0f, 0f, 0f);
                continue;
            }

            pixels[i] = new Color(
                Mathf.Clamp01(onBlack[i].r / alpha),
                Mathf.Clamp01(onBlack[i].g / alpha),
                Mathf.Clamp01(onBlack[i].b / alpha),
                alpha);
        }

        Texture2D matte = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        matte.SetPixels(pixels);
        matte.Apply();

        return matte;
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
            {
                continue;
            }

            if (found)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            else
            {
                bounds = renderer.bounds;
                found = true;
            }
        }

        return found;
    }

    // --- 합성 ---------------------------------------------------------------

    // 투명 배경으로 뽑은 렌더 결과에 외곽선과 배경을 입힌다.
    private void Compose(Texture2D thumbnail)
    {
        int size = thumbnail.width;
        Color[] rendered = thumbnail.GetPixels();

        float luminance = SubjectLuminance(rendered);
        Color background = ResolveBackground(luminance);
        Color stroke = autoOutlineColor ? ContrastColor(luminance) : outlineColor;
        float[] outline = outlineThickness > 0 ? BuildOutlineMask(rendered, size, outlineThickness) : null;
        bool useVignette = vignette && backgroundMode != BackgroundMode.투명;

        Color[] result = new Color[rendered.Length];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                Color under = useVignette ? ApplyVignette(background, x, y, size) : background;

                if (outline != null && outline[i] > 0f)
                {
                    under = Blend(under, new Color(stroke.r, stroke.g, stroke.b, outline[i]));
                }

                result[i] = Blend(under, rendered[i]);
            }
        }

        thumbnail.SetPixels(result);
        thumbnail.Apply();
    }

    private Color ResolveBackground(float luminance)
    {
        switch (backgroundMode)
        {
            case BackgroundMode.단색:
                return backgroundColor;

            case BackgroundMode.자동대비:
                return luminance > 0.5f
                    ? new Color(0.20f, 0.21f, 0.24f, 1f)
                    : new Color(0.93f, 0.93f, 0.95f, 1f);

            default:
                return new Color(0f, 0f, 0f, 0f);
        }
    }

    // 가구의 평균 밝기. 실루엣 안쪽 픽셀만 센다.
    private static float SubjectLuminance(Color[] pixels)
    {
        double sum = 0.0;
        int count = 0;

        foreach (Color pixel in pixels)
        {
            if (pixel.a < 0.5f)
            {
                continue;
            }

            sum += pixel.r * 0.2126 + pixel.g * 0.7152 + pixel.b * 0.0722;
            count++;
        }

        return count == 0 ? 0.5f : (float)(sum / count);
    }

    private static Color ContrastColor(float luminance)
    {
        return luminance > 0.5f
            ? new Color(0.10f, 0.10f, 0.12f, 1f)
            : new Color(0.97f, 0.97f, 0.98f, 1f);
    }

    // 실루엣 바깥쪽으로 thickness 픽셀만큼 번지는 외곽선 마스크.
    private static float[] BuildOutlineMask(Color[] pixels, int size, int thickness)
    {
        float[] mask = new float[pixels.Length];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                if (pixels[index].a >= 0.5f)
                {
                    continue;
                }

                float strongest = 0f;

                for (int offsetY = -thickness; offsetY <= thickness; offsetY++)
                {
                    int sampleY = y + offsetY;
                    if (sampleY < 0 || sampleY >= size)
                    {
                        continue;
                    }

                    for (int offsetX = -thickness; offsetX <= thickness; offsetX++)
                    {
                        int sampleX = x + offsetX;
                        if (sampleX < 0 || sampleX >= size)
                        {
                            continue;
                        }

                        if (pixels[sampleY * size + sampleX].a < 0.5f)
                        {
                            continue;
                        }

                        float distance = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);
                        if (distance > thickness)
                        {
                            continue;
                        }

                        strongest = Mathf.Max(strongest, 1f - distance / (thickness + 1f));
                    }
                }

                mask[index] = strongest;
            }
        }

        return mask;
    }

    private static Color ApplyVignette(Color color, int x, int y, int size)
    {
        float half = size * 0.5f;
        float offsetX = (x - half) / half;
        float offsetY = (y - half) / half;
        float distance = Mathf.Clamp01(Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY) / 1.4142f);
        float scale = Mathf.Lerp(1.06f, 0.86f, distance);

        return new Color(
            Mathf.Clamp01(color.r * scale),
            Mathf.Clamp01(color.g * scale),
            Mathf.Clamp01(color.b * scale),
            color.a);
    }

    private static Color Blend(Color under, Color over)
    {
        float alpha = over.a + under.a * (1f - over.a);
        if (alpha <= 0f)
        {
            return new Color(0f, 0f, 0f, 0f);
        }

        float weight = under.a * (1f - over.a);

        return new Color(
            (over.r * over.a + under.r * weight) / alpha,
            (over.g * over.a + under.g * weight) / alpha,
            (over.b * over.a + under.b * weight) / alpha,
            alpha);
    }

    // --- 버튼 연결 ----------------------------------------------------------

    private void AssignToButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        int linked = 0;
        int missing = 0;

        foreach (Button button in buttons)
        {
            if (!TryReadActivateCall(button, out FurnitureSlot slot, out int index))
            {
                continue;
            }

            GameObject[] variants = ReadVariants(slot);
            if (index < 0 || index >= variants.Length || variants[index] == null)
            {
                Debug.LogWarning($"{button.name}: Activate({index}) 에 해당하는 variant 가 없습니다.", button);
                continue;
            }

            string path = $"{OutputFolder}/{ThumbnailName(slot, index, variants[index])}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"{button.name}: 썸네일이 없습니다 ({path}). 먼저 굽기를 실행하세요.", button);
                missing++;
                continue;
            }

            Image preview = FindPreviewImage(button);
            if (preview == null)
            {
                Debug.LogWarning($"{button.name}: {PreviewImageName} 자식을 찾지 못했습니다.", button);
                continue;
            }

            // 프리팹 인스턴스면 원본에 넣어야 씬에 오버라이드가 쌓이지 않는다.
            Image target = PrefabUtility.GetCorrespondingObjectFromSource(preview);
            if (target == null)
            {
                target = preview;
            }

            Undo.RecordObject(target, "가구 썸네일 연결");
            target.sprite = sprite;
            target.preserveAspect = true;
            target.color = Color.white;
            EditorUtility.SetDirty(target);
            linked++;
        }

        AssetDatabase.SaveAssets();

        if (missing > 0)
        {
            Debug.LogWarning($"썸네일이 없어 건너뛴 버튼 {missing}개가 있습니다.");
        }

        Debug.Log($"버튼 {linked}개에 가구 썸네일을 연결했습니다.");
    }

    private static bool TryReadActivateCall(Button button, out FurnitureSlot slot, out int index)
    {
        slot = null;
        index = -1;

        SerializedObject serialized = new SerializedObject(button);
        SerializedProperty calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (calls == null)
        {
            return false;
        }

        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            if (call.FindPropertyRelative("m_MethodName").stringValue != ActivateMethod)
            {
                continue;
            }

            slot = call.FindPropertyRelative("m_Target").objectReferenceValue as FurnitureSlot;
            if (slot == null)
            {
                continue;
            }

            index = call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;
            return true;
        }

        return false;
    }

    private static Image FindPreviewImage(Button button)
    {
        Transform named = button.transform.Find(PreviewImageName);
        if (named != null)
        {
            Image image = named.GetComponent<Image>();
            if (image != null)
            {
                return image;
            }
        }

        foreach (Image image in button.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject != button.gameObject)
            {
                return image;
            }
        }

        return null;
    }

    // --- 공통 ---------------------------------------------------------------

    private static List<FurnitureSlot> CollectSlots(List<GameObject> loadedPrefabs)
    {
        List<FurnitureSlot> slots = new List<FurnitureSlot>(
            FindObjectsByType<FurnitureSlot>(FindObjectsInactive.Include));

        if (slots.Count > 0)
        {
            return slots;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<FurnitureSlot>(true) == null)
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            loadedPrefabs.Add(root);
            slots.AddRange(root.GetComponentsInChildren<FurnitureSlot>(true));
        }

        return slots;
    }

    private static GameObject[] ReadVariants(FurnitureSlot slot)
    {
        SerializedObject serialized = new SerializedObject(slot);
        SerializedProperty array = serialized.FindProperty("variants");
        if (array == null || !array.isArray)
        {
            return new GameObject[0];
        }

        GameObject[] variants = new GameObject[array.arraySize];
        for (int i = 0; i < variants.Length; i++)
        {
            variants[i] = array.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
        }

        return variants;
    }

    private static string ThumbnailName(FurnitureSlot slot, int index, GameObject variant)
    {
        return $"{Sanitize(slot.gameObject.name)}_{index}_{Sanitize(variant.name)}";
    }

    private static string Sanitize(string value)
    {
        string trimmed = value.Trim();

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            trimmed = trimmed.Replace(invalid, '_');
        }

        return trimmed.Replace(' ', '_');
    }

    private static void ApplySpriteSettings(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
