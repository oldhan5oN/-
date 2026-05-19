using System.Collections;
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
    [Tooltip("统一按键（F3）")]
    public KeyCode actionKey = KeyCode.F3;

    [Tooltip("长按退出时间（秒）")]
    public float longPressDuration = 2f;

    [Header("状态")]
    public GameState currentState;

    private float exitKeyHoldTime;
    private bool hasTriggeredLongPress;
    private bool pendingTransition;
    private Coroutine transitionCoroutine;

    public enum GameState
    {
        IntroVideo,
        TransitionVideo,
        MainGame,
        Restarting
    }

    private void Start()
    {
        EnterIntroVideoState();
    }

    private void Update()
    {
        HandleInput();
        UpdateExitKeyHold();
    }

    private void HandleInput()
    {
        switch (currentState)
        {
            case GameState.IntroVideo:
                HandleIntroVideoInput();
                break;
            case GameState.MainGame:
                HandleMainGameInput();
                break;
        }
    }

    private void HandleIntroVideoInput()
    {
        if (Input.GetKeyDown(actionKey))
        {
            RequestTransition();
        }
    }

    private void HandleMainGameInput()
    {
        if (Input.GetKeyDown(actionKey))
        {
            exitKeyHoldTime = 0f;
            hasTriggeredLongPress = false;
        }

        if (Input.GetKey(actionKey))
        {
            exitKeyHoldTime += Time.deltaTime;

            if (exitKeyHoldTime >= longPressDuration && !hasTriggeredLongPress)
            {
                hasTriggeredLongPress = true;
                ExitToIntro();
            }
        }

        if (Input.GetKeyUp(actionKey))
        {
            if (!hasTriggeredLongPress)
            {
                RestartGame();
            }
            exitKeyHoldTime = 0f;
        }
    }

    private void UpdateExitKeyHold()
    {
        if (currentState == GameState.MainGame && Input.GetKey(actionKey))
        {
            float progress = exitKeyHoldTime / longPressDuration;
            Debug.Log($"退出进度：{progress:P0}");
        }
    }

    private void RequestTransition()
    {
        if (pendingTransition) return;

        pendingTransition = true;
        Debug.Log("已请求进入过渡视频，等待当前轮次播放完毕");

        if (transitionVideoPlayer != null)
        {
            transitionVideoPlayer.Prepare();
            Debug.Log("已预加载过渡视频");
        }
    }

    private void EnterIntroVideoState()
    {
        currentState = GameState.IntroVideo;
        pendingTransition = false;
        Debug.Log("进入初始视频界面");

        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached += OnIntroVideoLoop;
            introVideoPlayer.Play();
        }
    }

    private void EnterTransitionVideoState()
    {
        currentState = GameState.TransitionVideo;
        Debug.Log("进入过渡视频");

        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoLoop;
            introVideoPlayer.Stop();
        }

        if (transitionVideoPlayer != null)
        {
            transitionVideoPlayer.loopPointReached += OnTransitionVideoEnd;

            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(PlayTransitionWhenReady());
        }
        else
        {
            Debug.LogWarning("没有设置过渡视频播放器，将直接进入游戏");
            if (autoEnterGameAfterVideo)
            {
                EnterMainGame();
            }
        }
    }

    private IEnumerator PlayTransitionWhenReady()
    {
        if (!transitionVideoPlayer.isPrepared)
        {
            Debug.Log("等待过渡视频预加载完成...");
            yield return new WaitUntil(() => transitionVideoPlayer.isPrepared);
        }

        transitionVideoPlayer.Play();
        Debug.Log("过渡视频开始播放");
        transitionCoroutine = null;
    }

    public void EnterMainGame()
    {
        if (currentState == GameState.MainGame)
            return;

        currentState = GameState.MainGame;
        Debug.Log("进入主游戏场景");

        StopAllVideos();

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == mainSceneName)
        {
            Debug.Log("已经在主游戏场景中，无需加载");
            return;
        }

        Debug.Log($"从场景 '{currentSceneName}' 加载到场景 '{mainSceneName}'");
        SceneManager.LoadScene(mainSceneName);
    }

    public void RestartGame()
    {
        if (currentState == GameState.Restarting)
            return;

        currentState = GameState.Restarting;
        Debug.Log("重启游戏");

        StopAllVideos();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ExitToIntro()
    {
        Debug.Log("退出到初始视频");

        StopAllVideos();
        exitKeyHoldTime = 0f;
        EnterIntroVideoState();
    }

    private void StopAllVideos()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

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

    private void OnIntroVideoLoop(VideoPlayer vp)
    {
        if (pendingTransition)
        {
            pendingTransition = false;
            Debug.Log("初始视频当前轮次播放完毕，进入过渡视频");
            EnterTransitionVideoState();
        }
    }

    private void OnTransitionVideoEnd(VideoPlayer vp)
    {
        if (autoEnterGameAfterVideo)
        {
            EnterMainGame();
        }
    }



//外部调用接口
    public void PublicEnterGame()
    {
        switch (currentState)
        {
            case GameState.IntroVideo:
                RequestTransition();
                break;
            case GameState.TransitionVideo:
                EnterMainGame();
                break;
        }
    }

    public void PublicRestart()
    {
        if (currentState == GameState.MainGame)
        {
            RestartGame();
        }
    }

    public void PublicExit()
    {
        ExitToIntro();
    }
}
