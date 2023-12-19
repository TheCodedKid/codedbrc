using CrewBoomMono;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(CharacterDefinition))]
public class CharacterDefinitionEditor : Editor
{
    private bool _initialized;
    private CharacterDefinition _targetDefinition;

    private GameObject _prefab;
    private bool _isEditable;

    //Base fields
    private SerializedProperty _isNewCharacter;
    private SerializedProperty _characterToReplace;
    private SerializedProperty _characterName;
    private SerializedProperty _characterRenderers;
    private SerializedProperty _characterGraffiti;
    private SerializedProperty _characterGraffitiName;
    private SerializedProperty _voiceDie;
    private SerializedProperty _voiceDieFall;
    private SerializedProperty _voiceTalk;
    private SerializedProperty _voiceBoostTrick;
    private SerializedProperty _voiceCombo;
    private SerializedProperty _voiceGetHit;
    private SerializedProperty _voiceJump;
    private SerializedProperty _characterCanBlink;

    private const int OUTFIT_AMOUNT = 4;
    private bool[][] _materialFoldouts;
    private bool[][] _errorOutfits;

    private GUIStyle _propertyWindowStyle;
    private GUIStyle _hiddenFoldoutStyle;
    private GUIContent _invalidMessage;
    private GUIContent _validMessage;
    private GUIContent _prefabNotFoundMessage;
    private GUIContent _shaderToggleContent;
    private GUIContent _blinkShapeKeyContent;
    private GUIContent _newCharacterContent;
    private GUIContent _skinnedRendererContent;

    private Texture _errorIcon;

    private bool _showCharacterProperties = true;
    private GUIContent _characterProperties;
    private bool _showRigProperties = true;
    private GUIContent _rigProperties;
    private bool _showOffsetProperties = true;
    private GUIContent _offsetProperties;
    private bool _showVoiceProperties = false;
    private GUIContent _voiceProperties;

    private static readonly string[] _outfitLabels =
    {
        "Spring",
        "Summer",
        "Autumn",
        "Winter"
    };

    private Transform _root;
    private GameObject _outfitRoot;
    private Avatar _avatar;
    private CharacterRig _rig;

    private Mesh _skatesMeshL, _skatesMeshR, _phoneMesh, _spraycanMesh, _boostMesh;
    private Material _previewMaterial, _spraycanMaterial, _boostMaterial;
    private const string REFERENCE_PATH = "Assets/Reference Models/";
    private const string SKATES_MESH_L_PATH = REFERENCE_PATH + "skateLeft.obj";
    private const string SKATES_MESH_R_PATH = REFERENCE_PATH + "skateRight.obj";
    private const string SPRAYCAN_MESH_PATH = REFERENCE_PATH + "spraycanMesh.obj";
    private const string PHONE_MESH_PATH = REFERENCE_PATH + "phoneMesh.obj";
    private const string PREVIEW_MATERIAL_PATH = REFERENCE_PATH + "previewMaterial.mat";
    private const string SPRAYCAN_MATERIAL_PATH = REFERENCE_PATH + "spraycanMesh_0Mat.mat";
    private const string BOOST_MESH_PATH = REFERENCE_PATH + "boostMesh.obj";
    private const string BOOST_MATERIAL_PATH = REFERENCE_PATH + "boostMesh_0Mat.mat";

    private void OnEnable()
    {
        _initialized = false;
    }

    private void Initialize()
    {
        _isNewCharacter = serializedObject.FindProperty(nameof(CharacterDefinition.IsNewCharacter));
        _characterToReplace = serializedObject.FindProperty(nameof(CharacterDefinition.CharacterToReplace));
        _characterName = serializedObject.FindProperty(nameof(CharacterDefinition.CharacterName));
        _characterRenderers = serializedObject.FindProperty(nameof(CharacterDefinition.Renderers));
        _characterGraffiti = serializedObject.FindProperty(nameof(CharacterDefinition.Graffiti));
        _characterGraffitiName = serializedObject.FindProperty(nameof(CharacterDefinition.GraffitiName));
        _voiceDie = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceDie));
        _voiceDieFall = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceDieFall));
        _voiceTalk = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceTalk));
        _voiceBoostTrick = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceBoostTrick));
        _voiceCombo = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceCombo));
        _voiceGetHit = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceGetHit));
        _voiceJump = serializedObject.FindProperty(nameof(CharacterDefinition.VoiceJump));
        _characterCanBlink = serializedObject.FindProperty(nameof(CharacterDefinition.CanBlink));

        _invalidMessage = EditorGUIUtility.IconContent("Invalid@2x");
        _invalidMessage.text = "Character is not valid and can not be built.";
        _validMessage = EditorGUIUtility.IconContent("Installed@2x");
        _validMessage.text = "Character is valid and ready to be built.";
        _prefabNotFoundMessage = EditorGUIUtility.IconContent("console.warnicon.sml");
        _prefabNotFoundMessage.text = "Character is not a prefab. Please create one.";
        _errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;

        _shaderToggleContent = EditorGUIUtility.IconContent("Shader Icon");
        _shaderToggleContent.text = "Use game shader";
        _shaderToggleContent.tooltip = "On - Uses the shader the game uses for characters for the ouftits.\nOff - Uses the shader applied to the material (Custom shaders).";

        _characterProperties = EditorGUIUtility.IconContent("d_CustomTool");
        _characterProperties.text = " Character Properties";
        _rigProperties = EditorGUIUtility.IconContent("AvatarSelector");
        _rigProperties.text = " Rig Properties";
        _offsetProperties = EditorGUIUtility.IconContent("AvatarPivot");
        _offsetProperties.text = "Offset Properties";
        _voiceProperties = EditorGUIUtility.IconContent("SceneViewAudio On");
        _voiceProperties.text = " Custom Voice";
        _blinkShapeKeyContent = new GUIContent();
        _blinkShapeKeyContent.text = "Blinking Blend Shape";
        _blinkShapeKeyContent.tooltip = "The blend shape to use on the mesh for blinking.";
        _newCharacterContent = new GUIContent();
        _newCharacterContent.text = "Base Character";
        _newCharacterContent.tooltip = "The character to base this new character on for missing data.";
        _skinnedRendererContent = new GUIContent();
        _skinnedRendererContent.text = "Skinned Mesh";
        _skinnedRendererContent.tooltip = "The skinned mesh renderer used to render this character.";

        _targetDefinition = (CharacterDefinition)target;
        _root = _targetDefinition.gameObject.transform;
        Transform outfitRootTransform = _root.Find("outfits");
        if (outfitRootTransform)
        {
            _outfitRoot = outfitRootTransform.gameObject;
        }
        else
        {
            GameObject newRoot = new GameObject("outfits");

            StageUtility.PlaceGameObjectInCurrentStage(newRoot);
            GameObjectUtility.SetParentAndAlign(newRoot, _root.gameObject);
            Undo.RegisterCreatedObjectUndo(newRoot, $"Created {name}");

            _outfitRoot = newRoot;
        }
        _outfitRoot.hideFlags = HideFlags.NotEditable;

        Animator animator = _root.GetComponent<Animator>();
        if (animator)
        {
            _avatar = animator.avatar;
        }

        _targetDefinition.hideFlags = HideFlags.None;

        //Setting arrays
        {
            _materialFoldouts = new bool[4][];
            _errorOutfits = new bool[4][];

            if (_characterRenderers.arraySize == 0)
            {
                SkinnedMeshRenderer primaryRenderer = _root.GetComponentInChildren<SkinnedMeshRenderer>();
                if (primaryRenderer != null)
                {
                    _characterRenderers.arraySize = 1;
                    _characterRenderers.GetArrayElementAtIndex(0).objectReferenceValue = primaryRenderer;
                }
            }

            serializedObject.ApplyModifiedProperties();

            ValidateOutfits();
        }

        _rig = new CharacterRig();
        _rig.Setup(_avatar);

        //Find prefab from either the scene or prefab stage
        _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(target));
        if (!EditorUtility.IsPersistent(_prefab))
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
            }
            else
            {
                _prefab = PrefabUtility.GetCorrespondingObjectFromSource(_targetDefinition.gameObject);
            }
        }

        _isEditable = !EditorUtility.IsPersistent(_targetDefinition.gameObject);

        if (_isEditable)
        {
            LoadPreviewAssets();
            SceneView.duringSceneGui += DrawPreviews;
        }

        _initialized = true;
    }

    private void ValidateOutfits()
    {
        if (_targetDefinition.Outfits == null)
        {
            _targetDefinition.Outfits = new CharacterOutfit[OUTFIT_AMOUNT];
        }
        else if (_targetDefinition.Outfits.Length != OUTFIT_AMOUNT)
        {
            //Delete any extra components
            for (int i = _targetDefinition.Outfits.Length - 1; i > OUTFIT_AMOUNT - 1; i--)
            {
                DestroyImmediate(_targetDefinition.Outfits[i]);
            }
            Array.Resize(ref _targetDefinition.Outfits, OUTFIT_AMOUNT);
        }
        for (int i = 0; i < _targetDefinition.Outfits.Length; i++)
        {
            if (_targetDefinition.Outfits[i] == null)
            {
                _targetDefinition.Outfits[i] = _outfitRoot.AddComponent<CharacterOutfit>();
            }
        }

        for (int outfit = 0; outfit < OUTFIT_AMOUNT; outfit++)
        {
            //Enabled renderers array
            if (_targetDefinition.Outfits[outfit].EnabledRenderers == null)
            {
                _targetDefinition.Outfits[outfit].EnabledRenderers = new bool[_characterRenderers.arraySize];
            }
            else if (_targetDefinition.Outfits[outfit].EnabledRenderers.Length != _characterRenderers.arraySize)
            {
                Array.Resize(ref _targetDefinition.Outfits[outfit].EnabledRenderers, _characterRenderers.arraySize);
            }

            _materialFoldouts[outfit] = new bool[_characterRenderers.arraySize];
            _errorOutfits[outfit] = new bool[_characterRenderers.arraySize];

            //Material lists
            if (_targetDefinition.Outfits[outfit].MaterialContainers == null)
            {
                _targetDefinition.Outfits[outfit].MaterialContainers = new CharacterOutfitRenderer[_characterRenderers.arraySize];
            }
            else if (_targetDefinition.Outfits[outfit].MaterialContainers.Length != _characterRenderers.arraySize)
            {
                //Delete any extra components
                for (int i = _targetDefinition.Outfits[outfit].MaterialContainers.Length - 1; i > _characterRenderers.arraySize - 1; i--)
                {
                    DestroyImmediate(_targetDefinition.Outfits[outfit].MaterialContainers[i]);
                }
                Array.Resize(ref _targetDefinition.Outfits[outfit].MaterialContainers, _characterRenderers.arraySize);
            }
            for (int i = 0; i < _targetDefinition.Outfits[outfit].MaterialContainers.Length; i++)
            {
                if (_targetDefinition.Outfits[outfit].MaterialContainers[i] == null)
                {
                    _targetDefinition.Outfits[outfit].MaterialContainers[i] = _outfitRoot.AddComponent<CharacterOutfitRenderer>();
                }
            }

            for (int renderer = 0; renderer < _characterRenderers.arraySize; renderer++)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = _targetDefinition.Renderers[renderer];
                if (skinnedMeshRenderer == null)
                {
                    continue;
                }
                int rendererMaterialCount = skinnedMeshRenderer.sharedMaterials.Length;

                if (_targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials == null)
                {
                    _targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials = skinnedMeshRenderer.sharedMaterials;
                }
                else if (_targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials.Length != rendererMaterialCount)
                {
                    Array.Resize(ref _targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials, rendererMaterialCount);
                }
                if (_targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial == null)
                {
                    _targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial = new bool[rendererMaterialCount];
                    for (int material = 0; material < rendererMaterialCount; material++)
                    {
                        _targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial[material] = true;
                    }
                }
                else if (_targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial.Length != rendererMaterialCount)
                {
                    Array.Resize(ref _targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial, rendererMaterialCount);
                }
            }
        }
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= DrawPreviews;
    }

    private void InitializeWindowStyle()
    {
        if (_propertyWindowStyle != null)
        {
            return;
        }

        _propertyWindowStyle = new GUIStyle("GroupBox");
        _propertyWindowStyle.fontStyle = FontStyle.Bold;
        _propertyWindowStyle.fontSize = 14;
        _propertyWindowStyle.alignment = TextAnchor.UpperLeft;
        Vector2 offset = _propertyWindowStyle.contentOffset;
        offset.y = -16f;
        _propertyWindowStyle.contentOffset = offset;
    }

    private void InitializeFoldoutStyle()
    {
        if (_hiddenFoldoutStyle != null)
        {
            return;
        }

        _hiddenFoldoutStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        _hiddenFoldoutStyle.alignment = TextAnchor.MiddleLeft;
        RectOffset padding = _hiddenFoldoutStyle.padding;
        padding.top = 2;
        _hiddenFoldoutStyle.margin = padding;
    }

    private void LoadPreviewAssets()
    {
        var skatesLeftAssets = AssetDatabase.LoadAllAssetsAtPath(SKATES_MESH_L_PATH);
        foreach (var obj in skatesLeftAssets)
        {
            if (obj as Mesh != null)
            {
                _skatesMeshL = obj as Mesh;
                break;
            }
        }
        var skatesRightAssets = AssetDatabase.LoadAllAssetsAtPath(SKATES_MESH_R_PATH);
        foreach (var obj in skatesRightAssets)
        {
            if (obj as Mesh != null)
            {
                _skatesMeshR = obj as Mesh;
                break;
            }
        }
        var phoneMeshAssets = AssetDatabase.LoadAllAssetsAtPath(PHONE_MESH_PATH);
        foreach (var obj in phoneMeshAssets)
        {
            if (obj as Mesh != null)
            {
                _phoneMesh = obj as Mesh;
                break;
            }
        }
        var spraycanAssets = AssetDatabase.LoadAllAssetsAtPath(SPRAYCAN_MESH_PATH);
        foreach (var obj in spraycanAssets)
        {
            if (obj as Mesh != null)
            {
                _spraycanMesh = obj as Mesh;
                break;
            }
        }

        var boostAssets = AssetDatabase.LoadAllAssetsAtPath(BOOST_MESH_PATH);
        foreach (var obj in boostAssets)
        {
            if (obj as Mesh != null)
            {
                _boostMesh = obj as Mesh;
                break;
            }
        }

        _previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(PREVIEW_MATERIAL_PATH);
        _spraycanMaterial = AssetDatabase.LoadAssetAtPath<Material>(SPRAYCAN_MATERIAL_PATH);
        _boostMaterial = AssetDatabase.LoadAssetAtPath<Material>(BOOST_MATERIAL_PATH);
    }

    public void DrawPreviews(SceneView sceneView)
    {
        DrawProps();
        DrawSkateMeshes();
        DrawBoostMesh();
    }

    private void DrawProps()
    {
        if (_rig.PreviewProps)
        {
            if (_spraycanMesh && _rig.RightPropBone)
            {
                _spraycanMaterial.SetPass(0);
                Graphics.DrawMeshNow(_spraycanMesh, _rig.RightPropBone.localToWorldMatrix);
            }
            if (_phoneMesh && _rig.LeftPropBone)
            {
                _previewMaterial.SetPass(0);
                Graphics.DrawMeshNow(_phoneMesh, _rig.LeftPropBone.localToWorldMatrix);
            }
        }
    }
    private void DrawSkateMeshes()
    {
        if (_rig.PreviewSkates)
        {
            if (_skatesMeshL && _rig.LeftSkateBone)
            {
                _previewMaterial.SetPass(0);
                Graphics.DrawMeshNow(_skatesMeshL, _rig.LeftSkateBone.localToWorldMatrix);
            }
            if (_skatesMeshR && _rig.RightSkateBone)
            {
                _previewMaterial.SetPass(0);
                Graphics.DrawMeshNow(_skatesMeshR, _rig.RightSkateBone.localToWorldMatrix);
            }
        }
    }

    private void DrawBoostMesh()
    {
        if (_rig.PreviewBoost)
        {
            Transform boostBone = _rig.BoostBone;
            if (_boostMesh && boostBone)
            {
                _boostMaterial.SetPass(0);
                Quaternion offset = Quaternion.Euler(Vector3.up * 270.0f);

                Matrix4x4 boostMatrix = Matrix4x4.TRS(boostBone.position, boostBone.rotation * offset, Vector3.one * 0.7f);
                Graphics.DrawMeshNow(_boostMesh, boostMatrix);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (!_initialized)
        {
            Initialize();
        }

        InitializeWindowStyle();
        InitializeFoldoutStyle();

        bool characterValid = true;

        if (!ValidateProperties() | !ValidateRig())
        {
            characterValid = false;
        }

        DrawOffsetProperties();
        DrawVoiceProperties();

        using (new EditorGUILayout.VerticalScope())
        {
            bool disableGui = false;
            if (characterValid)
            {
                EditorGUILayout.LabelField(_validMessage, EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(_invalidMessage, EditorStyles.centeredGreyMiniLabel);
                disableGui = true;
            }

            if (_prefab == null)
            {
                EditorGUILayout.LabelField(_prefabNotFoundMessage, EditorStyles.centeredGreyMiniLabel);
                disableGui = true;
            }

            if (disableGui)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Build character bundle"))
            {
                CustomCharacterBundleBuilder.BuildBundle(_prefab);
            }

            if (disableGui)
            {
                GUI.enabled = true;
            }
        }
    }

    private bool ValidateProperties()
    {
        bool allValid = true;

        if (!_isEditable)
        {
            EditorGUILayout.HelpBox("You are viewing the prefab asset. To edit the character you need to open the prefab.", MessageType.Warning);
            EditorGUILayout.Space();
        }

        GUILayout.BeginVertical(_characterProperties, _propertyWindowStyle);
        {
            EditorGUI.indentLevel++;
            _showCharacterProperties = EditorGUILayout.Foldout(_showCharacterProperties, _showCharacterProperties ? string.Empty : "Click to show", true, _hiddenFoldoutStyle);
            EditorGUI.indentLevel--;

            if (_showCharacterProperties)
            {
                //EditorGUILayout.PropertyField(_isNewCharacter);
                if (_isNewCharacter.boolValue)
                {
                    EditorGUILayout.PropertyField(_characterToReplace, _newCharacterContent);
                }
                else
                {
                    EditorGUILayout.PropertyField(_characterToReplace);
                }
                EditorGUILayout.PropertyField(_characterName, new GUIContent("Name"));

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Outfits", EditorStyles.boldLabel);

                int initialRendererAmount = _characterRenderers.arraySize;

                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.PropertyField(_characterRenderers);
                    serializedObject.ApplyModifiedProperties();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    ValidateOutfits();
                }
                EditorGUI.indentLevel--;

                if (_characterRenderers.arraySize != initialRendererAmount)
                {
                    ValidateOutfits();
                }

                for (int outfit = 0; outfit < OUTFIT_AMOUNT; outfit++)
                {
                    if (!DrawMultiMeshOutfitProperty(outfit, _outfitLabels[outfit]))
                    {
                        allValid = false;
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Graffiti (Optional)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_characterGraffitiName, new GUIContent("Title"));
                EditorGUILayout.PropertyField(_characterGraffiti, new GUIContent());

                Material graffitiMaterial = _characterGraffiti.objectReferenceValue as Material;
                if (graffitiMaterial)
                {
                    Texture mainTexture = graffitiMaterial.mainTexture;
                    if (mainTexture)
                    {
                        if (mainTexture.height != 256 || mainTexture.width != 256)
                        {
                            EditorGUILayout.HelpBox("The graffiti's texture resolution is not 256x256. It may not display correctly in-game.", MessageType.Warning);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("The graffiti material needs to have a main texture assigned (Property \"_MainTex\").", MessageType.Warning);
                    }
                }

                if (_characterRenderers.arraySize > 0)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Blinking", EditorStyles.boldLabel);

                    bool hasBlinkSomewhere = true;
                    foreach (SkinnedMeshRenderer renderer in _targetDefinition.Renderers)
                    {
                        if (renderer.sharedMesh.blendShapeCount > 0)
                        {
                            hasBlinkSomewhere = true;
                        }
                    }
                    if (hasBlinkSomewhere)
                    {
                        EditorGUILayout.PropertyField(_characterCanBlink, new GUIContent("Enabled"));
                        if (_characterCanBlink.boolValue)
                        {
                            EditorGUILayout.HelpBox("For multiple meshes/renderers, the game will make the first enabled renderer per outfit blink when possible.", MessageType.Warning);
                        }
                    }
                    else
                    {
                        _characterCanBlink.boolValue = false;
                        EditorGUILayout.HelpBox("Renderers do not have any BlendShapes, blinking is disabled.", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Could not find renderers for character. Please assign at least one.", MessageType.Warning);
                }
            }
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        return allValid;
    }
    private bool DrawMultiMeshOutfitProperty(int outfit, string title)
    {
        bool validOutfit = true;

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginVertical("GroupBox");
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);

            EditorGUI.indentLevel++;
            for (int renderer = 0; renderer < _characterRenderers.arraySize; renderer++)
            {
                if (_targetDefinition.Renderers[renderer] == null)
                {
                    EditorGUILayout.HelpBox($"Renderer {renderer} is null.", MessageType.Error);
                    validOutfit = false;
                    continue;
                }

                EditorGUILayout.BeginVertical("GroupBox");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUIContent content = new GUIContent();
                        content.text = _targetDefinition.Renderers[renderer].name;

                        if (_errorOutfits[outfit][renderer])
                        {
                            content.image = _errorIcon;
                            content.tooltip = "Mesh has missing materials!";
                        }

                        _materialFoldouts[outfit][renderer] = EditorGUILayout.Foldout(_materialFoldouts[outfit][renderer], content, true);
                        _targetDefinition.Outfits[outfit].EnabledRenderers[renderer] = EditorGUILayout.Toggle(_targetDefinition.Outfits[outfit].EnabledRenderers[renderer]);
                    }
                    EditorGUILayout.EndHorizontal();

                    bool copy = false;

                    if (_materialFoldouts[outfit][renderer])
                    {
                        EditorGUILayout.Space();

                        if (GUILayout.Button("Copy Original Materials"))
                        {
                            copy = true;
                        }

                        EditorGUILayout.Space();
                    }

                    _errorOutfits[outfit][renderer] = false;

                    for (int materialId = 0; materialId < _targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials.Length; materialId++)
                    {
                        if (_materialFoldouts[outfit][renderer])
                        {
                            if (copy)
                            {
                                _targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials[materialId] = _targetDefinition.Renderers[renderer].sharedMaterials[materialId];
                            }

                            _targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials[materialId] = (Material)EditorGUILayout.ObjectField(_targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials[materialId], typeof(Material), false);

                            GUI.enabled = false;
                            EditorGUILayout.ObjectField("Original", _targetDefinition.Renderers[renderer].sharedMaterials[materialId], typeof(Material), false);
                            GUI.enabled = true;

                            _targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial[materialId] = EditorGUILayout.Toggle(_shaderToggleContent, _targetDefinition.Outfits[outfit].MaterialContainers[renderer].UseShaderForMaterial[materialId]);

                            EditorGUILayout.Space();
                        }

                        if (_targetDefinition.Outfits[outfit].MaterialContainers[renderer].Materials[materialId] == null && _targetDefinition.Outfits[outfit].EnabledRenderers[renderer])
                        {
                            validOutfit = false;
                            _errorOutfits[outfit][renderer] = true;
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;

            if (!validOutfit)
            {
                EditorGUILayout.HelpBox("One or more material slot(s) are not set to anything. Please assign all material slots that are used.", MessageType.Error);
            }
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_root);
        }

        return validOutfit;
    }

    private bool ValidateRig()
    {
        bool valid = true;

        GUILayout.BeginVertical(_rigProperties, _propertyWindowStyle);
        {
            EditorGUI.indentLevel++;
            _showRigProperties = EditorGUILayout.Foldout(_showRigProperties, _showRigProperties ? string.Empty : "Click to show", true, _hiddenFoldoutStyle);
            EditorGUI.indentLevel--;

            if (_showRigProperties)
            {
                EditorGUILayout.ObjectField("Character Root", _root, typeof(Transform), true);

                EditorGUILayout.Space();

                if (!_rig.Validate(_root, _isEditable))
                {
                    valid = false;
                }
            }
        }
        GUILayout.EndVertical();

        return valid;
    }
    private void DrawOffsetProperties()
    {
        GUILayout.BeginVertical(_offsetProperties, _propertyWindowStyle);
        {
            EditorGUI.indentLevel++;
            _showOffsetProperties = EditorGUILayout.Foldout(_showOffsetProperties, _showOffsetProperties ? string.Empty : "Click to show", true, _hiddenFoldoutStyle);
            EditorGUI.indentLevel--;

            if (_showOffsetProperties)
            {
                _rig.ValidateSkateOffsets();
            }
        }
        GUILayout.EndVertical();
    }
    private void DrawVoiceProperties()
    {
        GUILayout.BeginVertical(_voiceProperties, _propertyWindowStyle);
        {
            if (_targetDefinition.CharacterToReplace == BrcCharacter.FelixWithCyberHead)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The game uses Red's voice lines for Felix with a cyberhead, so custom voice is disabled.", MessageType.Warning);
            }
            else
            {
                EditorGUI.indentLevel++;
                _showVoiceProperties = EditorGUILayout.Foldout(_showVoiceProperties, _showVoiceProperties ? string.Empty : "Click to show", true, _hiddenFoldoutStyle);
                EditorGUI.indentLevel--;

                if (_showVoiceProperties)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_voiceDie);
                    EditorGUILayout.PropertyField(_voiceDieFall);
                    EditorGUILayout.PropertyField(_voiceTalk);
                    EditorGUILayout.PropertyField(_voiceBoostTrick);
                    EditorGUILayout.PropertyField(_voiceCombo);
                    EditorGUILayout.PropertyField(_voiceGetHit);
                    EditorGUILayout.PropertyField(_voiceJump);
                    EditorGUI.indentLevel--;
                }
            }
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
