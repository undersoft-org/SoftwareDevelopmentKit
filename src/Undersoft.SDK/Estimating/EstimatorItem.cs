using System.Collections;

namespace Undersoft.SDK.Estimating
{
    using Uniques;

    public class EstimatorItem<T> : EstimatorItem
        where T : class, IIdentifiable
    {
        public new T Target
        {
            get => (T)base.Target;
            set => base.Target = value;
        }

        public EstimatorItem(long id, string name, object vector, T target = null)
            : base(id, name, vector, target) { }

        public EstimatorItem(EstimatorItem item)
            : base(item) { }
    }

    public class EstimatorItem : Origin, IEstimatorItem
    {
        public string Name
        {
            get => base.Label;
            set => base.Label = value;
        }
        public double[] Vector { get; set; }

        public EstimatorObjectMode Mode { get; set; }

        public virtual object Target { get; set; }

        public EstimatorItem()
            : base() { }

        public EstimatorItem(long id, string name, object vector, object target = null)
        {
            Id = id;
            Name = name;
            Target = target;
            SetVector(vector);
        }

        public EstimatorItem(EstimatorItem item)
        {
            Target = item.Target;
            Vector = item.Vector;
            Mode = item.Mode;
            Name = item.Name;
            Id = item.Id;
        }

        public EstimatorItem(object vector)
            : this()
        {
            SetVector(vector);
        }

        public virtual void SetVector(object vector)
        {
            var type = vector.GetType();
            if (type.IsValueType)
            {
                Vector = new double[] { Convert.ToDouble(vector) };
                Mode = EstimatorObjectMode.Single;
            }
            else if (type.IsArray)
            {
                Vector = ((Array)vector).Cast<object>().Select(o => Convert.ToDouble(o)).ToArray();
                Mode = EstimatorObjectMode.Multi;
            }
            else if (type.IsAssignableTo(typeof(IList)))
            {
                var vectorList = (IList)vector;
                if (vectorList.Count > 0 && vectorList[0] is ValueType)
                {
                    Vector = vectorList.Cast<object>().Select(o => Convert.ToDouble(o)).ToArray();
                    Mode = EstimatorObjectMode.Multi;
                }
                else
                {
                    throw new Exception("Wrong data type");
                }
            }
            else
            {
                throw new Exception("Wrong data type");
            }
        }
    }

    public enum EstimatorObjectMode
    {
        Multi,
        Single,
        Cluster,
    }
}
