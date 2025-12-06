#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ManagerEditor
{
    private static bool IsPlaying() => !EditorApplication.isPlaying;

    #region 초기화
    private static void ResetInspector(Component _comp)
    {
        Undo.RegisterCompleteObjectUndo(_comp, "Reset");
        Unsupported.SmartReset(_comp);
        EditorUtility.SetDirty(_comp);
        EditorSceneManager.MarkSceneDirty(_comp.gameObject.scene);
    }

    private static void ResetAll(Type _type)
    {
        var _objs = UnityEngine.Object.FindObjectsByType(
            _type, FindObjectsInactive.Include, FindObjectsSortMode.None
        );
        for (int i = 0; i < _objs.Length; i++)
        {
            var _comp = _objs[i] as Component;
            if (_comp != null) ResetInspector(_comp);
        }
    }

    [MenuItem("Tools/스크립트 초기화", true)]
    private static bool ResetManagers_Validate() => IsPlaying();
    [MenuItem("Tools/스크립트 초기화", false, 1)]
    private static void ResetManagers()
    {
        var _types = new Type[]
        {
            typeof(GameManager),
            typeof(SoundManager),
            typeof(EntityManager),
            typeof(HandleManager),
            typeof(UIManager),
            typeof(ADManager),
            typeof(TestManager),
            typeof(AutoCamera),
            typeof(AutoUICanvas),
            typeof(AutoBackground),
            typeof(HoleSystem),
        };
        for (int i = 0; i < _types.Length; i++) ResetAll(_types[i]);
    }
    #endregion

    #region 켜기/끄기
    private static T FindSingle<T>() where T : Component
        => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

    private static bool AnyActive<T>() where T : Component
    {
        var c = FindSingle<T>();
        if (c == null) return false;
        var go = c.gameObject;
        return (go != null) && go.activeSelf;
    }

    private static void SetActive<T>(bool _on, string _onLabel, string _offLabel) where T : Component
    {
        var c = FindSingle<T>();
        if (c == null) return;
        var go = c.gameObject;
        if (go == null) return;

        Undo.RegisterFullObjectHierarchyUndo(go, _on ? _onLabel : _offLabel);
        go.SetActive(_on);
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static void SetBtnActive(string _name, bool _on, string _undoOn, string _undoOff)
    {
        var _objs = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        for (int i = 0; i < _objs.Length; i++)
        {
            var _obj = _objs[i];
            if (_obj == null || _obj.name != _name) continue;

            Undo.RegisterFullObjectHierarchyUndo(_obj, _on ? _undoOn : _undoOff);
            _obj.SetActive(_on);
            EditorUtility.SetDirty(_obj);
            EditorSceneManager.MarkSceneDirty(_obj.scene);
        }
    }

    private static void SetTestActive(bool _on)
    {
        SetActive<TestManager>(_on, "테스트 켜기", "테스트 끄기");
        SetBtnActive("TestBtn", _on, "테스트 버튼 켜기", "테스트 버튼 끄기");
    }

    private static void SetQuitActive(bool _on)
        => SetBtnActive("QuitBtn", _on, "종료 버튼 켜기", "종료 버튼 끄기");

    private static bool AnyQuitOn()
    {
        var objs = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        for (int i = 0; i < objs.Length; i++)
        {
            var obj = objs[i];
            if (obj == null || obj.name != "QuitBtn") continue;
            if (obj.activeSelf) return true;
        }
        return false;
    }

    private static bool AnyQuitOff()
    {
        var objs = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None
        );

        for (int i = 0; i < objs.Length; i++)
        {
            var obj = objs[i];
            if (obj == null || obj.name != "QuitBtn") continue;
            if (!obj.activeSelf) return true;
        }
        return false;
    }
    #endregion

    #region 빌드
    private static void PrepareTest()
    {
        SetActive<UIManager>(true, "UI 켜기", "UI 끄기");
        SetActive<ADManager>(false, "광고 켜기", "광고 끄기");
        SetTestActive(true);
        SetQuitActive(true);

        FindSingle<UIManager>()?.SetCountdown(true);
    }

    private static void PrepareAndroid()
    {
        SetActive<UIManager>(true, "UI 켜기", "UI 끄기");
        SetActive<ADManager>(true, "광고 켜기", "광고 끄기");
        SetTestActive(false);
        SetQuitActive(true);

        FindSingle<UIManager>()?.SetCountdown(false);
    }

    private static void PrepareWebGL()
    {
        SetActive<UIManager>(true, "UI 켜기", "UI 끄기");
        SetActive<ADManager>(false, "광고 켜기", "광고 끄기");
        SetTestActive(false);
        SetQuitActive(false);

        FindSingle<UIManager>()?.SetCountdown(false);
    }

    [MenuItem("Tools/Test 빌드 준비", true)]
    private static bool TestBuildValidate() => IsPlaying();
    [MenuItem("Tools/Test 빌드 준비", false, 101)]
    private static void TestBuild() => PrepareTest();

    [MenuItem("Tools/Android 빌드 준비", true)]
    private static bool AndroidBuildValidate() => IsPlaying();
    [MenuItem("Tools/Android 빌드 준비", false, 102)]
    private static void AndroidBuild() => PrepareAndroid();

    [MenuItem("Tools/WebGL 빌드 준비", true)]
    private static bool WebGLBuildValidate() => IsPlaying();
    [MenuItem("Tools/WebGL 빌드 준비", false, 103)]
    private static void WebGLBuild() => PrepareWebGL();
    #endregion

    #region UI
    [MenuItem("Tools/UI 켜기", true)]
    private static bool UIsOnValidate() => IsPlaying() && !AnyActive<UIManager>();
    [MenuItem("Tools/UI 켜기", false, 201)]
    private static void UIsOn() => SetActive<UIManager>(true, "UI 켜기", "UI 끄기");

    [MenuItem("Tools/UI 끄기", true)]
    private static bool UIsOffValidate() => IsPlaying() && AnyActive<UIManager>();
    [MenuItem("Tools/UI 끄기", false, 202)]
    private static void UIsOff() => SetActive<UIManager>(false, "UI 켜기", "UI 끄기");
    #endregion

    #region 광고
    [MenuItem("Tools/광고 켜기", true)]
    private static bool ADsOnValidate() => IsPlaying() && !AnyActive<ADManager>();
    [MenuItem("Tools/광고 켜기", false, 301)]
    private static void ADsOn() => SetActive<ADManager>(true, "광고 켜기", "광고 끄기");

    [MenuItem("Tools/광고 끄기", true)]
    private static bool ADsOff_Validate() => IsPlaying() && AnyActive<ADManager>();
    [MenuItem("Tools/광고 끄기", false, 302)]
    private static void ADsOff() => SetActive<ADManager>(false, "광고 켜기", "광고 끄기");
    #endregion

    #region 테스트
    [MenuItem("Tools/테스트 켜기", true)]
    private static bool TestsOnValidate() => IsPlaying() && !AnyActive<TestManager>();
    [MenuItem("Tools/테스트 켜기", false, 401)]
    private static void TestsOn() => SetTestActive(true);

    [MenuItem("Tools/테스트 끄기", true)]
    private static bool TestsOffValidate() => IsPlaying() && AnyActive<TestManager>();
    [MenuItem("Tools/테스트 끄기", false, 402)]
    private static void TestsOff() => SetTestActive(false);
    #endregion

    #region 종료
    [MenuItem("Tools/종료 버튼 켜기", true)]
    private static bool QuitBtnsOnValidate() => IsPlaying() && !AnyQuitOn() && AnyQuitOff();
    [MenuItem("Tools/종료 버튼 켜기", false, 501)]
    private static void QuitBtnsOn() => SetQuitActive(true);

    [MenuItem("Tools/종료 버튼 끄기", true)]
    private static bool QuitBtnsOffValidate() => IsPlaying() && AnyQuitOn();
    [MenuItem("Tools/종료 버튼 끄기", false, 502)]
    private static void QuitBtnsOff() => SetQuitActive(false);
    #endregion
}
#endif
