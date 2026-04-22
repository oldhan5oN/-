using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 场景1管理器 - 简化版本，只保留核心场景切换功能
/// 增加了15天使用时间限制
/// </summary>
public class Scene1Manager : MonoBehaviour
{
    [Header("使用时间限制设置")]
    [SerializeField] private int trialDays = 15; // 试用天数

    private DateTime _firstUseTime; // 首次使用时间
    private const string FIRST_USE_KEY = "FirstUseTime"; // 存储键名

    void Start()
    {
        // 检查使用时间限制
        CheckUsageTimeLimit();
    }

    /// <summary>
    /// 检查使用时间限制
    /// </summary>
    private void CheckUsageTimeLimit()
    {
        // 获取或设置首次使用时间
        if (PlayerPrefs.HasKey(FIRST_USE_KEY))
        {
            // 读取已存储的首次使用时间
            string storedTime = PlayerPrefs.GetString(FIRST_USE_KEY);
            if (DateTime.TryParse(storedTime, out _firstUseTime))
            {
                Debug.Log($"首次使用时间: {_firstUseTime}");
            }
            else
            {
                SetFirstUseTime();
            }
        }
        else
        {
            // 第一次使用，记录当前时间
            SetFirstUseTime();
            return; // 第一次使用不检查限制
        }

        // 计算已使用天数
        TimeSpan usageTime = DateTime.Now - _firstUseTime;
        int daysUsed = usageTime.Days;

        Debug.Log($"已使用天数: {daysUsed}/{trialDays}");

        // 检查是否超过限制
        if (daysUsed >= trialDays)
        {
            Debug.LogError($"试用期已结束！已使用{daysUsed}天，限制{trialDays}天");
            ShowExpirationMessage();
            QuitApplication();
        }
        else if (daysUsed >= trialDays - 3) // 提前3天警告
        {
            int remainingDays = trialDays - daysUsed;
            Debug.LogWarning($"试用期即将结束！剩余{remainingDays}天");
            ShowWarningMessage(remainingDays);
        }
    }

    /// <summary>
    /// 设置首次使用时间
    /// </summary>
    private void SetFirstUseTime()
    {
        _firstUseTime = DateTime.Now;
        PlayerPrefs.SetString(FIRST_USE_KEY, _firstUseTime.ToString());
        PlayerPrefs.Save();
        Debug.Log($"记录首次使用时间: {_firstUseTime}");
    }

    /// <summary>
    /// 显示过期消息
    /// </summary>
    private void ShowExpirationMessage()
    {
        // 这里可以添加UI显示逻辑
        // 暂时使用Debug.Log和简单的UI提示
#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog("试用期结束", "您的15天试用期已结束，应用程序将退出。", "确定");
#else
        // 在构建版本中，您可以使用自己的UI系统显示消息
        // 例如：MessageBox.Show("试用期结束", "您的15天试用期已结束，应用程序将退出。");
#endif
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    private void ShowWarningMessage(int remainingDays)
    {
        // 这里可以添加UI显示逻辑
#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog("试用期提醒", $"您的试用期还剩{remainingDays}天，请及时处理。", "确定");
#else
        // 在构建版本中，您可以使用自己的UI系统显示消息
        // 例如：MessageBox.Show("试用期提醒", $"您的试用期还剩{remainingDays}天，请及时处理。");
#endif
    }

    /// <summary>
    /// 重置使用时间（用于测试或正式授权）
    /// </summary>
    public void ResetUsageTime()
    {
        PlayerPrefs.DeleteKey(FIRST_USE_KEY);
        PlayerPrefs.Save();
        Debug.Log("使用时间已重置");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog("重置成功", "使用时间已重置，可以重新开始试用。", "确定");
#endif
    }

    /// <summary>
    /// 获取剩余试用天数
    /// </summary>
    public int GetRemainingDays()
    {
        if (PlayerPrefs.HasKey(FIRST_USE_KEY))
        {
            string storedTime = PlayerPrefs.GetString(FIRST_USE_KEY);
            if (DateTime.TryParse(storedTime, out DateTime firstUse))
            {
                TimeSpan usageTime = DateTime.Now - firstUse;
                return Math.Max(0, trialDays - usageTime.Days);
            }
        }
        return trialDays;
    }

    /// <summary>
    /// 检查是否已过期
    /// </summary>
    private bool IsExpired()
    {
        if (PlayerPrefs.HasKey(FIRST_USE_KEY))
        {
            string storedTime = PlayerPrefs.GetString(FIRST_USE_KEY);
            if (DateTime.TryParse(storedTime, out DateTime firstUse))
            {
                TimeSpan usageTime = DateTime.Now - firstUse;
                return usageTime.Days >= trialDays;
            }
        }
        return false;
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("应用程序退出");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnApplicationQuit()
    {
        Debug.Log($"应用程序退出时间: {DateTime.Now}");
    }
}