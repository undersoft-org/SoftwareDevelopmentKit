using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Undersoft.SDK.Instant.Attributes;
using Undersoft.SDK.Logging;
using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Uniques;

namespace Undersoft.SDK
{
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public class Origin : Identifiable, IOrigin, IUnique
    {
        public Origin() : base(true)
        {
        }

        public Origin(bool autoId) : base(autoId) { }       

        [IdentityRubric(Order = 3)]
        [DataMember(Order = 3)]
        [Column(Order = 3)]
        public virtual long OriginId
        {
            get
            {
                return GetOriginId();
            }
            set
            {
                SetOriginId(value);
            }
        }

        [NotMapped]
        [DataMember(Order = 4)]
        public virtual int ServiceId
        {
            get
            {
                return GetServiceId();
            }
            set
            {
                SetServiceId(value);
            }
        }

        [NotMapped]
        [DataMember(Order = 5)]
        public virtual int CategoryId
        {
            get
            {
                return GetCategoryId();
            }
            set
            {
                SetCategoryId(value);
            }
        }

        [NotMapped]
        [DataMember(Order = 6)]
        public virtual int ClusterId
        {
            get
            {
                return GetClusterId();
            }
            set
            {
                SetClusterId(value);
            }
        }

        [Column(TypeName = "timestamp", Order = 5)]
        [DataMember(Order = 8)]
        [InstantAs(UnmanagedType.I8, SizeConst = 8)]
        public virtual DateTime Modified { get; set; }

        [StringLength(128)]
        [Column(Order = 6)]
        [DataMember(Order = 9)]
        [InstantAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public virtual string Modifier { get; set; } = "";

        [Column(TypeName = "timestamp", Order = 7)]
        [DataMember(Order = 10)]
        [InstantAs(UnmanagedType.I8, SizeConst = 8)]
        public virtual DateTime Created { get; set; }

        [StringLength(128)]
        [Column(Order = 8)]
        [DataMember(Order = 11)]
        [InstantAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public virtual string Creator { get; set; } = "";

        [DataMember(Order = 12)]
        [Column(Order = 9)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Index { get; set; } = -1;

        [Column(Order = 10)]
        [StringLength(256)]
        [DataMember(Order = 13)]
        [InstantAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public virtual string Label { get; set; } = "";

        public virtual TEntity Sign<TEntity>(TEntity entity = null) where TEntity : class, IOrigin
        {
            IOrigin origin = this;
            if (entity != null)
                origin = entity;

            origin.AutoId();
            if (!HaveTime())
                entity.Time = Log.Clock;
            Stamp(origin);
            origin.Created = origin.Time;
            return default;

        }

        public virtual TEntity Stamp<TEntity>(TEntity entity = null) where TEntity : class, IOrigin
        {
            IOrigin origin = this;
            if (entity != null)
                origin = entity;
            origin.Modified = Log.Clock;
            return entity;
        }

        public virtual bool Equals(IUnique other)
        {
            return Id.Equals(other.Id);
        }

        public virtual int CompareTo(IUnique other)
        {
            return Id.CompareTo(other.Id);
        }
    }



}
