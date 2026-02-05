namespace JPS
{
    public interface ISceneLifecycle
    {
        void CreateScene();
        void Process();
        void DestroyScene();
    }
}
