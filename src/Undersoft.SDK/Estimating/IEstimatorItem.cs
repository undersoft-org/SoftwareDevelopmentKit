namespace Undersoft.SDK.Estimating
{
    public interface IEstimatorItem : IOrigin
    {
        EstimatorObjectMode Mode { get; set; }
        string Name { get; set; }
        double[] Vector { get; set; }
        object Target { get; set; }

        void SetVector(object vector);
    }
}