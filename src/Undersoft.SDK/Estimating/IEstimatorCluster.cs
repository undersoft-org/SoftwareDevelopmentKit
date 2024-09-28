using Undersoft.SDK.Series;

namespace Undersoft.SDK.Estimating
{
    public interface IEstimatorCluster<T> : IEstimatorCluster
    {
        ISeries<IEstimatorCluster<T>> HyperClusters { get; set; }

        void AddToCluster(T item);
        bool RemoveFromCluster(T item);
    }

    public interface IEstimatorCluster : IEstimatorItem
    {
        ISeries<IEstimatorCluster> Clusters { get; set; }
        ISeries<IEstimatorItem> Items { get; set; }
        double[] VectorSummary { get; set; }
        bool IsNode { get; set; }

        void AddToCluster(IEstimatorItem item);
        ISeries<IEstimatorItem> MergeItems();
        bool RemoveFromCluster(IEstimatorItem item);
    }
}