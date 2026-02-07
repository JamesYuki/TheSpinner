using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Spinner
{
    public class TitleScene : BaseGameScene
    {
        [SerializeField] private SceneEnum m_SceneToLoad = SceneEnum.Lobby;

        private TitleUIModule m_UIModule;

        private const string k_AssetPath = "Prefabs/UI/Title/TitleView";

        private async void Start()
        {
            var ui = await ServiceLocator.Service<UI.IMenuUI>().LoadViewToForwardAsync(k_AssetPath);
            m_UIModule = ui.GetComponent<TitleUIModule>();
            if (m_UIModule != null)
            {
                m_UIModule.Initialize();
                m_UIModule.StartGameButtonEvent += OnStartButtonClicked;
            }
        }

        private void OnDestroy()
        {
            if (m_UIModule != null)
            {
                m_UIModule.Complete();
                m_UIModule.StartGameButtonEvent -= OnStartButtonClicked;
            }
        }

        private void OnStartButtonClicked()
        {
            Addressables.LoadSceneAsync(m_SceneToLoad.ToString());
        }
    }
}
