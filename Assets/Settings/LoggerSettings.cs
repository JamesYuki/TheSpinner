using UnityEngine;

[CreateAssetMenu(fileName = "LoggerSettings", menuName = "Settings/LoggerSettings")]
public class LoggerSettings : ScriptableObject
{
    [Header("ビルドにログを含めるか（true: 含める, false: 含めない）")]
    public bool enableBuildLog = true;
}
