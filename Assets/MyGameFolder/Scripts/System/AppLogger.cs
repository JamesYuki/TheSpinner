using UnityEngine;

/// <summary>
/// ログ出力設定クラス
/// </summary>
public static class AppLoggerSettings
{
    /// <summary>
    /// ログ出力を含めるか（true: ログ出力あり, false: ログ出力なし）
    /// </summary>
    public static bool EnableLog = true;

    /// <summary>
    /// エディタ実行時にログ出力するか
    /// </summary>
    public static bool EnableEditorLog = true;

    /// <summary>
    /// ビルド（実機）時にログ出力するか
    /// </summary>
    public static bool EnableBuildLog = true;
}

public static class AppLogger
{

    public static void Log(object message)
    {
        if (AppLoggerSettings.EnableLog)
        {
#if UNITY_EDITOR
            if (AppLoggerSettings.EnableEditorLog)
                Debug.Log(message);
#else
                if (AppLoggerSettings.EnableBuildLog)
                    Debug.Log(message);
#endif
        }
    }

    public static void LogWarning(object message)
    {
        if (AppLoggerSettings.EnableLog)
        {
#if UNITY_EDITOR
            if (AppLoggerSettings.EnableEditorLog)
                Debug.LogWarning(message);
#else
                if (AppLoggerSettings.EnableBuildLog)
                    Debug.LogWarning(message);
#endif
        }
    }

    public static void LogError(object message)
    {
        if (AppLoggerSettings.EnableLog)
        {
#if UNITY_EDITOR
            if (AppLoggerSettings.EnableEditorLog)
                Debug.LogError(message);
#else
                if (AppLoggerSettings.EnableBuildLog)
                    Debug.LogError(message);
#endif
        }
    }

    public static void Log(object message, GameObject origin)
    {
        if (!AppLoggerSettings.EnableLog) return;
        var originInfo = origin != null ? $"[Origin: {origin.name}] " : "";
#if UNITY_EDITOR
        if (AppLoggerSettings.EnableEditorLog)
            Debug.Log(originInfo + message);
#else
            if (AppLoggerSettings.EnableBuildLog)
                Debug.Log(originInfo + message);
#endif
    }

    public static void LogWarning(object message, GameObject origin)
    {
        if (!AppLoggerSettings.EnableLog) return;
        var originInfo = origin != null ? $"[Origin: {origin.name}] " : "";
#if UNITY_EDITOR
        if (AppLoggerSettings.EnableEditorLog)
            Debug.LogWarning(originInfo + message);
#else
            if (AppLoggerSettings.EnableBuildLog)
                Debug.LogWarning(originInfo + message);
#endif
    }

    public static void LogError(object message, GameObject origin)
    {
        if (!AppLoggerSettings.EnableLog) return;
        var originInfo = origin != null ? $"[Origin: {origin.name}] " : "";
#if UNITY_EDITOR
        if (AppLoggerSettings.EnableEditorLog)
            Debug.LogError(originInfo + message);
#else
            if (AppLoggerSettings.EnableBuildLog)
                Debug.LogError(originInfo + message);
#endif
    }
}
