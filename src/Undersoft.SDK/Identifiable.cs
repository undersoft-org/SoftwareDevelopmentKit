using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Undersoft.SDK.Instant.Attributes;
using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Uniques;

namespace Undersoft.SDK
{
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public class Identifiable : IIdentifiable
    {
        public Identifiable() : this(true) { }

        public Identifiable(bool autoId)
        {
            IsNew = true;

            if (!autoId)
                return;

            code.SetId(Unique.NewId);
        }

        [NotMapped]
        [JsonIgnore]
        [IgnoreDataMember]
        private Uscn code;

        [IdentityRubric(Order = 2)]
        [StringLength(32)]
        [ConcurrencyCheck]
        [DataMember(Order = 0)]
        [Column(Order = 0)]
        [InstantAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public virtual string CodeNo
        {
            get => code;
            set
            {
                if (value != null)
                    code.FromTetrahex(value.ToCharArray());
            }
        }

        [Key]
        [KeyRubric(Order = 0)]
        [DataMember(Order = 1)]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public virtual long Id
        {
            get => code.Id;
            set
            {
                if (IsNew && value != 0 && !code.Equals(value))
                {
                    code.SetId(value);
                    IsNew = false;
                }
            }
        }

        [IdentityRubric(Order = 1)]
        [DataMember(Order = 2)]
        [Column(Order = 2)]
        public virtual long TypeId
        {
            get
            {
                if (code.TypeId == 0)
                {
                    var t = GetType();
                    TypeName = t.FullName;
                    code.SetTypeId(TypeName.UniqueKey32());
                }
                return code.TypeId;
            }
            set
            {
                if (value != 0 && value != code.TypeId)
                {
                    code.TypeId = value;
                }
            }
        }

        [StringLength(768)]
        [DataMember(Order = 7)]
        [Column(Order = 4)]
        [InstantAs(UnmanagedType.ByValTStr, SizeConst = 768)]
        public virtual string TypeName { get; set; }

        [NotMapped]
        [JsonIgnore]
        [IgnoreDataMember]
        public virtual DateTime Time
        {
            get => DateTime.FromBinary(code.Time);
            set => code.SetDateLong(value.ToBinary());
        }

        [NotMapped]
        [JsonIgnore]
        [IgnoreDataMember]
        private bool IsNew { get; set; }

        public virtual void SetOriginId(long tenantId)
        {
            code.SetOriginId(tenantId);
        }
        public virtual long GetOriginId()
        {
            return code.OriginId;
        }

        public virtual void SetServiceId(int serviceId)
        {
            code.BlockZ = (ushort)serviceId;
        }
        public virtual int GetServiceId()
        {
            return code.BlockZ;
        }

        public virtual void SetCategoryId(int categoryId)
        {
            code.CategroyId = categoryId;
        }
        public virtual int GetCategoryId()
        {
            return code.CategroyId;
        }

        public virtual void SetClusterId(int clusterId)
        {
            code.BlockX = (ushort)clusterId;
        }
        public virtual int GetClusterId()
        {
            return code.BlockX;
        }

        public virtual long AutoId()
        {
            var key = code.Id;
            return key != 0 ? key : code.SetId(Unique.NewId);
        }

        public virtual byte GetPriority()
        {
            return code.GetPriority();
        }
        public virtual byte SetPriority(byte priority)
        {
            return code.SetPriority(priority);
        }

        public virtual void SetFlag(DataFlags state, bool flag)
        {
            code.SetFlag(state, flag);
        }
        public virtual void GetFlag(DataFlags state)
        {
            code.GetFlag(state);
        }

        public virtual long SetId(long id)
        {
            long longid = id;
            long key = Id;
            if (longid != 0 && key != longid)
                return Id = longid;
            return AutoId();
        }
        public virtual long SetId(object id)
        {
            if (id == null)
                return AutoId();
            else if (id.GetType().IsPrimitive)
                return SetId((long)id);
            else
                return SetId(id.UniqueKey64());
        }

        public virtual long SetTypeId(long typeid)
        {
            return code.SetTypeId(typeid);
        }
        public virtual long SetTypeId(Type type)
        {

            return code.SetTypeId(type.FullName.UniqueKey32());
        }

        public virtual bool HaveTime()
        {
            return code.GetDateLong() != 0;
        }
    }
}
