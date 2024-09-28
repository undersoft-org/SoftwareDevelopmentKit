using System.Collections;
using Undersoft.SDK.Series;

namespace Undersoft.SDK.Estimating
{
    public class EstimatorCluster<T> : EstimatorCluster, IEstimatorCluster<T> where T : IEstimatorItem
    {
        public ISeries<IEstimatorCluster<T>> HyperClusters { get; set; }          

        public EstimatorCluster(int id) :base(id) { }

        public EstimatorCluster(T item, int id) : this(id)
        {
            item.ClusterId = id;
            Vector = item.Vector[..item.Vector.Length];
            VectorSummary = item.Vector[..item.Vector.Length];
            Items = new EstimatorSeries([item]);
        }

        public virtual bool RemoveFromCluster(T item)
        {
            if (Items.TryRemove(item))
            {
                item.ClusterId = 0;
                if (Items.Count > 0)
                {
                    AdaptiveResonainceTheoryEstimator.CalculateIntersection(Items, Vector);
                    AdaptiveResonainceTheoryEstimator.CalculateSummary(Items, VectorSummary);

                }
            }
            return Items.Count > 0;
        }

        public virtual void AddToCluster(T item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                item.ClusterId = (ushort)Id;
                AdaptiveResonainceTheoryEstimator.UpdateIntersectionByLast(Items, Vector);
                AdaptiveResonainceTheoryEstimator.UpdateSummaryByLast(Items, VectorSummary);
            }
        }
    }

    public class EstimatorCluster : EstimatorItem, IEstimatorCluster
    {
        public virtual ISeries<IEstimatorCluster> Clusters { get; set; }

        public ISeries<IEstimatorItem> Items { get; set; }

        public double[] VectorSummary { get; set; }

        public bool IsNode { get; set; }

        public EstimatorCluster(int id) { Id = id; }

        public EstimatorCluster(IEstimatorItem item, int id) : this(id)
        {
            item.ClusterId = id;
            Vector = item.Vector[..item.Vector.Length];
            VectorSummary = item.Vector[..item.Vector.Length];
            Items = new EstimatorSeries([item]);
        }

        public virtual bool RemoveFromCluster(IEstimatorItem item)
        {
            if (Items.TryRemove(item))
            {
                item.ClusterId = 0;
                if (Items.Count > 0)
                {
                    AdaptiveResonainceTheoryEstimator.CalculateIntersection(Items, Vector);
                    AdaptiveResonainceTheoryEstimator.CalculateSummary(Items, VectorSummary);

                }
            }
            return Items.Count > 0;
        }

        public virtual void AddToCluster(IEstimatorItem item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                item.ClusterId = (ushort)Id;
                AdaptiveResonainceTheoryEstimator.UpdateIntersectionByLast(Items, Vector);
                AdaptiveResonainceTheoryEstimator.UpdateSummaryByLast(Items, VectorSummary);
            }
        }

        public virtual ISeries<IEstimatorItem> MergeItems()
        {
            return Items;
        }
    }

}
