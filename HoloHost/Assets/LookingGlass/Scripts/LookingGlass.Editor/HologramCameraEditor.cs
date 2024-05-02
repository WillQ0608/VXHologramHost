﻿//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using LookingGlass.Editor.EditorPropertyGroups;

namespace LookingGlass.Editor {
    [InitializeOnLoad]
    [CustomEditor(typeof(HologramCamera))]
    [CanEditMultipleObjects]
    public class HologramCameraEditor : UnityEditor.Editor {
        #region Static Section
        static HologramCameraEditor() {
            CalibrationSystem.onReload += () => {
                EditorUpdates.Delay(6, () => {
                    UpdateUserGameViews();
                });
            };
        }

        public const string PrefabGuid = "3019d4eebe2593e428d7b9ccb096ceda";
        public static string PrefabAssetPath => AssetDatabase.GUIDToAssetPath(PrefabGuid);
        private static int CondensedLineSpace => (int) (0.75f * EditorGUIUtility.singleLineHeight);

        [MenuItem("GameObject/Looking Glass/Add Hologram Camera to Scene", false, 10)]
        private static void AddHologramCameraToScene() => CreateHologramCamera();

        [MenuItem("GameObject/Looking Glass/Convert to Hologram Camera", validate = true)]
        private static bool ValidateCameraComponentToHologramCamera() {
            GameObject selected = Selection.activeGameObject;
            return selected != null && selected.GetComponent<Camera>() != null && selected.GetComponent<HologramCamera>() == null;
        }

        // This function is for a right click menu on a game object with a camera component.
        [MenuItem("GameObject/Looking Glass/Convert to Hologram Camera")]
        private static void CameraComponentToHologramCamera() => ConvertCameraConfirmWindow();
            

        // This function is for right clicking on a camera component
        [MenuItem("CONTEXT/Camera/Convert to Hologram Camera")]
        private static void ContextCameraObjectToHologramCamera() => ConvertCameraConfirmWindow();

        /// <summary>
        /// Creates a <see cref="HologramCamera"/> prefab instance with undo creation support, and selects it in the Unity editor.
        /// </summary>
        public static HologramCamera CreateHologramCamera(Transform parent = null, int siblingIndex = -1) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LookingGlass/Prefabs/Hologram Camera.prefab");
            if (prefab == null) {
                Debug.LogWarning("[Hologram Camera] Couldn't find the Hologram Camera folder or prefab.");
                return null;
            }

            if (siblingIndex < 0) {
                //NOTE: This temp GameObject is just here to calculate what the sibling index WOULD be.
                //For some reason, PrefabUtility.InstantiatePrefab(...) may put the new prefab instance in the middle of the hierarchy,
                //So it makes the hierarchy less orderly, and more messy.
                //However, doing "new GameObject()" puts it at the end of the hierarchy. So we copy that sibling index to mimick the same behavior with the prefab instance.

                GameObject temp = new GameObject("temp");
                siblingIndex = temp.transform.GetSiblingIndex();
                GameObject.DestroyImmediate(temp);
            }

            GameObject gameObject = (GameObject) PrefabUtility.InstantiatePrefab(prefab, parent);
            gameObject.name = prefab.name;

            if (siblingIndex >= 0)
                gameObject.transform.SetSiblingIndex(siblingIndex);

            Selection.activeObject = gameObject;
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Hologram Camera");

            HologramCamera hologramCamera = gameObject.GetComponent<HologramCamera>();
            Assert.IsNotNull(hologramCamera);
            return hologramCamera;
        }

        /// <summary>
        /// <para>Converts the given <paramref name="target"/> <see cref="GameObject"/> into a <see cref="HologramCamera"/> prefab instance.<br />This method has full undo support.</para>
        /// <para>When <paramref name="useHologramCameraDefaults"/> is set to <c>true</c>, and if <paramref name="target"/> has a <see cref="Camera"/> component, fields from it are copied over to the new <see cref="HologramCamera"/> component.</para>
        /// </summary>
        /// <param name="target">The object to convert.</param>
        /// <param name="useHologramCameraDefaults">
        /// Should the new <see cref="HologramCamera"/> component initialize with default values from the prefab?<br />
        /// When set to <c>false</c>, if <paramref name="target"/> has a <see cref="Camera"/> component, fields from it are copied over to the new <see cref="HologramCamera"/> component.
        /// </param>
        public static void ConvertToHologramCamera(GameObject target, bool useHologramCameraDefaults) {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Convert to Hologram Camera");

            HologramCamera hologramCamera = CreateHologramCamera(target.transform.parent, target.transform.GetSiblingIndex());
            if (!useHologramCameraDefaults && target.TryGetComponent(out Camera previousCamera))
                hologramCamera.CameraProperties.CopyFromCamera(previousCamera);

            Undo.DestroyObjectImmediate(target);
        }

        private static void ConvertCameraConfirmWindow()
        {
            GameObject target = Selection.activeGameObject;
            Assert.IsNotNull(target);

            int dialogChoice = EditorUtility.DisplayDialogComplex("Use recommended settings for Looking Glass Camera? ",
                "Looking Glass Cameras work best with a smaller field of view and constrained clipping planes." +
                "Would you like to set your camera to Looking Glass Camera defaults? \n\n" +
                "Warning! This may break existing camera animations!", "Yes", "Cancel", "No");

            switch (dialogChoice)
            {
                case 0:
                    ConvertToHologramCamera(target, true);
                    Debug.Log("Use recommended values function"); // Slap on default Looking Glass Camera?
                    break;
                case 1:
                    Debug.Log("Cancel"); // THIS IS CANCEL
                    break;
                case 2:
                    ConvertToHologramCamera(target, false);
                    Debug.Log("Copy camera values to Looking Glass Camera function");
                    break;
            }
        }

        public static bool AddErrorCondition(HelpMessageCondition condition) {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (errorConditions == null) {
                errorConditions = new List<HelpMessageCondition>();
                errorConditions.Add(condition);
                return true;
            }
            if (errorConditions.Contains(condition))
                return false;
            errorConditions.Add(condition);
            return true;
        }

        public static bool RemoveErrorCondition(HelpMessageCondition condition) {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            return errorConditions != null && errorConditions.Remove(condition);
        }
        #endregion

        private static GUIContent versionLabel1;
        private static GUIContent versionLabel2;

        private static int editorsOpen = 0;
        private static string[] serialNames = new string[0];

        private static bool showCalibrationData;
        private static List<HelpMessageCondition> errorConditions;

        private HologramCamera[] cameras;

        private bool changedTargetDisplay = false;
        private bool changedLKGName = false;
        private bool changedEmulatedDevice = false;
        private bool changedQuiltPreset = false;
        private bool requiresPreviewUpdate = false;
        private bool changedTransformMode = false;
        private TransformMode nextTransformMode;

        private const string UseCustomRenderSettingsTooltip = "Determines whether or not to use custom render settings, or automatic render settings (when set to false).\n\n" +
            "Note that in the API, this sets " + nameof(HologramCamera) + "." + nameof(HologramCamera.QuiltPreset) + ".\n\n" +
            "In C# scripting, you can also set the camera to use the quilt preset for various specific devices, such as the LKG Portrait, LKG 16\", LKG 32\", etc.\n\n" +
            "(See the " + nameof(QuiltPreset) + " enum for all possible values, include Custom and Automatic).";

        private static HashSet<string> volumeModeProperties = new HashSet<string> {
            nameof(HologramCamera.size),
            nameof(HologramCamera.sizeMode),
            nameof(HologramCamera.nearClipFactor),
            nameof(HologramCamera.farClipFactor)
        };
        private static HashSet<string> cameraModeProperties = new HashSet<string> {
            nameof(HologramCamera.nearClipPlane),
            nameof(HologramCamera.farClipPlane),
            nameof(HologramCamera.focalPlane)
        };
        private static readonly EditorPropertyGroup[] Groups = new EditorPropertyGroup[] {
            new EditorPropertyGroup(
                "Camera Properties",
                "Contains a set of fields that correspond to fields on a Unity Camera, with some extra hologramCamera fields.",
                new string[] {
                    nameof(HologramCamera.transformMode),
                    nameof(HologramCamera.nearClipFactor),
                    nameof(HologramCamera.farClipFactor),
                    nameof(HologramCamera.nearClipPlane),
                    nameof(HologramCamera.farClipPlane),
                    nameof(HologramCamera.focalPlane),
                    nameof(HologramCamera.size),
                    nameof(HologramCamera.sizeMode),
                    nameof(HologramCamera.clearFlags),
                    nameof(HologramCamera.background),
                    nameof(HologramCamera.cullingMask),
                    nameof(HologramCamera.fov),
                    nameof(HologramCamera.depth),
                    nameof(HologramCamera.renderingPath),
                    nameof(HologramCamera.useOcclusionCulling),
                    nameof(HologramCamera.allowHDR),
                    nameof(HologramCamera.allowMSAA),
                    nameof(HologramCamera.allowDynamicResolution),
                    nameof(HologramCamera.useFrustumTarget),
                    nameof(HologramCamera.frustumTarget),
                    nameof(HologramCamera.centerOffset),
                    nameof(HologramCamera.horizontalFrustumOffset),
                    nameof(HologramCamera.verticalFrustumOffset),
                    nameof(HologramCamera.depthiness)
                }
            ),
            new EditorPropertyGroup(
                "Gizmos",
                "Contains settings for visualizations in the Scene View.",
                new string[] {
                    nameof(HologramCamera.drawHandles),
                    nameof(HologramCamera.frustumColor),
                    nameof(HologramCamera.middlePlaneColor),
                    nameof(HologramCamera.handleColor)
                }
            ),
            new EditorPropertyGroup(
                "Events",
                "Contains LookingGlass events, related to initialization and rendering.",
                new string[] {
                    nameof(HologramCamera.onViewRendered)
                }
            ),
            new EditorPropertyGroup(
                "Optimization",
                "Contains LookingGlass optimization techniques to increase performance.",
                new string[] {
                    nameof(HologramCamera.viewInterpolation),
                    nameof(HologramCamera.reduceFlicker),
                    nameof(HologramCamera.fillGaps),
                    nameof(HologramCamera.blendViews),
                }
            ),
            new EditorPropertyGroup(
                "Debugging",
                "Contains settings to ease debugging the LookingGlass script.",
                new string[] {
                    nameof(HologramCamera.showAllObjects),
                    nameof(HologramCamera.onlyShowView),
                    nameof(HologramCamera.onlyRenderOneView),
                    nameof(HologramCamera.manualCalibrationMode),
                    nameof(HologramCamera.calibrationFile),
                    nameof(HologramCamera.manualCalibration),
                }
            )
        };

        private static Dictionary<HologramCamera, HologramEmulation> holograms = new Dictionary<HologramCamera, HologramEmulation>();
        private static List<HologramCamera> removeBuffer = new List<HologramCamera>();

        private IEnumerable<HologramEmulation> Holograms {
            get {
                foreach (HologramCamera h in cameras)
                    if (holograms.TryGetValue(h, out HologramEmulation hologram))
                        yield return hologram;
            }
        }

        #region Unity Messages
        private void OnEnable() {
            cameras = targets.Select(t => (HologramCamera) t).ToArray();

            Preview.onToggled += Repaint;
            editorsOpen++;
            if (editorsOpen == 1) {
                RefreshLKGNames();
                _ = RefreshAfterCalibrations();
            }

            Groups[0].IsExpanded = true;
            serializedObject.FindProperty(nameof(HologramCamera.customRenderSettings)).isExpanded = true;

            foreach (HologramCamera c in cameras)
                if (!EditorUtility.IsPersistent(c) && !holograms.ContainsKey(c))
                    holograms.Add(c, new HologramEmulation(c));
        }

        private void OnDisable() {
            DisposeHolograms();

            Preview.onToggled -= Repaint;
            editorsOpen--;
            if (editorsOpen <= 0)
                CalibrationSystem.onReload -= RefreshLKGNames;

            versionLabel1 = null;
            versionLabel2 = null;
        }

        protected virtual void OnSceneGUI() {
            HologramCamera current = (HologramCamera) target;
            try {
                if (current.enabled && !current.Gizmos.DrawHandles) {
                    if (current.CameraProperties.TransformMode == TransformMode.Volume)
                        DrawSizeHandles(current);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }

            if (holograms.TryGetValue(current, out HologramEmulation hologram) &&
                (current.ShowHologramPreview && (cameras.Length == 1 || Array.IndexOf(cameras, current) == 0)))
                hologram.OnSceneGUI();
        }
        #endregion

        #region Inspector Preview
        public override bool RequiresConstantRepaint() {
            HologramCamera current = (HologramCamera) target;
            if (!current.ShowHologramPreview)
                return false;
            if (!holograms.TryGetValue(current, out HologramEmulation hologram))
                return false;
            return hologram != null && hologram.RequiresConstantRepaint();
        }

        public override bool HasPreviewGUI() {
            HologramCamera current = (HologramCamera) target;
            if (!current.ShowHologramPreview)
                return false;
            if (!holograms.TryGetValue(current, out HologramEmulation hologram))
                return false;
            return hologram != null && hologram.HasPreviewGUI();
        }

        public override void OnPreviewSettings() {
            HologramCamera current = (HologramCamera) target;
            if (!current.ShowHologramPreview)
                return;
            if (!holograms.TryGetValue(current, out HologramEmulation hologram))
                return;

            bool value = GUILayout.Toggle(hologram.AutoRotate, "Auto Rotate", EditorStyles.toolbarButton);
            if (value != hologram.AutoRotate) {
                foreach (HologramEmulation h in Holograms)
                    h.AutoRotate = value;
            }
        }

        public override string GetInfoString() {
            HologramCamera current = (HologramCamera) target;
            if (!current.ShowHologramPreview)
                return null;
            if (!holograms.TryGetValue(current, out HologramEmulation hologram))
                return null;
            return hologram.GetInfoString();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            HologramCamera current = (HologramCamera) target;
            if (!current.ShowHologramPreview)
                return;
            if (!holograms.TryGetValue(current, out HologramEmulation hologram))
                return;
            hologram.OnPreviewGUI(r, background);
        }
        #endregion

        private void DisposeHolograms() {
            removeBuffer.Clear();
            foreach (HologramCamera camera in cameras)
                if (holograms.ContainsKey(camera))
                    removeBuffer.Add(camera);

            foreach (HologramCamera camera in removeBuffer) {
                HologramEmulation hologram = holograms[camera];
                holograms.Remove(camera);
                hologram.Dispose();
            }
        }

        private void DrawSizeHandles(HologramCamera camera) {
            // for some reason, doesn't need the gamma conversion like gizmos do
            Handles.color = camera.Gizmos.HandleColor;

            Matrix4x4 originalMatrix = Handles.matrix;
            Matrix4x4 hpMatrix = Matrix4x4.TRS(
                camera.transform.position,
                camera.transform.rotation,
                    new Vector3(camera.SingleViewCamera.aspect, 1, 1));
            Handles.matrix = hpMatrix;

            float size = camera.CameraProperties.Size;
            Vector3[] dirs = new Vector3[] {
                        new Vector3(-size, 0),
                        new Vector3( size, 0),
                        new Vector3(0, -size),
                        new Vector3(0,  size),
                    };

            foreach (Vector3 direction in dirs) {
                EditorGUI.BeginChangeCheck();
                Vector3 newDir = Handles.Slider(direction, direction, HandleUtility.GetHandleSize(direction) * 0.03f, Handles.DotHandleCap, 0);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(camera, "LookingGlass Size");

                    camera.CameraProperties.Size = Vector3.Dot(newDir, direction.normalized);
                    camera.OnValidate();
                    foreach (PropertyGroup group in camera.PropertyGroups)
                        group.OnValidate();
                    camera.ResetCameras();
                }
            }

            Handles.matrix = originalMatrix;
        }

        private static async Task RefreshAfterCalibrations() {
            await CalibrationSystem.WaitForCalibrations();
            RefreshLKGNames();
        }

        private static void RefreshLKGNames() {
            if (CalibrationSystem.CalibrationCount <= 0) {
                serialNames = new string[0];
                return;
            }
            serialNames = CalibrationSystem.Calibrations.Select(c => c.serial).ToArray();
        }

        public override void OnInspectorGUI() {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            //WARNING: This script has the [CanEditMultipleObjects] attribute, yet we directly use base.target instead of base.targets!!
            HologramCamera hp = (HologramCamera) target;

            if (errorConditions != null) {
                foreach (HologramCamera h in cameras) {
                    foreach (HelpMessageCondition condition in errorConditions) {
                        HelpMessage helpMessage = condition(h);
                        if (helpMessage.HasMessage) {
                            EditorGUILayout.HelpBox(helpMessage.message, helpMessage.type);
                            continue;
                        }
                    }
                }
            }

            changedTargetDisplay = false;
            changedLKGName = false;
            changedEmulatedDevice = false;
            changedQuiltPreset = false;
            requiresPreviewUpdate = false;
            changedTransformMode = false;

            if (versionLabel1 == null) {
                string version = HologramCamera.Version.ToString();
                versionLabel1 = new GUIContent("Version");
                versionLabel2 = new GUIContent(version.ToString(), "LookingGlass Unity Plugin " + version);
            }
            EditorGUILayout.LabelField(versionLabel1, versionLabel2, EditorStyles.miniLabel);
            GUILayout.Space(CondensedLineSpace);

            SectionGUILayout.DrawDefaultInspectorWithSections(serializedObject, Groups, CustomGUI);

            if (changedTransformMode) {
                Undo.IncrementCurrentGroup();
                string undoName = "Set " + nameof(HologramCameraProperties.TransformMode);
                Undo.RecordObject(hp.transform, undoName);
                Undo.RecordObject(hp,           undoName);

                hp.CameraProperties.TransformMode = nextTransformMode;
                serializedObject.Update();
            } else {
                //Let's make sure to save here, in case we used any non-SerializedProperty Editor GUI:
                if (serializedObject.ApplyModifiedProperties())
                    EditorUtility.SetDirty(hp);
            }

            bool needsToSaveAgain = false;
            if (changedTargetDisplay) {
                needsToSaveAgain = true;
                hp.TargetDisplay = hp.TargetDisplay;
            }
            if (changedLKGName) {
                needsToSaveAgain = true;
                hp.TargetLKG = hp.TargetLKG;
            }
            if (changedEmulatedDevice) {
                needsToSaveAgain = true;
                hp.EmulatedDevice = hp.EmulatedDevice;

                //NOTE: This assumes the emulatedDevice field only shows in the inspector when 0 LKG devices are recognized.
                //Thus, all game views should be set to the resolution of the HologramCamera's emulated device
                HologramCameraEditor.UpdateUserGameViews();
            }
            if (changedQuiltPreset) {
                needsToSaveAgain = true;
                hp.QuiltPreset = hp.QuiltPreset;
            }

            if (needsToSaveAgain)
                EditorUtility.SetDirty(hp);
            if (requiresPreviewUpdate) {
                PreviewWindow preview = PreviewWindow.GetPreview(hp);
                if (preview != null)
                    preview.ForceReposition();
                UpdateUserGameViews();
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            GUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel++;
            showCalibrationData = EditorGUILayout.Foldout(showCalibrationData, "Calibration Data");
            if (showCalibrationData) {
                EditorGUI.indentLevel++;
                try {
                    //NOTE: When a custom resolution is NOT being used, we still print out a helpbox with 2 lines of no text,
                    //For UX -- to avoid shifting the view of the text area on the user!
                    EditorGUILayout.HelpBox(hp.IsUsingCustomResolution ? "A custom resolution is being used that differs from the LKG device's native resolution.\n" +
                        "Calibration values may be different than the native device's values." : "\n", MessageType.Info);
                    EditorGUILayout.TextArea(JsonUtility.ToJson(hp.cal, true), GUI.skin.label);
                } finally {
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            LookingGlassGUIUtility.ToggleCastToLKGButton();

            if (GUILayout.Button("Reload Calibration")) {
                hp.UpdateCalibration();
            }
        }

        private bool CustomGUI(SerializedProperty property) {
            HologramCamera hp = cameras[0];
            string propertyName = property.name;

            TransformMode mode = hp.CameraProperties.TransformMode;
            switch (mode) {
                case TransformMode.Volume:
                    //If this property is for the OTHER camera mode,
                    //DON'T draw this property, and return true to tell the GUI that we did custom GUI for this property.
                    if (cameraModeProperties.Contains(propertyName))
                        return true;
                    break;
                case TransformMode.Camera:
                    if (volumeModeProperties.Contains(propertyName))
                        return true;
                    break;
            }

            switch (propertyName) {
                case nameof(HologramCamera.quiltPreset):
                    Rect r = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                    GUIContent label = new("Use Custom Render Settings", UseCustomRenderSettingsTooltip);
                    EditorGUI.BeginProperty(r, label, property);
                    try {
                        EditorGUI.BeginChangeCheck();
                        bool useCustom = EditorGUI.Toggle(r, label, hp.QuiltPreset == QuiltPreset.Custom);
                        if (EditorGUI.EndChangeCheck()) {
                            QuiltPreset[] values = (QuiltPreset[]) Enum.GetValues(typeof(QuiltPreset));
                            Array.Sort(values, (QuiltPreset lhs, QuiltPreset rhs) => ((int) lhs).CompareTo((int) rhs));
                            property.enumValueIndex = Array.IndexOf(values, useCustom ? QuiltPreset.Custom : QuiltPreset.Automatic);
                        }
                    } finally {
                        EditorGUI.EndProperty();
                    }
                    return true;
                case nameof(HologramCamera.customRenderSettings):

                    if (hp.AreRenderSettingsLockedForRecording)
                        EditorGUILayout.HelpBox("Quilt settings are being overriden for recording.", MessageType.Info);

                    using (new EditorGUI.DisabledScope(hp.AreRenderSettingsLockedForRecording)) {
                        if (hp.QuiltPreset == QuiltPreset.Custom) {
                            EditorGUILayout.PropertyField(property);
                        } else {
                            EditorGUILayoutHelper.ReadOnlyRenderSettings(hp.RenderSettings);
                        }
                    }
                    return true;
                case nameof(HologramCamera.targetDisplay):
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property, true);

                    if (EditorGUI.EndChangeCheck()) {
                        changedTargetDisplay = true;
                        requiresPreviewUpdate = true;
                    }
                    return true;
                case nameof(HologramCamera.targetLKG):
                    if (serialNames != null && serialNames.Length > 0) {
                        bool changed = false;

                        for (int i = 0; i < cameras.Length; i++) {
                            int index = Array.IndexOf(serialNames, property.stringValue);
                            if (index < 0 || index >= serialNames.Length) {
                                changed = true;
                                cameras[i].TargetLKG = serialNames[0];
                            }
                        }

                        if (changed) {
                            property.serializedObject.Update();
                            foreach (HologramCamera camera in cameras)
                                camera.UpdateCalibration();
                        }

                        bool wasMixed = EditorGUI.showMixedValue;
                        bool isMixed = property.hasMultipleDifferentValues;
                        EditorGUI.showMixedValue = isMixed;
                        try {
                            int sharedIndex = isMixed ? 0 : Array.IndexOf(serialNames, property.stringValue);

                            EditorGUI.BeginChangeCheck();
                            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));

                            EditorGUI.BeginProperty(rect, new GUIContent("Target LKG"), property);
                            sharedIndex = EditorGUI.Popup(rect, "Target LKG", sharedIndex, serialNames);
                            EditorGUI.EndProperty();

                            if (EditorGUI.EndChangeCheck()) {
                                property.stringValue = serialNames[sharedIndex];
                                changedLKGName = true;
                                requiresPreviewUpdate = true;
                            }
                        } finally {
                            EditorGUI.showMixedValue = wasMixed;
                        }
                    }
                    return true;
                case nameof(HologramCamera.emulatedDevice):
                    //NOTE: We hide this field when at least 1 LKG device (calibration) is found.
                    if (CalibrationSystem.CalibrationCount > 0)
                        return true;

                    using (new EditorGUI.DisabledScope(hp.AreRenderSettingsLockedForRecording)) {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(property);

                        if (EditorGUI.EndChangeCheck()) {
                            changedEmulatedDevice = true;
                            requiresPreviewUpdate = true;
                        }
                    }
                    return true;
                case nameof(HologramCamera.transformMode):
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property);
                    if (EditorGUI.EndChangeCheck()) {
                        changedTransformMode = true;
                        nextTransformMode = (TransformMode) property.enumValueIndex;
                    }
                    return true;
                case nameof(HologramCamera.focalPlane):
                    using (new EditorGUI.DisabledScope(mode != TransformMode.Camera)) {
                        EditorGUILayout.PropertyField(property);
                    }
                    return true;
                case nameof(HologramCamera.size):
                    using (new EditorGUI.DisabledScope(mode == TransformMode.Camera || hp.CameraProperties.SizeMode == SizeMode.ScaleSetsSize)) {
                        EditorGUILayout.PropertyField(property);
                    }
                    return true;
                case nameof(HologramCamera.sizeMode):
                    EditorGUILayout.PropertyField(property);
                    return true;
                case nameof(HologramCamera.quiltTexture):
                    GUILayout.Space(CondensedLineSpace);
                    EditorGUILayout.PropertyField(property);
                    return true;
                case nameof(HologramCamera.renderStack):
                    GUILayout.Space(CondensedLineSpace);
                    EditorGUILayout.PropertyField(property);
                    GUILayout.Space(CondensedLineSpace);
                    return true;
                case nameof(HologramCamera.clearFlags):
                    GUILayout.Space(CondensedLineSpace);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property);

                    if (EditorGUI.EndChangeCheck()) {
                        foreach (HologramCamera h in cameras) {
                            MultiViewRendering.Clear(h.QuiltTexture, CameraClearFlags.SolidColor, Color.clear);
                            MultiViewRendering.Clear(h.Preview2DRT, CameraClearFlags.SolidColor, Color.clear);
                        }
                    }
                    return true;
                case nameof(HologramCamera.manualCalibrationMode):
                case nameof(HologramCamera.calibrationFile):
                case nameof(HologramCamera.manualCalibration):
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(property);
                    if (EditorGUI.EndChangeCheck()) {
                        serializedObject.ApplyModifiedProperties();
                        foreach (HologramCamera h in cameras)
                            h.UpdateCalibration();
                    }
                    return true;
            }
            return false;
        }

        public static bool UpdateUserGameViews() {
            HologramCamera main = HologramCamera.Instance;
            if (main == null)
                return false;

            Vector2Int resolution;
            string deviceTypeName;

            HologramRenderSettings renderSettings = main.RenderSettings;
            Calibration cal = main.UnmodifiedCalibration;
            resolution = new Vector2Int(cal.screenWidth, cal.screenHeight);
            deviceTypeName = main.DeviceTypeName;
            foreach (EditorWindow gameView in GameViewExtensions.FindUnpairedGameViews()) {
                if (gameView.GetGameViewTargetDisplay() == (int) main.TargetDisplay)
                    gameView.SetGameViewResolution(resolution.x, resolution.y, deviceTypeName);
            }

            return true;
        }
    }
}
