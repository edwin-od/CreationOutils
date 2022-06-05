using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class AnimatorManipulationWindow : EditorWindow
{
    static string savePath = "Assets/_Creation Outils/Editor/Save/AnimatorToolSave.asset";
    static AnimatorToolSaveScriptableObject save;

    float timelineTickSmall = 1f;
    float timelineTickBig = 2f;
    float minDistTicks = 8.5f;

    static UnityEngine.SceneManagement.Scene currentScene;

    static List<Animator> sceneAnimators = new List<Animator>();
    static List<AnimationClip> animations = new List<AnimationClip>();

    static GUIStyle hierarchySelectedPauseTextStyle = new GUIStyle();
    static GUIStyle hierarchySelectedPlayTextStyle = new GUIStyle();

    static Color defaultBackground;

    static bool playing = false;
    static bool looping = true;

    static float speed = 1;

    double t0;
    static float elapsedTime = 0;

    static int tab = 0;
    static int animationIndex = 0;
    static int animatorIndex = 0;
    static string[] tabs = new string[] { "Animators", "Settings" };
    static Vector2 scrollPos = Vector2.zero;

    static (float, bool) lastSave = (1, true);

    [MenuItem("Window/Animator Tool")]
    public static void Open()
    {
        defaultBackground = GUI.backgroundColor;

        AnimatorManipulationSceneViewListener.window = GetWindow<AnimatorManipulationWindow>("Animator Tool");

        if (!FindSaveFile()) CreateSaveFile();

        Setup();

        hierarchySelectedPauseTextStyle.normal = new GUIStyleState { textColor = save.GetSettings().hierarchySelectedTextColorPause };
        hierarchySelectedPlayTextStyle.normal = new GUIStyleState { textColor = save.GetSettings().hierarchySelectedTextColorPlay };
    }

    public void SelectAnimator(Animator anim)
    {
        for (int i = 0; i < sceneAnimators.Count; ++i)
        {
            if (sceneAnimators[i] == anim)
            {
                animatorIndex = i;
                ResetAnimationClipsList();
                RepaintHierarchy();
                Repaint();
                break;
            }
        }
    }

    public bool IsAlreadySelected(Animator anim) => sceneAnimators.Count > 0 && anim == sceneAnimators[animatorIndex];

    private void OnEnable()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyUpdate;
        EditorApplication.playModeStateChanged += UpdateEditorModeStatus;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneChanged;
    }

    private void OnDisable()
    {
        StopAnimation();
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyUpdate;
        EditorApplication.playModeStateChanged -= UpdateEditorModeStatus;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneChanged;
    }

    private static void CreateSaveFile()
    {
        save = CreateInstance<AnimatorToolSaveScriptableObject>();
        save.Init();
        AssetDatabase.CreateAsset(save, savePath);
        Save();
    }

    private static bool FindSaveFile()
    {
        save = (AnimatorToolSaveScriptableObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(AnimatorToolSaveScriptableObject));
        return save;
    }

    private static void Save()
    {
        EditorUtility.SetDirty(save);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void GetOrSaveAnimationData(bool get = true)
    {
        if (sceneAnimators.Count == 0 || animations.Count == 0) return;

        try
        {
            if (get)
            {
                AnimatorToolSaveScriptableObject.ToolData data = save.Get();
                if (data != null)
                {
                    speed = data.speed;
                    looping = data.looping;
                    lastSave = (speed, looping);
                }
                else
                {
                    speed = 1;
                    looping = true;
                    SetAnimationTime(0);
                    lastSave = (speed, looping);
                }
            }
            else
            {
                save.SaveToolData(speed, looping);
                lastSave = (speed, looping);
                Save();
            }
        } 
        catch (NullReferenceException)
        {
            Debug.LogWarning("Animator tool save file reset!");
            save.Init();
            Save();
        }
    }

    private void OnGUI()
    {
        tab = GUILayout.Toolbar(tab, tabs);
        switch (tab)
        {
            case 0:
                {
                    if (sceneAnimators.Count > 0)
                    {
                        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

                        GUILayout.Space(20);

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Animator");
                        int oldAnimator = animatorIndex;
                        animatorIndex = EditorGUILayout.Popup(animatorIndex, sceneAnimators.Select(a => a.gameObject.name).ToArray(), GUILayout.Height(20));
                        if (animatorIndex != oldAnimator)
                        {
                            StopAnimation();

                            animationIndex = 0;
                            ResetAnimationClipsList();
                            RepaintHierarchy();
                            SetAnimationTime(0);
                        }
                        GUILayout.EndHorizontal();

                        Separator(60);

                        if (animations.Count > 0)
                        {
                            List<string> labels = new List<string>();
                            foreach (AnimationClip selected in animations)
                                labels.Add(selected.name);

                            int nb = (int)(position.width) / 250;
                            nb = nb > 0 ? nb : 1;
                            int hNb = (int)Mathf.Ceil(animations.Count / (float)nb);
                            int oldAnimSel = animationIndex;
                            float cellH = hNb * 20;
                            animationIndex = GUI.SelectionGrid(new Rect(0, 85, position.width, cellH), animationIndex, labels.ToArray(), nb);
                            if (oldAnimSel != animationIndex)
                            {
                                StopAnimation();

                                SetAnimationTime(0);
                            }

                            Separator(cellH + 105);
                            GUILayout.Space((hNb + 4) * 20);

                            GUILayout.BeginHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            // save
                            bool canSave = speed != lastSave.Item1 || looping != lastSave.Item2;
                            EditorGUI.BeginDisabledGroup(!canSave);
                            if (GUILayout.Button(EditorGUIUtility.IconContent("SaveAs@2x"), GUILayout.Height(30), GUILayout.MaxWidth(30)))
                                GetOrSaveAnimationData(false);
                            EditorGUI.EndDisabledGroup();

                            GUILayout.Space(5);

                            // Reset to default
                            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh@2x"), GUILayout.Height(30), GUILayout.MaxWidth(30)))
                            {
                                speed = 1;
                                looping = true;
                                SetAnimationTime(0);
                            }

                            float extraPad = 0;
                            // Reset to last save
                            if (canSave)
                            {
                                GUILayout.Space(5);
                                extraPad = 38;
                                if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_win_close@2x"), GUILayout.Height(30), GUILayout.MaxWidth(30)))
                                    GetOrSaveAnimationData();
                            }

                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Space(100 - extraPad);
                            SetAnimationTime(Mathf.Clamp(EditorGUILayout.FloatField(elapsedTime, GUILayout.Width(60)), 0, animations[animationIndex].length));
                            GUILayout.Label("/  " + animations[animationIndex].length);
                            GUILayout.Space(20);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Speed");
                            speed = EditorGUILayout.FloatField(speed, GUILayout.Width(50));
                            speed = Mathf.Max(speed, 0.1f);
                            GUILayout.Space(10);
                            looping = GUILayout.Toggle(looping, "Loop");
                            GUILayout.Space(20);
                            GUILayout.EndHorizontal();

                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            EditorGUI.BeginDisabledGroup(animations.Count == 1);
                            if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.PrevKey"), GUILayout.Height(20), GUILayout.MaxWidth(25)))
                            {
                                StopAnimation();
                                SetAnimationTime(0);
                                --animationIndex;
                                if (animationIndex < 0) animationIndex = animations.Count - 1;
                                GetOrSaveAnimationData();
                            }
                            EditorGUI.EndDisabledGroup();

                            GUI.backgroundColor = playing ? save.GetSettings().buttonPlayColor : save.GetSettings().buttonPauseColor;
                            if (GUILayout.Button(playing ? EditorGUIUtility.IconContent("d_PauseButton") : EditorGUIUtility.IconContent("d_PlayButton"), GUILayout.Height(20), GUILayout.MaxWidth(40)))
                                StartStopAnimation(!playing);
                            GUI.backgroundColor = defaultBackground;

                            EditorGUI.BeginDisabledGroup(animations.Count == 1);
                            if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.NextKey"), GUILayout.Height(20), GUILayout.MaxWidth(25)))
                            {
                                StopAnimation();
                                SetAnimationTime(0);
                                ++animationIndex;
                                animationIndex %= animations.Count;
                                GetOrSaveAnimationData();
                            }
                            EditorGUI.EndDisabledGroup();

                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            Rect timeline = new Rect(0, (10 + hNb) * 20, position.width, 40);
                            EditorGUI.DrawRect(timeline, save.GetSettings().timelineBackgroundColor);

                            float width = animations[animationIndex].length * (minDistTicks + timelineTickSmall);
                            int intermediateTicks = width * 50 < position.width ? 50 : width * 25 < position.width ? 25 : width * 10 < position.width ? 10 : width * 5 < position.width ? 5 : width * 2 < position.width ? 2 : 1;
                            width *= intermediateTicks;

                            float tot = intermediateTicks * animations[animationIndex].length;
                            for (int i = 0; i < tot; ++i)
                            {
                                float thickness;
                                Color color;
                                if (i % intermediateTicks == 0)
                                {
                                    thickness = timelineTickBig;
                                    color = Color.white;
                                }
                                else
                                {
                                    thickness = timelineTickSmall;
                                    color = Color.gray;
                                }
                                EditorGUI.DrawRect(new Rect((i * (position.width / tot)) - (thickness / 2), timeline.y, thickness, timeline.height), color);
                            }

                            EditorGUI.DrawRect(new Rect((elapsedTime / animations[animationIndex].length) * position.width - 0.25f, timeline.y, 2.5f, timeline.height), Color.red);

                            Event e = Event.current;
                            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && timeline.Contains(e.mousePosition))
                            {
                                SetAnimationTime((e.mousePosition.x / position.width) * animations[animationIndex].length);
                                Repaint();
                            }

                            // avoid persisting field selections (ex: float field)
                            if (e.type == EventType.MouseDown)
                            {
                                GUI.FocusControl(null);
                                Repaint();
                            }

                            GUILayout.Space(85);
                        }
                        else
                        {
                            GUILayout.Space(30);
                            EditorGUILayout.HelpBox("No animation clips can be found on the current selected animator: " + sceneAnimators[animatorIndex].gameObject.name, MessageType.Warning);
                        }

                        EditorGUILayout.EndScrollView();
                    }
                    else
                    {
                        GUILayout.Space(15);
                        EditorGUILayout.HelpBox("No animators on the current scene: " + currentScene.name, MessageType.Warning);
                    }
                }
                break;
            case 1:
                {
                    EditorGUI.BeginChangeCheck();
                    save.GetSettings().hierarchySelectedTextColorPause = EditorGUILayout.ColorField("Hierarchy Pause Text", save.GetSettings().hierarchySelectedTextColorPause);
                    save.GetSettings().hierarchySelectedTextColorPlay = EditorGUILayout.ColorField("Hierarchy Play Text", save.GetSettings().hierarchySelectedTextColorPlay);
                    EditorGUILayout.Space(5);
                    save.GetSettings().hierarchySelectedColorPause = EditorGUILayout.ColorField("Hierarchy Pause Rect", save.GetSettings().hierarchySelectedColorPause);
                    save.GetSettings().hierarchySelectedColorPlay = EditorGUILayout.ColorField("Hierarchy Play Rect", save.GetSettings().hierarchySelectedColorPlay);
                    if (EditorGUI.EndChangeCheck())
                    {
                        hierarchySelectedPauseTextStyle.normal = new GUIStyleState { textColor = save.GetSettings().hierarchySelectedTextColorPause };
                        hierarchySelectedPlayTextStyle.normal = new GUIStyleState { textColor = save.GetSettings().hierarchySelectedTextColorPlay };
                        RepaintHierarchy();
                        Save();
                    }
                    EditorGUILayout.Space(20);
                    EditorGUI.BeginChangeCheck();
                    save.GetSettings().buttonPauseColor = EditorGUILayout.ColorField("Button Pause", save.GetSettings().buttonPauseColor);
                    save.GetSettings().buttonPlayColor = EditorGUILayout.ColorField("Button Play", save.GetSettings().buttonPlayColor);
                    if (EditorGUI.EndChangeCheck()) Save();
                }
                break;
            default:
                goto case 0;
        }
    }

    private void UpdateEditorModeStatus(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && playing)
        {
            Debug.LogWarning("Unable to play animation while in Play Mode! Animation Stopped.");
            StopAnimation();
        }
    }

    private void StartStopAnimation(bool status)
    {
        if (status) StartAnimation();
        else StopAnimation();   
    }

    private void StartAnimation()
    {
        if (playing) return;
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Unable to play animation while in Play Mode!");
            return;
        }

        AnimationMode.StartAnimationMode();
        
        t0 = EditorApplication.timeSinceStartup;
        elapsedTime = elapsedTime > animations[animationIndex].length ? 0 : elapsedTime;

        EditorApplication.update += UpdateAnimationClip;
        playing = true;
    }

    private void StopAnimation()
    {
        if (!playing) return;

        AnimationMode.StopAnimationMode();

        EditorApplication.update -= UpdateAnimationClip;
        playing = false;
    }

    private void UpdateAnimationClip()
    {
        if (animations.Count > 0)
        {
            elapsedTime += (float)(EditorApplication.timeSinceStartup - t0) * speed;
            if (looping) elapsedTime %= animations[animationIndex].length;
            else if (elapsedTime > animations[animationIndex].length)
            {
                StopAnimation();
                elapsedTime = 0;
            }
            t0 = EditorApplication.timeSinceStartup;

            Sample();

            Repaint();
        }
    }

    private static void SetAnimationTime(float time)
    {
        elapsedTime = time;
        Sample();
    }

    private static void Sample()
    {
        if (sceneAnimators.Count == 0 || animations.Count == 0) return;

        Vector3 oldPos = sceneAnimators[animatorIndex].transform.position;
        animations[animationIndex].SampleAnimation(sceneAnimators[animatorIndex].gameObject, Mathf.Clamp(elapsedTime, 0, animations[animationIndex].length));
        sceneAnimators[animatorIndex].transform.position = oldPos;
    }

    private static void ResetAnimationClipsList()
    {
        if (sceneAnimators.Count == 0) return;

        RuntimeAnimatorController cont = sceneAnimators[animatorIndex].runtimeAnimatorController;
        AnimationClip[] clips = cont?.animationClips ?? new AnimationClip[0];
        animations = clips.ToList();

        Selection.activeGameObject = sceneAnimators[animatorIndex].gameObject;
        SceneView.FrameLastActiveSceneView();

        GetOrSaveAnimationData();
    }

    static void OnHierarchyUpdate(int instanceID, Rect selectionRect)
    {
        if (animatorIndex < 0 || animatorIndex > sceneAnimators.Count || sceneAnimators.Count == 0) return;

        Animator animator = null;
        if (((EditorUtility.InstanceIDToObject(instanceID) as GameObject)?.TryGetComponent(out animator) ?? false) && animator == sceneAnimators[animatorIndex])
        {
            EditorGUI.DrawRect(selectionRect, playing ? save.GetSettings().hierarchySelectedColorPlay : save.GetSettings().hierarchySelectedColorPause);
            EditorGUI.LabelField(selectionRect, animator.gameObject.name, playing ? hierarchySelectedPlayTextStyle : hierarchySelectedPauseTextStyle);
        }
        RepaintHierarchy();
    }
    private static void RepaintHierarchy() => EditorApplication.RepaintHierarchyWindow();

    private static void Setup()
    {
        currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        sceneAnimators = new List<Animator>();
        animations = new List<AnimationClip>();
        animatorIndex = 0;
        animationIndex = 0;

        GameObject[] sceneObjects = currentScene.GetRootGameObjects();
        foreach (GameObject go in sceneObjects)
            AddAllChildrenAnimators(go.transform);

        ResetAnimationClipsList();
        RepaintHierarchy();

        GetOrSaveAnimationData();

        void AddAllChildrenAnimators(Transform go)
        {
            if (go.TryGetComponent(out Animator animator))
                sceneAnimators.Add(animator);

            for (int i = 0; i < go.childCount; ++i)
                AddAllChildrenAnimators(go.GetChild(i));
        }

        playing = false;
    }

    private void SceneChanged(UnityEngine.SceneManagement.Scene scene1, UnityEngine.SceneManagement.Scene scene2)
    {
        StopAnimation();
        Setup();
    }

    private void Separator(float height, float thickness = 2f)
    {
        EditorGUI.DrawRect(new Rect(new Vector2(0, height), new Vector2(position.width, thickness)), new Color(0.35f, 0.35f, 0.35f));
    }
}