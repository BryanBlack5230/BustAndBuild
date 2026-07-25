#nullable enable

using Reflex.Attributes;
using TMPro;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

using BarkingBird.Runtime.Gameplay.Scenes;
using BarkingBird.Runtime.Infrastructure.GameLoop;
using BarkingBird.Runtime.Infrastructure.Utilities;

namespace BarkingBird.Runtime.Gameplay.Beacon
{
    /// <summary>
    /// World-space hint that follows the Beacon Core and shows the current insert cost plus the live
    /// "Free in M:SS" countdown (ADR-0006). Read-only view: it pulls cost/timer from
    /// <see cref="BeaconCoreController"/> and the Core's world position + grab state from ECS each frame,
    /// and never drives the loop. Proximity-gated — shown only while the Core is being carried or rests
    /// near the socket, and hidden while the Core is inserted (its entity is disabled, so it drops out of
    /// the no-disabled query below).
    ///
    /// Author this on a hand-placed object that lives in the Battle scene (3.BattleGroundScene) so Reflex's
    /// SceneScope injects it from the Battle container where <see cref="BeaconCoreController"/> is bound.
    /// Instantiating it at runtime would skip that injection.
    /// </summary>
    public sealed class BeaconCoreHintView : MonoBehaviour, IWorldInitializable
    {
        [Header("References")]
        [SerializeField] private TMP_Text? _costLabel;
        [SerializeField] private TMP_Text? _timerLabel;
        [Tooltip("Visual group toggled by the proximity gate. If unset, the labels are toggled directly.")]
        [SerializeField] private GameObject? _content;

        [Header("Follow")]
        [SerializeField] private Vector3 _worldOffset = new(0f, 2f, 0f);
        [SerializeField] private bool _billboardToCamera = true;

        [Header("Gating")]
        [Tooltip("Also show while the Core rests within this distance of the socket. 0 = only while carried.")]
        [SerializeField] private float _nearSocketRadius = 4f;

        [Header("Copy")]
        [Tooltip("{0} = cost in Pearls. Shown while a cost is owed.")]
        [SerializeField] private string _costFormat = "{0}";
        [SerializeField] private string _countdownPrefix = "Free in ";
        [SerializeField] private string _freeText = "Free";

        private BeaconCoreController? _controller;
        private BattleSceneData? _battleSceneData;

        private EntityManager _entityManager;
        private EntityQuery _coreQuery;
        private bool _initialized;

        private int _lastCost = int.MinValue;
        private int _lastWholeSecondsLeft = int.MinValue;

        [Inject]
        private void Construct(BeaconCoreController controller, BattleSceneData battleSceneData)
        {
            _controller = controller;
            _battleSceneData = battleSceneData;
        }

        public void Initialize(EntityManager em)
        {
            _entityManager = em;
            _coreQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<BeaconCore>());
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (_controller is null || !_initialized)
            {
                SetVisible(false);
                return;
            }

            if (!_coreQuery.TryGetSingletonEntity<BeaconCore>(out var core))
            {
                SetVisible(false); // Core inserted (Disabled) or not yet spawned
                return;
            }

            Vector3 corePos = _entityManager.GetComponentData<LocalTransform>(core).Position;
            var visible = _entityManager.IsComponentEnabled<Grabbed>(core) || NearSocket(corePos);
            SetVisible(visible);
            if (!visible) return;

            FollowAndBillboard(corePos);
            Refresh();
        }

        private bool NearSocket(Vector3 corePos)
        {
            if (_nearSocketRadius <= 0f || _battleSceneData is null) return false;

            var socket = _battleSceneData.beaconCoreSocket;
            if (!socket) return false;

            return (corePos - socket.position).sqrMagnitude <= _nearSocketRadius * _nearSocketRadius;
        }

        private void FollowAndBillboard(Vector3 corePos)
        {
            var t = transform;
            t.position = corePos + _worldOffset;

            if (!_billboardToCamera) return;

            var cam = CoreHelper.MainCamera;
            if (cam) t.rotation = cam.transform.rotation;
        }

        private void Refresh()
        {
            var cost = _controller!.CurrentCost;
            if (cost != _lastCost)
            {
                if (_costLabel) _costLabel!.text = cost > 0 ? string.Format(_costFormat, cost) : _freeText;
                _lastCost = cost;
            }

            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (!_timerLabel) return;

            if (_controller!.IsFree)
            {
                if (_lastWholeSecondsLeft == 0) return;
                _timerLabel!.text = string.Empty;
                _lastWholeSecondsLeft = 0;
                return;
            }

            var seconds = Mathf.CeilToInt(_controller.FreeCountdownRemaining);
            if (seconds == _lastWholeSecondsLeft) return;

            _timerLabel!.text = _countdownPrefix +
                _controller.FreeCountdownRemaining.FormatDuration(TimeFormat.Digital, rounding: TimeRounding.Ceil);
            _lastWholeSecondsLeft = seconds;
        }

        private void SetVisible(bool visible)
        {
            if (_content)
            {
                if (_content!.activeSelf != visible) _content.SetActive(visible);
                return;
            }

            if (_costLabel) _costLabel!.enabled = visible;
            if (_timerLabel) _timerLabel!.enabled = visible;
        }

        private void OnDestroy()
        {
            if (!_initialized) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated && _coreQuery != default)
                _coreQuery.Dispose();
        }
    }
}
