namespace Spinner
{
    public interface ISceneLifecycle
    {
        void CreateScene();
        void Process();
        void DestroyScene();
    }
}
