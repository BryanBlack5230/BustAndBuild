using Unity.Entities;

namespace Game.Configs
{
    public class ConfigBridge
    {
        private readonly ConfigContainer _container;
        public ConfigBridge(ConfigContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
        
            var configEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(configEntity, new FindTargetConfigReference
            {
                // ConfigBlob = new BlobAssetReference<FindTargetConfigBlob>()
                ConfigBlob = _container.FindTargetConfigBlob
            });
        }
    }
}