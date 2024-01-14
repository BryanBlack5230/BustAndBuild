using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BB.Resources;
using BB.Core;

namespace BB.Movement
{
    public class Movement : MonoBehaviour, IAction
    {
        [SerializeField] float maxSpeed = 6f;
        Health health;
        bool canMove = true;

        public float MaxSpeed {get {return maxSpeed;}}

        private void Start()
        {
            health = GetComponent<Health>();
        }

        void Update()
        {
            //UpdateAnimator();
        }

        public void StartMoveAction(Vector3 destination, float speedFraction)
        {
            canMove = true;
            GetComponent<ActionScheduler>().StartAction(this);
            MoveTo(destination, speedFraction);
        }

        public void MoveTo(Vector3 target, float speedFraction)
        {
            if (!canMove) return;

            var step = Mathf.Clamp01(speedFraction) * maxSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);
        }

        private void UpdateAnimator()
        {
            // Vector3 velocity = navMeshAgent.velocity;
            // Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            // float speed = localVelocity.z;

            // GetComponent<Animator>().SetFloat(cashedSpeed, speed);
        }

        public void Cancel()
        {
            canMove = false;
        }
    }
}
