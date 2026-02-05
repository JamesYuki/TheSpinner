
using PurrLobby;
using PurrNet;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Spinner
{
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private LobbyManager m_LobbyManager;
        [PurrScene, SerializeField] private string m_NextScene;

        private void Awake()
        {
            m_LobbyManager.OnAllReady.AddListener(SwitchScene);
        }

        private void OnDestroy()
        {
            if (m_LobbyManager != null)
            {
                m_LobbyManager.OnAllReady.RemoveListener(SwitchScene);
            }
        }

        public void SwitchScene()
        {
            m_LobbyManager.SetLobbyStarted();
            Addressables.LoadSceneAsync(m_NextScene, UnityEngine.SceneManagement.LoadSceneMode.Single)
                .Completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("シーンのロードに成功しました");
            }
            else
            {
                Debug.LogError("シーンのロードに失敗しました");
            }
        }
    }
}
