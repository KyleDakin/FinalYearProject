using Manager_Scripts;
using SharpNeat.Phenomes;
using UnityEngine;
using UnityEngine.AI;

namespace Creature
{
    public class CreatureController : UnitController
    {
        //Public variables
        public float energy = 100000;
        public int age;
        
        //Private variables
        private Vision _vision;
        private NavMeshAgent _agent;
        private IBlackBox _box;
        private Genes _myGenes;
        private float _fleeSpeed;
        private bool _isRunning;
        private float _rateOfActivity = 0;
        private GameObject _predator;
        private Vector3 _wanderTarget = Vector3.negativeInfinity;
        public CreatureStates _currentState;
        private GameObject predator;
        private GameObject food;
        private int foodEaten;

        private void Awake()
        {
            foodEaten = 0;
            GetCreatureComponents();
            GeneController.Instance.CalculateMetabolicRate();
            _myGenes = GeneController.Instance.GetGenes(Species.Prey);
            InitialiseCreature();
            transform.localScale = new Vector3(_myGenes.height, _myGenes.height, _myGenes.height);
            _currentState = SetState();
        }
        
        void FixedUpdate()
        {
            if (_isRunning)
            {
                ISignalArray inputArray = _box.InputSignalArray;
                inputArray[0] = _myGenes.fleeWeight;
                inputArray[1] = _myGenes.hungerWeight;
                inputArray[2] = _myGenes.metabolicRate;
                inputArray[3] = energy;
                
                _box.Activate();

                ISignalArray outputArray = _box.OutputSignalArray;
                _myGenes.height = (float)outputArray[0];
                _myGenes.mass = (float) outputArray[1];
                _myGenes.speed = (float) outputArray[2];
                _myGenes.visionRange = (float) outputArray[3];
                _vision.visionRange = _myGenes.visionRange;
                
                if (energy <= 0)
                {
                    Die(CauseOfDeath.Starved);
                }
                
                predator = SearchForPredators();
                food = SearchForFood();

                _rateOfActivity = (_agent.speed > .1f) ? _agent.speed : .1f;
                energy -= Time.deltaTime * _myGenes.metabolicRate * _rateOfActivity;
                
                if (_currentState == CreatureStates.Sleeping)
                {
                    _agent.speed = 0;
                    _agent.destination = transform.position;
                    predator = null;
                    food = null;
                    _wanderTarget = Vector3.negativeInfinity;
                    if (GameManager.Instance.timeOfDay == GameManager.TimeOfDay.Day)
                    {
                        age++;
                        _currentState = CreatureStates.Wandering;
                    }
                }

                if (_currentState == CreatureStates.Wandering)
                {
                    Wander();
                    _currentState = SetState();
                }

                if (_currentState == CreatureStates.Fleeing)
                {
                    Flee();
                }

                if (_currentState == CreatureStates.MovingToFood)
                {
                    MoveToFood();
                }
            }
        }

        CreatureStates SetState()
        {
            if (GameManager.Instance.timeOfDay == GameManager.TimeOfDay.Night)
            {
                return CreatureStates.Sleeping;
            }

            if (food != null && predator != null)
            {
                if (_myGenes.fleeWeight >= _myGenes.hungerWeight)
                {
                    return CreatureStates.Fleeing;
                }

                return CreatureStates.MovingToFood;
            }
            if (food != null && predator == null)
            {
                return CreatureStates.MovingToFood;
            }

            if (food == null && predator != null)
            {
                return CreatureStates.Fleeing;
            }
            
            return CreatureStates.Wandering;
        }
        
        void GetCreatureComponents()
        {
            _agent = GetComponent<NavMeshAgent>();
            _vision = GetComponent<Vision>();
        }
        
        void InitialiseCreature()
        {
            _agent.speed = _myGenes.speed;
            _fleeSpeed = _myGenes.speed * 2;
            energy = 100;
        }
        
        public void Die(CauseOfDeath cause)
        {
            GeneController.Instance.Mutate(cause);
            gameObject.SetActive(false);
        }
        
        //UnityNeat Methods
        public override void Activate(IBlackBox box)
        {
            _box = box;
            _isRunning = true;
        }

        public override void Stop()
        {
            _isRunning = false;
        }

        public override float GetFitness()
        {
            float livingWeight = (gameObject.activeSelf) ? 1 : .5f;
            
            return (age + energy/5) > 0 ? (age + foodEaten + energy/10) * livingWeight : 0;
        }
        
        //StateMachine Methods
        GameObject SearchForPredators()
        {
            var target = _vision.SearchFor(Species.Predator);
            if (target == null) return null;
            return target.gameObject;
        }

        GameObject SearchForFood()
        {
            var target = _vision.SearchFor(Species.Plant);
            if (target == null) return null;
            return target.gameObject;
        }

        void Wander()
        {
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

        void Flee()
        {
            _wanderTarget = Vector3.negativeInfinity;

            if (food != null && _myGenes.hungerWeight > _myGenes.fleeWeight)
            {
                _currentState = CreatureStates.MovingToFood;
                return;
            }
            
            if (!predator)
            {
                _currentState = CreatureStates.Wandering;
                return;
            }

            Vector3 fleePoint = _vision.GenerateFleePoint(predator.transform.position);
            _agent.destination = fleePoint;
            _agent.speed = _fleeSpeed;

            if (Vector3.Distance(transform.position, fleePoint) < 5)
            {
                _currentState = CreatureStates.Wandering;
            }
        }

        void MoveToFood()
        {
            _wanderTarget = Vector3.negativeInfinity;
            
            if (predator != null && _myGenes.hungerWeight < _myGenes.fleeWeight)
            {
                _currentState = CreatureStates.Fleeing;
                return;
            }
            
            if (!food)
            {
                _currentState = CreatureStates.Wandering;
                return;
            }
            
            if (!_vision.FoodStillThere(food.transform))
            {
                _currentState = CreatureStates.Wandering;
                food = null;
                return;
            }
            if(!_agent.destination.Equals(food.transform.position)) _agent.destination = food.transform.position;
            _agent.speed = _myGenes.speed;

            if (Vector3.Distance(transform.position, food.transform.position) < 5f)
            {
                ConsumeFood();
            }
        }

        void ConsumeFood()
        {
            foodEaten++;
            energy += food.GetComponent<Plant>().GetEnergyReturn();
            Destroy(food.gameObject);
            _currentState = CreatureStates.Wandering;
        }
    }
}
