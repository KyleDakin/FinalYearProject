using UnityEngine;
using Random = System.Random;

namespace Creature
{
    public class GeneController : MonoBehaviour
    {
        public static GeneController Instance { get; private set; }
        
        [SerializeField]private Genes predatorGenePool;
        [SerializeField]private Genes preyGenePool;

        private Random _random;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            
            _random = new Random();
            
            predatorGenePool = new Genes();
            preyGenePool = new Genes();
        }

        public Genes GetGenes(Species species)
        {
            if (species == Species.Prey) return preyGenePool;
            return predatorGenePool;
        }

        public void CalculateMetabolicRate()
        {
            preyGenePool.metabolicRate = Mathf.Pow(preyGenePool.mass, .75f);
        }

        public void Mutate(CauseOfDeath cause)
        {
            if (cause == CauseOfDeath.Eaten)
            {
                preyGenePool.fleeWeight = ( _random.Next(0,100) < 50) ?  _random.Next((int)preyGenePool.fleeWeight, 100) : preyGenePool.fleeWeight;
                preyGenePool.hungerWeight = ( _random.Next(0,100) < 50) ?  _random.Next(0, (int)preyGenePool.hungerWeight) : preyGenePool.hungerWeight;
            }

            if (cause == CauseOfDeath.Starved)
            {
                preyGenePool.hungerWeight = ( _random.Next(0,100) < 50) ?  _random.Next((int)preyGenePool.hungerWeight, 100) : preyGenePool.hungerWeight;
                preyGenePool.fleeWeight = ( _random.Next(0,100) < 50) ?  _random.Next(0, (int)preyGenePool.fleeWeight) : preyGenePool.fleeWeight;
            }

            if (cause == CauseOfDeath.NoHeight)
            {
                preyGenePool.height = 5;
            }

            if (cause == CauseOfDeath.NoMass)
            {
                preyGenePool.mass = 1000;
            }
            CalculateMetabolicRate();
        }
        
    }

    [System.Serializable]
    public class Genes
    {
        public float speed;
        public float height;
        public float mass;
        public float fleeWeight;
        public float hungerWeight;
        public float metabolicRate;
        public float visionRange;
        
        public Genes()
        {
            speed = 1;
            height = 1;
            mass = 1;
            fleeWeight = 50;
            hungerWeight = 50;
            visionRange = 50;
        }
    }

    public enum CauseOfDeath
    {
        Eaten,
        Starved,
        NoHeight,
        NoMass
    }
}