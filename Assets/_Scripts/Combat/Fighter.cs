using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BB.Core;
using BB.Resources;
using BB.Movement;

namespace BB.Combat
{
    
    [RequireComponent(typeof(Animator))]
    public class Fighter : MonoBehaviour, IAction
    {
        Health target;

        [SerializeField] float attackCoolDown = 1f;
        [SerializeField] float damage = 10f;
        [SerializeField] float attackRange = 1f;
        [Range(0, 1)] [SerializeField] float speedFraction = 0.6f;

        private int attackCashed = Animator.StringToHash("attack");
        private int stopAttackCashed = Animator.StringToHash("stopAttack");
        private float timeSinceLastAttack = Mathf.Infinity;
        private Animator animator;
        //Movement movement;

        void Start()
        {
            animator = GetComponent<Animator>();
            //movement = GetComponent<Movement>();
        }

        void Update()
        {
            timeSinceLastAttack += Time.deltaTime;
            /*
            if (target == null) return;
            if (target.IsDead()) return;

            if (!IsInRange())
                movement.MoveTo(target.transform.position, speedFraction);
            else
            {
                movement.Cancel();
                AttackBehaviour();
            }*/
        }

        private void AttackBehaviour()
        {
            transform.LookAt(target.transform);

            if (timeSinceLastAttack > attackCoolDown)
            {
                // This will trigger Hit()
                animator.ResetTrigger(stopAttackCashed);
                animator.SetTrigger(attackCashed);
                timeSinceLastAttack = 0;
            }
        }

        private bool IsInRange()
        {
            return Vector3.Distance(transform.position, target.transform.position) < attackRange;
        }

        public bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null) return false;

            Health testTarget = combatTarget.GetComponent<Health>();
            return testTarget != null && !testTarget.IsDead();
        }

        public void Attack(GameObject combatTarget)
        {
            GetComponent<ActionScheduler>().StartAction(this);
            target = combatTarget.GetComponent<Health>();
        }

        public void Cancel()
        {
            target = null;
            animator.ResetTrigger(attackCashed);
            animator.SetTrigger(stopAttackCashed);
        }

        void Hit()
        {
            if (target == null) return;

            //if (currentWeapon.HasProjectile())
            //    currentWeapon.LaunchProjectile(rightHandTransform, leftHandTransform, target, gameObject);
            //else
                target.TakeDamage(gameObject, damage);
        }
    }
}
