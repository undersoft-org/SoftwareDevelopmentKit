using System.Collections.ObjectModel;
using Undersoft.SDK.Series.Base;

namespace Undersoft.SDK.Estimating
{
    public class EstimatorSeries : RegistryBase<IEstimatorItem>
    {
        public EstimatorSeries() : base() { }

        public EstimatorSeries(IEnumerable<IEstimatorItem> range) : base(range) { }
    }    
}
