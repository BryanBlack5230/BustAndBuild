using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace GameEngine.AI
{
    /// <summary>
    /// Acessing ECS from MB
    /// </summary>
    public class UnitSelectionManager : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        private EntityManager _entityManager;

        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            return;
            if (_target == null) return;
            
            // if (!Input.GetMouseButtonDown(1)) return;
            // var mousePosition = CoreHelper.CursorPosECS();
                
            var entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover>().WithDisabled<Grabbed>().Build(_entityManager);
                
            // var entityArray = entityQuery.ToEntityArray(Allocator.Temp);
            var unitMoverArray = entityQuery.ToComponentDataArray<UnitMover>(Allocator.Temp);
            for (var i = 0; i < unitMoverArray.Length; i++)
            {
                var unitMover = unitMoverArray[i];
                unitMover.targetPosition = _target.position;
                unitMoverArray[i] = unitMover;
            }
            entityQuery.CopyFromComponentDataArray(unitMoverArray);
        }
    }
}