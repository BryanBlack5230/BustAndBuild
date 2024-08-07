using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondOrderMotion : MonoBehaviour
{
	[Range(0.001f, 20)]
	public float F = 2;
	[Range(0, 1)]
	public float Z = 1;
	[Range(-1, 1)]
	public float R = 1;
	public Transform target;

	private float _f, _z, _r = 0;
	private Transform _transform;
	private SecondOrderDynamics[] _dymamics = new SecondOrderDynamics[3];

	// Start is called before the first frame update
	void Start()
	{
		_f = F;
		_z = Z;
		_r = R;
		_transform = GetComponent<Transform>();
		_dymamics[0] = new SecondOrderDynamics(_f, _z, _r, target.position.x);
		_dymamics[1] = new SecondOrderDynamics(_f, _z, _r, target.position.y);
		_dymamics[2] = new SecondOrderDynamics(_f, _z, _r, target.position.z);
	}

	// Update is called once per frame
	void Update()
	{
		if (_f != F || _z != Z || _r != R)
		{
			_f = F;
			_z = Z;
			_r = R;
			_dymamics[0] = new SecondOrderDynamics(_f, _z, _r, target.position.x);
			_dymamics[1] = new SecondOrderDynamics(_f, _z, _r, target.position.y);
			_dymamics[2] = new SecondOrderDynamics(_f, _z, _r, target.position.z);
		}
		float x = _dymamics[0].Update(Time.deltaTime, target.position.x);
		float y = _dymamics[1].Update(Time.deltaTime, target.position.y);
		float z = _dymamics[2].Update(Time.deltaTime, target.position.z);

		_transform.position = new Vector3(x, y, z);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(target.position, 0.5f);
	}


}

public class SecondOrderDynamics
{
	private float xp; // previous input
	private float y, yd; // state variables
	float _w, _z, _d, k1, k2, k3; // dynamic constraints

	public SecondOrderDynamics(float f, float z, float r, float x0)
	{
		//compute constants
		_w = 2 * Mathf.PI * f;
		_z = z;
		_d = _w * Mathf.Sqrt(Mathf.Abs(z * z - 1));
		k1 = z / (Mathf.PI * f);
		k2 = 1 / ((2*Mathf.PI * f) * (2 * Mathf.PI * f));
		k3 = r * z / (2 * Mathf.PI * f);

		//Initialize Variables
		xp = x0;
		y = x0;
		yd = 0;
	}

	public float Update(float T, float x)
	{
		//Estimate velocity
		float xd = (x - xp) / T;
		xp = x;

		float k1_stable, k2_stable;
		if (_w * T < _z) // clamp k2 to guarantee stability without jitter
		{
			k1_stable = k1;
			k2_stable = Mathf.Max(k2, T*T/2f + T*k1/2f, T*k1);
		}
		else // Use pole matching when the system is very fast
		{
			float t1 = Mathf.Exp(-_z * _w * T);
			float alpha = 2 * t1 * (_z <= 1 ? Mathf.Cos(T * _d) : System.MathF.Cosh(T * _d));
			float beta = t1 * t1;
			float t2 = T / (1 + beta - alpha);
			k1_stable = (1 - beta) * t2;
			k2_stable = T * t2;
		}

		y = y + T * yd; // integrate position by velocity
		yd = yd + T * (x + k3*xd - y - k1*yd)/k2_stable; // integrate velocity by acceleration
		return y;
	}
}

/*internal class SecondOrderSolvingStrategyEulerStableNoJitter
		: SecondOrderSolvingStrategyBase<SecondOrderSolvingStrategyEulerStableNoJitter>
	{
		/// <inheritdoc />
		public override SecondOrderSolvingStrategy StrategyType => SecondOrderSolvingStrategy.SemiImplicitEulerStableClampedK2NoJitter;

		/// <inheritdoc />
		public override void RecalculateConstants(ref SecondOrderState state, float initialValue)
			=> SecondOrderSolvingStrategyEuler.DefaultConstantCalculation(ref state, initialValue);

		/// <inheritdoc />
		protected override void OnNewValue(ref SecondOrderState state, float deltaTime, float targetValue, float targetVelocity)
		{
			// clamp k2 above the catastrophic error value.
			// not physically correct, but goal is to prevent failure, not produce accurate physics sim

			// clamp k2 to guarantee stability without jitter
			float k2Stable = Mathf.Max(state.K2,
				((deltaTime * deltaTime) / 2) + (deltaTime * state.K1 / 2),
				deltaTime * state.K1);

			Integrate(ref state, deltaTime, targetValue, targetVelocity, state.K1, k2Stable);
		}
	}

internal abstract class SecondOrderSolvingStrategyBase<TDerived>
		: ISecondOrderSolvingStrategyF where TDerived  : ISecondOrderSolvingStrategy<float>, new()
	{
		// a singleton instance; will be collected by the registry.
		// ReSharper disable once MemberCanBePrivate.Global -- reflection can't find private static inherited members.
		public static ISecondOrderSolvingStrategy Instance { get; } = new TDerived();

		static SecondOrderSolvingStrategyBase()
		{
			if (Instance.StrategyType == SecondOrderSolvingStrategy.Unknown)
			{
				throw new TypeLoadException(
					$"Instance of {Instance.GetType().FullName} created with unexpected strategy type: {Instance.StrategyType}");
			}
		}

		/// <inheritdoc />
		public virtual SecondOrderSolvingStrategy StrategyType => SecondOrderSolvingStrategy.Unknown;

		/// <inheritdoc />
		public abstract void RecalculateConstants(ref SecondOrderState state, float initialValue);

		/// <inheritdoc />
		public float UpdateStrategy(ref SecondOrderState state, float deltaTime, float targetValue, float? targetVelocity)
		{
			float targetVelocityActual = UpdateVelocity(ref state, deltaTime, targetValue, targetVelocity);
			OnNewValue(ref state, deltaTime, targetValue, targetVelocityActual);
			return state.CurrentValue;
		}


		/// <inheritdoc cref="UpdateStrategy"/>
		protected abstract void OnNewValue(ref SecondOrderState state, float deltaTime, float targetValue, float targetVelocity);

		/// <summary>
		/// A static helper to integrate a given <see cref="state"/> towards a new <see cref="targetValue"/>.
		/// The caller may optionally provide <see cref="k1Override"/> and <see cref="k2Override"/> to use
		/// instead of the pre-calculated constants in <see cref="state"/>. This allows the caller to
		/// clamp those values to safe ranges before integrating.
		/// </summary>
		/// <param name="state">The state to update.</param>
		/// <param name="deltaTime">The elapsed time since the previous update.</param>
		/// <param name="targetValue">The current target value.</param>
		/// <param name="targetVelocity">The velocity the target value is moving at.</param>
		/// <param name="k1Override">An optional override for the K1 constant.</param>
		/// <param name="k2Override">An optional override for the K2 constant.</param>
		protected static void Integrate(ref SecondOrderState state,
			float deltaTime,
			float targetValue,
			float targetVelocity,
			float? k1Override = null,
			float? k2Override = null
		)
		{
			float k1 = k1Override ?? state.K1;
			float k2 = k2Override ?? state.K2;

			// integrate position by velocity
			state.CurrentValue += (deltaTime * state.CurrentVelocity);

			// integrate velocity by acceleration
			state.CurrentVelocity += (deltaTime * (targetValue + (state.K3 * targetVelocity) - state.CurrentValue - (k1 * state.CurrentVelocity)) / k2);
		}

		private static float UpdateVelocity(ref SecondOrderState state,
			float deltaTime,
			float targetValue,
			float? targetVelocityOrNull
		)
		{
			float estimatedVelocity = (targetValue - state.PreviousTargetValue) / deltaTime;
			float xd = targetVelocityOrNull.GetValueOrDefault(estimatedVelocity);
			state.PreviousTargetValue = targetValue;
			return xd;
		}
	}
}

internal struct SecondOrderState
	{
		// user input
		public float F;
		public float Z;
		public float R;
		public float InitialValue;
		public float TargetValue;
		public float PreviousTargetValue;

		// system input
		public float DeltaTime;
		public float ElapsedTime;

		// system output
		public float CurrentValue; // at t0: initial value
		public float CurrentVelocity; // first derivative of iv
		public float LastStableValue;

		// function constants calculated from f, ζ, and r.
		public float K1;
		public float K2;
		public float K3;

		// calculated constants for pole zero matching.
		public float W;
		public float D;

		// the maximum time step for any one solve iteration
		public float MaximumTimeStep;

		// meta about the current state of the system as a whole.
		internal bool _isDeserializing;
	}


namespace cmdwtf.UnityTools.Dynamics
{
	/// <summary>
	/// A second-order dynamics simulation system.
	/// </summary>
	[Serializable]
	public class SecondOrderDynamics : ISimulatableDynamicsSystem, ISerializationCallbackReceiver
	{
		#region Private Constants

		private const float DefaultSaneF = 3f;
		private const float DefaultSaneZ = 1f;
		private const float DefaultSaneR = 0f;
		private const float DefaultSaneInitialValue = 0f;

		#endregion

		#region Serialized Fields

		[SerializeField]
		internal SecondOrderSettings settings;

		#endregion

		#region Private State

		private ISecondOrderSolvingStrategyF _solver;
		private SecondOrderState _state;

		#endregion

		#region Internal State Properties

		internal SecondOrderState State
		{
			get => _state;
			set => _state = value;
		}

		#endregion

		#region Public State Accessors

		/// <summary>
		/// The current frequency (f) value of the system.
		/// </summary>
		public float F => _state.F;

		/// <summary>
		/// The current damping coefficient (ζ) value of the system.
		/// </summary>
		public float Z => _state.Z;

		/// <summary>
		/// The current response (r) value of the system.
		/// </summary>
		public float R => _state.R;

		/// <summary>
		/// <see langword="true"/> if the system is currently stable.
		/// </summary>
		public bool IsStable => settings.GetStability(ref _state) == StabilityState.Stable;

		/// <summary>
		/// Gets a value representing the current stability of the system.
		/// </summary>
		public StabilityState StabilityState => settings.GetStability(ref _state);

		/// <summary>
		/// The last value the system produced.
		/// </summary>
		public float Value => _state.CurrentValue;

		/// <summary>
		/// The last stable value that the system produced.
		/// </summary>
		public float LastStableValue => _state.LastStableValue;

		#endregion

		#region Static Instance Properties

		/// <summary>
		/// A default second order dynamics system that has some sane default values to produce eased movement.
		/// </summary>
		public static SecondOrderDynamics Default => new();

		#endregion

		/// <summary>
		/// Creates a new <see cref="SecondOrderDynamics"/> with default input values.
		/// </summary>
		public SecondOrderDynamics() : this(DefaultSaneF, DefaultSaneZ, DefaultSaneR)
		{ }

		/// <summary>
		/// Creates a new <see cref="SecondOrderDynamics"/> with specified input values.
		/// </summary>
		/// <param name="f">The frequency of the system.</param>
		/// <param name="z">The dampening coefficient of the system.</param>
		/// <param name="r">The response of the system.</param>
		/// <param name="initialValue">The value the system should start from when reset.</param>
		/// <param name="strategy">The solving strategy to use for the system.</param>
		public SecondOrderDynamics(float f, float z, float r, float initialValue = DefaultSaneInitialValue, SecondOrderSolvingStrategy strategy = SecondOrderSolvingStrategy.PoleZeroMatching)
		{
			// setup initial state
			_state._isDeserializing = true;
			_state.InitialValue = initialValue;

			// store settings
			settings = new SecondOrderSettings(f, z, r, strategy);

			// calculate constants
			RecalculateConstants();

			// all finished, we are done deserializing
			_state._isDeserializing = false;
		}

		/// <summary>
		/// Resets the system to the original initial value.
		/// </summary>
		public void Reset() => ResetTemporaryIv(_state.InitialValue);

		/// <summary>
		/// Resets the system to the given initial value, and optionally
		/// the initial velocity.
		/// </summary>
		/// <param name="newIv"></param>
		/// <param name="newVelocity"></param>
		public void Reset(float newIv, float newVelocity = default)
		{
			_state.InitialValue = newIv;
			ResetTemporaryIv(_state.InitialValue, newVelocity);
		}

		/// <summary>
		/// Updates the system with the new target value and timestep.
		/// </summary>
		/// <param name="deltaTime">The amount of time elapsed since the last update.</param>
		/// <param name="targetValue">The value the system should be moving towards.</param>
		/// <param name="xdOrNull">The velocity of the target, or null if the system should estimate it.</param>
		/// <returns>The updated value from the system.</returns>
		public float Update(float deltaTime, float targetValue, float? xdOrNull = null)
		{
			if (settings.unstableAutoReset && !IsStable)
			{
				Reset();
			}

			deltaTime *= settings.SampleDeltaTimeScale(in _state);

			// update timing
			_state.DeltaTime = deltaTime;
			_state.ElapsedTime += deltaTime;

			// solve for our new step. this will update the state.
			_solver?.UpdateStrategy(ref _state, deltaTime, targetValue, xdOrNull);

			// if our system has failed, handle the failure by the desired failure mode.
			if (!IsStable)
			{
				return settings.unstableHandlingMode switch
				{
					UnstableHandlingMode.ReturnTarget => targetValue,
					UnstableHandlingMode.Return0 => 0,
					UnstableHandlingMode.ReturnLastStableValue => _state.LastStableValue,
					UnstableHandlingMode.AllowDenormalizedValues => _state.CurrentValue,
					_ => throw new NotImplementedException(),
				};
			}

			// system is still stable, record this current value as last stable.
			_state.LastStableValue = _state.CurrentValue;
			return _state.CurrentValue;
		}

		private void ValidateUserInputs()
		{
			// validate to the ranges ensure they're good
			settings.ValidateRangeLimits();
		}

		private void ResampleUserInputs()
		{
			_state.F = settings.SampleF(in _state);
			_state.Z = settings.SampleZ(in _state);
			_state.R = settings.SampleR(in _state);
		}

		private void RecalculateConstants()
		{
			ValidateSolver();
			ValidateUserInputs();
			ResampleUserInputs();
			settings.ValidateRangeLimits();
			_solver?.RecalculateConstants(ref _state, _state.InitialValue);
		}

		internal void ResetTemporaryIv(float iv, float newYd = default)
		{
			_state.PreviousTargetValue = iv;
			_state.CurrentValue = iv;
			_state.LastStableValue = iv;
			_state.CurrentVelocity = newYd;
			_state.DeltaTime = 0;
			_state.ElapsedTime = 0;
			ResampleUserInputs();
		}

		private bool ValidateSolver()
		{
			if (_solver is not null && _solver.StrategyType == settings.solvingStrategy)
			{
				return true;
			}

			_solver = SecondOrderStrategyRegistry.Get(settings.solvingStrategy);

			if (_solver != null)
			{
				return true;
			}

			Debug.LogError($"Unable to satisfy solver for strategy: {settings.solvingStrategy}");
			return false;
		}

		#region Overrides of Object

		/// <inheritdoc />
		public override string ToString()
			=> $"f={F:0.###}, " +
			   $"ζ={Z:0.###}, " +
			   $"r={R:0.###}";

		#endregion

		#region ISerializationCallbackReceiver Implementation

		void ISerializationCallbackReceiver.OnBeforeSerialize() { }

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			_state._isDeserializing = true;
			RecalculateConstants();
			_state._isDeserializing = false;
		}

		#endregion
	}
}*/
