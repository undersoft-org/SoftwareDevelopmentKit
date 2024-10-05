using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Access.MultiTenancy;

public class Tenant : DataObject, ITenant
{
    [DisabledRubric]
    [DisplayRubric("TenantId")]
    public override long Id
    {
        get => base.Id;
        set => base.Id = value;
    }

    [RubricSize(32)]
    [DisplayRubric("Name")]
    public string TenantName { get; set; }

    [RubricSize(32)]
    [DisplayRubric("Url")]
    public string TenantUrl { get; set; }

    [RubricSize(32)]
    [DisplayRubric("Path")]
    public string TenantPath { get; set; }
}
