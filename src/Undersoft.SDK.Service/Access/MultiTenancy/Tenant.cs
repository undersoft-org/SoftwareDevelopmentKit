using System.ComponentModel.DataAnnotations.Schema;
using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Access.MultiTenancy;

public class Tenant : DataObject, ITenant
{
    [NotMapped]
    [DisabledRubric]
    [RubricSize(20)]
    [DisplayRubric("Number")]
    public string TenantNumber
    {
        get => base.Id.ToString();
        set => base.Id = long.Parse(value);
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
