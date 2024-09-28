using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using Undersoft.SDK.Series;

namespace Undersoft.SDK.Estimating
{
    public class EstimatorHyperCluster : EstimatorCluster<IEstimatorCluster>
    {
        public EstimatorHyperCluster(ISeries<IEstimatorCluster> clusters, int id) : base(id)
        {
            Clusters = clusters;
            IsNode = true;
            HyperClusters = new Listing<IEstimatorCluster<IEstimatorCluster>>();
        }

        public EstimatorHyperCluster(IEstimatorCluster cluster, int id) : base(id)
        {
            Vector = cluster.Vector[..cluster.Vector.Length];
            VectorSummary = cluster.VectorSummary[..cluster.VectorSummary.Length];
            Clusters = new Listing<IEstimatorCluster>([cluster]);
        }

        public override bool RemoveFromCluster(IEstimatorCluster cluster)
        {
            if (Clusters.Remove(cluster))
            {
                cluster.Id = 0;
                if (Clusters.Count > 0)
                {
                    AdaptiveResonainceTheoryEstimator.CalculateIntersection(Clusters, Vector);
                    AdaptiveResonainceTheoryEstimator.CalculateSummary(Clusters, VectorSummary);
                }
            }
            return Clusters.Count > 0;
        }

        public override void AddToCluster(IEstimatorCluster cluster)
        {
            cluster.ClusterId = (ushort)Id;
            Clusters.Add(cluster);
            AdaptiveResonainceTheoryEstimator.UpdateIntersectionByLast(Clusters, Vector);
            AdaptiveResonainceTheoryEstimator.UpdateSummaryByLast(Clusters, VectorSummary);
        }

        public override ISeries<IEstimatorItem> MergeItems()
        {
            return Items = new EstimatorSeries(Clusters.Concat(Items));
        }

    }

}
