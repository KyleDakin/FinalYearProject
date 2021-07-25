using System;
using Manager_Scripts;
using SharpNeat.Phenomes;
using UnityEngine;
using UnityEngine.AI;

namespace Creature
{
    public class PredatorController : MonoBehaviour
    {
        //Private variables
        private Vision _vision;
        private NavMeshAgent _agent;
        private Genes _myGenes;
        private Vector3 _wanderTarget = Vector3.negativeInfinity;
        public CreatureStates _currentState;
        private GameObject food;
        

        private void Awake()
        {
            GetCreatureComponents();
            _myGenes = GeneController.Instance.GetGenes(Species.Predator);
            InitialiseCreature();
        }

        private void FixedUpdate()
        {
            _currentState = SetState();

            if (_currentState == CreatureStates.Sleeping)
            {
                _agent.speed = 0;
                _agent.destination = transform.position;
                food = null;
                _wanderTarget = Vector3.negativeInfinity;
                if (GameManager.Instance.timeOfDay == GameManager.TimeOfDay.Day)
                {
                    _currentState = CreatureStates.Wandering;
                }
            }

            if (_currentState == CreatureStates.Wandering)
            {
                Wander();
                _currentState = SetState();
            }

            if (_currentState == CreatureStates.MovingToFood)
            {
                MoveToFood();
            }
        }

        void GetCreatureComponents()
        {
            _agent = GetComponent<NavMeshAgent>();
            _vision = GetComponent<Vision>();
        }
        
        void InitialiseCreature()
        {
            _agent.speed = _myGenes.speed;
        }
        
        CreatureStates SetState()
        {
            if (GameManager.Instance.timeOfDay == GameManager.TimeOfDay.Night)
            {
                return CreatureStates.Sleeping;
            }

            food = SearchForFood();

            if (food != null){

                return CreatureStates.MovingToFood;
            }

            return CreatureStates.Wandering;
        }
        
        GameObject SearchForFood()
        {
            var target = _vision.SearchFor(Species.Prey);
            if (target == null) return null;
            return target.gameObject;
        }
        
        void Wander()
        {
            food = null;
            if (_wanderTarget.Equals(Vector3.negativeInfinity)) _wanderTarget = _vision.GenerateWanderPoint();
            if (_wanderTarget.Equals(transform.position)) _wanderTarget = _vision.GenerateWanderPoint();
            if (!_agent.destination.Equals(_wanderTarget)) _agent.destination = _wanderTarget;
            _agent.speed = _myGenes.speed;
            
            if (_agent.velocity.Equals(Vector3.zero))
            {
                _wanderTarget = _vision.GenerateWanderPoint();
                _agent.destination = _wanderTarget;
            }
            
            if(Vector3.Distance(transform.position, _wanderTarget) < 5f) _wanderTarget = Vector3.negativeInfinity;
        }
        
        void MoveToFood()
        {
            _wanderTarget = Vector3.negativeInfinity;
            if (!_agent.destination.Equals(food.transform.position)) _agent.destination = food.transform.position;
            _agent.speed = _myGenes.speed*2;

            if (Vector3.Distance(transform.position, food.transform.position) < 2.5f)
            {
                ConsumeFood();
                _currentState = CreatureStates.Wandering;
            }
        }

        void ConsumeFood()
        {
            food.GetComponent<CreatureController>().Die(CauseOfDeath.Eaten);
        }
    }
}
