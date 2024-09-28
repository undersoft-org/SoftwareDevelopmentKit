using System.Diagnostics;
using System.Text;

namespace Undersoft.SDK.Estimating
{
    using System.Linq;
    using Undersoft.SDK.Series;

    public class AdaptiveResonainceTheoryEstimator
    {
        private int nextClusterId = 0;
        private int nextHyperClusterId = 0;

        public ISeries<string> NameList { get; set; }

        public int ItemSize { get; set; }

        public ISeries<IEstimatorItem> Items { get; set; }

        public ISeries<IEstimatorCluster> Clusters { get; set; }

        public ISeries<IEstimatorCluster<IEstimatorCluster>> HyperClusters { get; set; }

        private ISeries<IEstimatorCluster> ItemsToClusters;

        private ISeries<(
            IEstimatorCluster,
            IEstimatorCluster<IEstimatorCluster>
        )> ClustersToClusters;

        public double bValue = 0.2f;

        public double pValue = 0.6f;

        public double p2Value = 0.3f;

        public const int rangeLimit = 1;

        public int IterationLimit = 50;

        private string tempHardFileName = "surveyResults.art";

        public AdaptiveResonainceTheoryEstimator()
        {
            NameList = new Listing<string>();
            Items = new EstimatorSeries();
            Clusters = new Listing<IEstimatorCluster>();
            HyperClusters = new Listing<IEstimatorCluster<IEstimatorCluster>>();
            ItemsToClusters = new Listing<IEstimatorCluster>();
            ClustersToClusters =
                new Listing<(IEstimatorCluster, IEstimatorCluster<IEstimatorCluster>)>();
        }

        public int NextClusterId()
        {
            return Interlocked.Increment(ref nextClusterId);
        }

        public int NextHyperClusterId()
        {
            return Interlocked.Increment(ref nextHyperClusterId);
        }

        public void Create()
        {
            Clusters.Clear();
            HyperClusters.Clear();
            ItemsToClusters.Clear();
            ClustersToClusters.Clear();

            for (int i = 0; i < Items.Count; i++)
            {
                Assign(Items[i]);
            }

            Aggregate(HyperClusters.FirstOrDefault());
        }

        public void Create(IEnumerable<EstimatorItem> itemCollection)
        {
            Items.Add(itemCollection);

            Clusters.Clear();
            HyperClusters.Clear();
            ItemsToClusters.Clear();
            ClustersToClusters.Clear();

            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].Id = i;
                Assign(Items[i]);
            }

            Aggregate(HyperClusters.FirstOrDefault());
        }

        public void Append(ICollection<EstimatorItem> itemCollection)
        {
            int currentCount = Items.Count;

            Items.Add(itemCollection);

            for (int i = currentCount; i < Items.Count; i++)
            {
                Items[i].Id = i;
                Assign(Items[i]);
            }

            Aggregate(HyperClusters.FirstOrDefault());
        }

        public void Append(IEstimatorItem item)
        {
            item.Id = Items.Count;
            Items.Add(item);
            Assign(item);

            Aggregate(HyperClusters.FirstOrDefault());
        }

        public void Aggregate(IEstimatorCluster<IEstimatorCluster> hyperCluster)
        {
            var children = hyperCluster.HyperClusters;
            if (children != null)
            {
                if (HyperClusters.TryGet(children, out var childCluster))
                    Aggregate(childCluster.Value);

                foreach (var child in children)
                    child.MergeItems();
            }
        }

        public void Assign(IEstimatorItem item)
        {
            int iterationCounter = IterationLimit;
            bool isAssignementChanged = true;
            double itemVectorMagnitude = CalculateVectorMagnitude(item.Vector);

            while (isAssignementChanged && iterationCounter-- > 0)
            {
                isAssignementChanged = false;

                var clusterToProximityList = new List<(IEstimatorCluster, double)>();

                double proximityThreshold = itemVectorMagnitude / (bValue + rangeLimit * ItemSize);

                foreach (var cluster in Clusters)
                {
                    double clusterVectorMagnitude = CalculateVectorMagnitude(cluster.Vector);
                    double proximity =
                        CaulculateVectorIntersectionMagnitude(item.Vector, cluster.Vector)
                        / (bValue + clusterVectorMagnitude);

                    if (proximity > proximityThreshold)
                    {
                        clusterToProximityList.Add((cluster, proximity));
                    }
                }
                IEstimatorCluster newCluster = null;
                if (clusterToProximityList.Count > 0)
                {
                    clusterToProximityList.Sort((x, y) => -1 * x.Item2.CompareTo(y.Item2));

                    foreach (var clusterToProximity in clusterToProximityList)
                    {
                        newCluster = clusterToProximity.Item1;
                        double vigilance =
                            CaulculateVectorIntersectionMagnitude(newCluster.Vector, item.Vector)
                            / itemVectorMagnitude;
                        if (vigilance >= pValue)
                        {
                            if (ItemsToClusters.TryGet(item.Id, out var previousCluster))
                            {
                                if (ReferenceEquals(newCluster, previousCluster))
                                    break;
                                if (!previousCluster.RemoveFromCluster(item))
                                    Clusters.Remove(previousCluster);
                            }
                            newCluster.AddToCluster(item);
                            ItemsToClusters[item.Id] = newCluster;
                            isAssignementChanged = true;
                            break;
                        }
                    }
                }

                if (!ItemsToClusters.TryGet(item.Id, out newCluster))
                {
                    newCluster = new EstimatorCluster(item, NextClusterId());
                    Clusters.Add(newCluster);
                    ItemsToClusters.Add(item.Id, newCluster);
                    isAssignementChanged = true;
                }
            }

            Assign(Clusters);
        }

        public void Assign(ISeries<IEstimatorCluster> clusters)
        {
            if (!HyperClusters.TryGet(clusters, out var hyperClusterNode))
                hyperClusterNode.Value = HyperClusters
                    .Put(clusters, new EstimatorHyperCluster(clusters, NextHyperClusterId()))
                    .Value;

            var hyperClusters = hyperClusterNode.Value.HyperClusters;

            int iterationCounter = IterationLimit;
            bool isAssignementChanged = true;

            while (isAssignementChanged && iterationCounter-- > 0)
            {
                isAssignementChanged = false;
                foreach (var cluster in Clusters)
                {
                    var hyperClusterToProximityList =
                        new List<(IEstimatorCluster<IEstimatorCluster>, double)>();

                    double clusterVectorMagnitude = CalculateVectorMagnitude(cluster.Vector);
                    double proximityThreshold =
                        clusterVectorMagnitude / (bValue + rangeLimit * ItemSize);

                    foreach (var hyperCluster in hyperClusters)
                    {
                        double hyperClusterVectorMagnitude = CalculateVectorMagnitude(
                            hyperCluster.Vector
                        );
                        double proximity =
                            CaulculateVectorIntersectionMagnitude(
                                cluster.Vector,
                                hyperCluster.Vector
                            ) / (bValue + hyperClusterVectorMagnitude);
                        if (proximity > proximityThreshold)
                        {
                            hyperClusterToProximityList.Add((hyperCluster, proximity));
                        }
                    }

                    IEstimatorCluster<IEstimatorCluster> newHyperCluster = null;
                    if (hyperClusterToProximityList.Count > 0)
                    {
                        hyperClusterToProximityList.Sort((x, y) => -1 * x.Item2.CompareTo(y.Item2));

                        foreach (var HyperClusterToProximity in hyperClusterToProximityList)
                        {
                            newHyperCluster = HyperClusterToProximity.Item1;
                            double vigilance =
                                CaulculateVectorIntersectionMagnitude(
                                    newHyperCluster.Vector,
                                    cluster.Vector
                                ) / clusterVectorMagnitude;

                            if (vigilance >= p2Value)
                            {
                                if (
                                    ClustersToClusters.TryGet(
                                        cluster,
                                        out var previousHyperClusterPair
                                    )
                                )
                                {
                                    IEstimatorCluster previousHyperCluster =
                                        previousHyperClusterPair.Value.Item2;
                                    if (ReferenceEquals(newHyperCluster, previousHyperCluster))
                                        break;
                                    if (previousHyperCluster.RemoveFromCluster(cluster) == false)
                                    {
                                        HyperClusters.Remove(previousHyperCluster);
                                    }
                                }
                                newHyperCluster.AddToCluster(cluster);
                                ClustersToClusters[cluster] = (cluster, newHyperCluster);
                                isAssignementChanged = true;

                                break;
                            }
                        }
                    }

                    if (!ClustersToClusters.TryGet(cluster, out var newHyperClusterPair))
                    {
                        newHyperCluster = new EstimatorHyperCluster(cluster, nextHyperClusterId++);
                        HyperClusters.Add(newHyperCluster);
                        ClustersToClusters.Add(cluster, (cluster, newHyperCluster));
                        isAssignementChanged = true;
                    }
                }
            }
        }

        public IEstimatorItem SimilarTo(IEstimatorItem item)
        {
            StringBuilder outputText = new StringBuilder();
            double tempItemSimilarSum = 0;
            double itemSimilarSum = 0;
            IEstimatorItem itemSimilar = null;
            IEstimatorCluster cluster = null;

            if (ItemsToClusters.TryGet(item.Id, out cluster))
            {
                foreach (var clusterItem in cluster.Items.AsValues())
                {
                    if (!ReferenceEquals(item, clusterItem))
                    {
                        tempItemSimilarSum =
                            CaulculateVectorIntersectionMagnitude(item.Vector, clusterItem.Vector)
                            / CalculateVectorMagnitude(clusterItem.Vector);
                        if (itemSimilarSum < tempItemSimilarSum)
                        {
                            itemSimilarSum = tempItemSimilarSum;
                            itemSimilar = clusterItem;
                        }
                    }
                }

                if (itemSimilar != null)
                {
                    outputText.Append(
                        " Most similiar taste have item " + itemSimilar.Name + "\r\n\r\n"
                    );
                }
                else
                {
                    outputText.Append(" There is no similiar item " + item.Name + "\r\n\r\n");
                }
            }
            Debug.WriteLine(outputText.ToString());

            return itemSimilar;
        }

        public IEstimatorItem SimilarInGroupsTo(IEstimatorItem item)
        {
            StringBuilder outputText = new StringBuilder();
            double tempItemSimilarSum = 0;
            double itemSimilarSum = 0;
            IEstimatorItem itemSimilar = null;
            IEstimatorCluster cluster = null;

            if (ItemsToClusters.TryGet(item.Id, out cluster))
            {
                foreach (
                    var hyperClusterItem in ClustersToClusters[cluster]
                        .Item2.MergeItems()
                        .AsValues()
                )
                {
                    if (!ReferenceEquals(item, hyperClusterItem))
                    {
                        tempItemSimilarSum =
                            CaulculateVectorIntersectionMagnitude(
                                item.Vector,
                                hyperClusterItem.Vector
                            ) / CalculateVectorMagnitude(hyperClusterItem.Vector);
                        if (itemSimilarSum < tempItemSimilarSum)
                        {
                            itemSimilarSum = tempItemSimilarSum;
                            itemSimilar = hyperClusterItem;
                        }
                    }
                }

                if (itemSimilar != null)
                {
                    outputText.Append(
                        " Most similiar taste in hyper cluster have item "
                            + itemSimilar.Name
                            + "\r\n\r\n"
                    );
                }
                else
                {
                    outputText.Append(
                        " There is no simiilar item in hyper cluster " + item.Name + "\r\n\r\n"
                    );
                }
            }
            Debug.WriteLine(outputText.ToString());

            return itemSimilar;
        }

        public IEstimatorItem SimilarInOtherGroupsTo(IEstimatorItem item)
        {
            StringBuilder outputText = new StringBuilder();
            double tempItemSimilarSum = 0;
            double itemSimilarSum = 0;
            IEstimatorItem itemSimilar = null;

            if (ItemsToClusters.TryGet(item.Id, out IEstimatorCluster cluster))
            {
                foreach (var checkCluster in ClustersToClusters[cluster].Item2.Clusters.AsValues())
                {
                    if (!ReferenceEquals(cluster, checkCluster))
                    {
                        foreach (var clusterItem in checkCluster.Items.AsValues())
                        {
                            tempItemSimilarSum =
                                CaulculateVectorIntersectionMagnitude(
                                    item.Vector,
                                    clusterItem.Vector
                                ) / CalculateVectorMagnitude(clusterItem.Vector);
                            if (itemSimilarSum < tempItemSimilarSum)
                            {
                                itemSimilarSum = tempItemSimilarSum;
                                itemSimilar = clusterItem;
                            }
                        }
                    }
                }

                if (itemSimilar != null)
                {
                    outputText.Append(
                        " Most similiar taste in hyper cluster (other clusters) have item "
                            + itemSimilar.Name
                            + "\r\n\r\n"
                    );
                }
                else
                {
                    outputText.Append(
                        " There is no simiilar item in hyper cluster (other clusters) "
                            + item.Name
                            + "\r\n\r\n"
                    );
                }
            }
            Debug.WriteLine(outputText.ToString());

            return itemSimilar;
        }

        public static double[] CalculateIntersection<T>(ISeries<T> input, double[] output)
            where T : IEstimatorItem
        {
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = input[0].Vector[i];
                for (int j = 1; j < input.Count; j++)
                {
                    output[i] = Math.Min(output[i], input[j].Vector[i]);
                }
            }
            return output;
        }

        public static double[] CalculateSummary<T>(ISeries<T> input, double[] output)
            where T : IEstimatorItem
        {
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = 0;
                for (int j = 0; j < input.Count; j++)
                {
                    output[i] += input[j].Vector[i];
                }
            }

            return output;
        }

        public static double[] UpdateIntersectionByLast<T>(ISeries<T> input, double[] output)
            where T : IEstimatorItem
        {
            int n = input.Count - 1;
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = Math.Min(output[i], input[n].Vector[i]);
            }
            return output;
        }

        public static double[] UpdateSummaryByLast<T>(ISeries<T> input, double[] output)
            where T : IEstimatorItem
        {
            int n = input.Count - 1;
            for (int i = 0; i < output.Length; i++)
            {
                output[i] += input[n].Vector[i];
            }
            return output;
        }

        public static ISeries<IEstimatorItem> NormalizeItemList(
            ISeries<IEstimatorItem> featureItems
        )
        {
            EstimatorSeries normalizedItems = new EstimatorSeries();

            var average = featureItems.SelectMany(f => f.Vector).Average();
            var extremes = featureItems.SelectMany(f => f.Vector).Extremes();
            var bias = Math.Abs(extremes.Item1);
            var rangeValue = bias + extremes.Item2;
            var margin = (rangeValue / 2) - average;
            rangeValue += margin;
            bias += margin;

            foreach (var item in featureItems)
            {
                double[] featureVector = item
                    .Vector.ForEach((v, i) => (v + bias) / rangeValue)
                    .ToArray();

                normalizedItems.Add(
                    new EstimatorItem(item.Id, item.Name, featureVector, item.Target)
                );
            }
            return normalizedItems;
        }

        public static double CalculateVectorMagnitude(double[] vector)
        {
            double result = 0;
            for (int i = 0; i < vector.Length; ++i)
            {
                result += vector[i];
            }
            return result;
        }

        public static double CaulculateVectorIntersectionMagnitude(
            double[] vector1,
            double[] vector2
        )
        {
            double result = 0;

            for (int i = 0; i < vector1.Length; ++i)
            {
                result += Math.Min(vector1[i], vector2[i]);
            }

            return result;
        }

        public void LoadFile(string fileLocation)
        {
            string line;
            NameList.Clear();
            NameList.Add("Name");

            StreamReader file = new StreamReader(fileLocation);

            while ((line = file.ReadLine()) != null)
            {
                if (line == "Workers")
                {
                    break;
                }
            }

            if (line == null)
            {
                throw new Exception("ART File does not have a section marked Workers!");
            }
            else
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line == "--")
                    {
                        break;
                    }
                    else
                    {
                        NameList.Add(line);
                    }
                }
                ItemSize = NameList.Count - 1;

                int featureItemId = 0;
                while ((line = file.ReadLine()) != null)
                {
                    string featureName = line;
                    line = file.ReadLine();
                    double[] featureVector = new double[ItemSize];
                    int i = 0;
                    while ((line != null) && (line != "--"))
                    {
                        featureVector[i++] = Int32.Parse(line);
                        line = file.ReadLine();
                    }

                    if (line == "--")
                    {
                        if (i != ItemSize)
                        {
                            for (int j = i; j < ItemSize; ++j)
                            {
                                featureVector[j] = 0;
                            }
                        }
                        Items.Add(new EstimatorItem(featureItemId, featureName, featureVector));
                        featureItemId++;
                    }
                }
            }

            file.Close();
        }
    }
}
