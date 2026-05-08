using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("游戏主场景名称")]
    public string mainSceneName = "111";

    [Header("视频设置")]
    [Tooltip("初始循环视频播放器")]
    public VideoPlayer introVideoPlayer;

    [Tooltip("过渡视频播放器")]
    public VideoPlayer transitionVideoPlayer;

    [Tooltip("过渡视频播放完后自动进入游戏")]
    public bool autoEnterGameAfterVideo = true;

    [Header("输入设置")]
    [Tooltip("进入游戏按钮")]
    public KeyCode enterGameKey = KeyCode.Return;

    [Tooltip("重启游戏按钮")]
    public KeyCode restartKey = KeyCode.R;

    [Tooltip("退出游戏按钮（长按）")]
    public KeyCode exitKey = KeyCode.Escape;

    [Tooltip("长按退出时间（秒）")]
    public float longPressDuration = 2f;

    [Header("状态")]
    public GameState currentState;

    private float exitKeyHoldTime;
    private bool isExiting;

    public enum GameState
    {
        IntroVideo,      // 初始循环视频
        TransitionVideo, // 过渡视频
        MainGame,        // 主游戏场景
        Restarting       // 重启中
    }

    private void Start()
    {
        // 初始状态：播放循环视频
        EnterIntroVideoState();
    }

    private void Update()
    {
        HandleInput();
        UpdateExitKeyHold();
    }

    /// <summary>
    /// 处理所有输入
    /// </summary>
    private void HandleInput()
    {
        switch (currentState)
        {
            case GameState.IntroVideo:
                HandleIntroVideoInput();
                break;

            case GameState.TransitionVideo:
                // 过渡视频期间不允许重启
                break;

            case GameState.MainGame:
                HandleMainGameInput();
                break;

            case GameState.Restarting:
                // 重启中不处理输入
                break;
        }
    }

    /// <summary>
    /// 初始视频界面输入
    /// </summary>
    private void HandleIntroVideoInput()
    {
        // 按下回车键：进入过渡视频
        if (Input.GetKeyDown(enterGameKey))
        {
            EnterTransitionVideoState();
        }
    }

    /// <summary>
    /// 主游戏界面输入
    /// </summary>
    private void HandleMainGameInput()
    {
        // 按下 R 键：重启游戏（不播放视频）
        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }

        // 长按 Escape 键：退出游戏回到初始视频
        if (Input.GetKey(exitKey))
        {
            exitKeyHoldTime += Time.deltaTime;

            if (exitKeyHoldTime >= longPressDuration && !isExiting)
            {
                ExitToIntro();
            }
        }
        else if (Input.GetKeyUp(exitKey))
        {
            exitKeyHoldTime = 0f;
        }
    }

    /// <summary>
    /// 更新退出按键长按状态
    /// </summary>
    private void UpdateExitKeyHold()
    {
        if (currentState == GameState.MainGame && Input.GetKey(exitKey))
        {
            // 显示长按进度（可选）
            float progress = exitKeyHoldTime / longPressDuration;
            Debug.Log($"退出进度：{progress:P0}");
        }
    }

    /// <summary>
    /// 进入初始循环视频状态
    /// </summary>
    private void EnterIntroVideoState()
    {
        currentState = GameState.IntroVideo;
        Debug.Log("进入初始视频界面");

        // 确保在主场景或视频场景
        if (!SceneManager.GetActiveScene().name.Contains("Video"))
        {
            // 如果需要，可以加载视频场景
        }

        // 播放循环视频
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached += OnIntroVideoLoop;
            introVideoPlayer.Play();
        }
    }

    /// <summary>
    /// 进入过渡视频状态
    /// </summary>
    private void EnterTransitionVideoState()
    {
        currentState = GameState.TransitionVideo;
        Debug.Log("进入过渡视频");

        // 停止初始视频
        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        // 播放过渡视频
        if (transitionVideoPlayer != null)
        {
            transitionVideoPlayer.loopPointReached += OnTransitionVideoEnd;
            transitionVideoPlayer.Play();
        }
        else
        {
            // 如果没有过渡视频，直接进入游戏
            if (autoEnterGameAfterVideo)
            {
                EnterMainGame();
            }
        }
    }

    /// <summary>
    /// 进入主游戏场景
    /// </summary>
    public void EnterMainGame()
    {
        currentState = GameState.MainGame;
        Debug.Log("进入主游戏场景");

        // 停止所有视频
        StopAllVideos();

        // 加载主场景
        if (SceneManager.GetActiveScene().name != mainSceneName)
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }

    /// <summary>
    /// 重启游戏（不播放视频）
    /// </summary>
    public void RestartGame()
    {
        if (currentState == GameState.Restarting)
            return;

        currentState = GameState.Restarting;
        Debug.Log("重启游戏");

        // 停止所有视频
        StopAllVideos();

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // 重启后状态
        currentState = GameState.MainGame;
    }

    /// <summary>
    /// 退出到初始视频
    /// </summary>
    private void ExitToIntro()
    {
        if (isExiting)
            return;

        isExiting = true;
        Debug.Log("退出到初始视频");

        // 停止所有视频
        StopAllVideos();

        // 重置长按时间
        exitKeyHoldTime = 0f;

        // 加载初始场景（如果需要）
        // SceneManager.LoadScene("IntroScene");

        // 进入初始视频状态
        EnterIntroVideoState();

        isExiting = false;
    }

    /// <summary>
    /// 停止所有视频
    /// </summary>
    private void StopAllVideos()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoLoop;
            introVideoPlayer.Stop();
        }

        if (transitionVideoPlayer != null)
        {
            transitionVideoPlayer.loopPointReached -= OnTransitionVideoEnd;
            transitionVideoPlayer.Stop();
        }
    }

    /// <summary>
    /// 初始视频循环回调
    /// </summary>
    private void OnIntroVideoLoop(VideoPlayer vp)
    {
        // 循环播放，不需要处理
    }

    /// <summary>
    /// 过渡视频结束回调
    /// </summary>
    private void OnTransitionVideoEnd(VideoPlayer vp)
    {
        if (autoEnterGameAfterVideo)
        {
            EnterMainGame();
        }
    }

    /// <summary>
    /// 公开方法：外部调用进入游戏
    /// </summary>
    public void PublicEnterGame()
    {
        switch (currentState)
        {
            case GameState.IntroVideo:
                EnterTransitionVideoState();
                break;

            case GameState.TransitionVideo:
                // 跳过过渡视频
                EnterMainGame();
                break;

            case GameState.MainGame:
                // 已经在游戏中，不做任何事
                break;
        }
    }

    /// <summary>
    /// 公开方法：外部调用重启
    /// </summary>
    public void PublicRestart()
    {
        if (currentState == GameState.MainGame)
        {
            RestartGame();
        }
    }

    /// <summary>
    /// 公开方法：外部调用退出
    /// </summary>
    public void PublicExit()
    {
        ExitToIntro();
    }
}
